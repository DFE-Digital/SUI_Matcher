#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$SettingsPath = ".idea/.idea.DotSettings.user"
)

$ErrorActionPreference = "Stop"

# --- Resolve settings file ---
$settingsFile = Resolve-Path -LiteralPath $SettingsPath -ErrorAction SilentlyContinue
if (-not $settingsFile) {
    Write-Host "[mise] DotSettings not found at $SettingsPath, skipping"
    exit 0
}
$settingsFile = $settingsFile.Path

# --- Locate dotnet (prefer mise which) ---
function Get-DotnetPath {
    $mise = Get-Command mise -ErrorAction SilentlyContinue
    if ($mise) {
        try {
            $p = (& $mise.Path which dotnet).Trim()
            if ($p) { return $p }
        }
        catch {}
    }
    ($cmd = Get-Command dotnet -ErrorAction SilentlyContinue) ? $cmd.Path : $null
}
$dotnetPath = Get-DotnetPath
if (-not $dotnetPath) {
    Write-Host "[mise] dotnet not found, skipping"
    exit 0
}

# --- Parse Base Path from dotnet --info ---
$info = & $dotnetPath --info
$basePathLine = $info -split "`r?`n" | Where-Object { $_.Trim().ToLower().StartsWith("base path") } | Select-Object -First 1
if (-not $basePathLine) { throw "[mise] Could not parse Base Path from 'dotnet --info'" }
$basePath = ($basePathLine -split ":", 2)[1].Trim()
$msbuildDll = Join-Path $basePath "MSBuild.dll"

# --- Build recents ---
$recentPaths = [System.Collections.Generic.List[string]]::new()
$recentPaths.Add($dotnetPath) | Out-Null
if ($IsWindows) {
    $winShim = Join-Path $env:LOCALAPPDATA "mise\shims\dotnet.exe"
    if (Test-Path $winShim) { $recentPaths.Add($winShim) | Out-Null }
}
else {
    $unixShim = Join-Path (Join-Path $HOME ".local/share/mise") "shims/dotnet"
    if (Test-Path $unixShim) { $recentPaths.Add($unixShim) | Out-Null }
}

# --- JetBrains key-encoding ---
function Encode-JBKey([string]$s) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $s.ToCharArray()) {
        if ($ch -match '[A-Za-z0-9\-_]') { [void]$sb.Append($ch) }
        else { [void]$sb.Append(("_{0:X4}" -f [int][char]$ch)) }
    }
    $sb.ToString()
}

# --- XPath literal builder (handles quotes) ---
function Convert-ToXPathLiteral([string]$s) {
    if ($s -notmatch "'") { return "'$s'" }
    if ($s -notmatch '"') { return "`"$s`"" }
    $parts = $s -split "'"
    $segments = @()
    for ($i = 0; $i -lt $parts.Count; $i++) {
        if ($parts[$i] -ne "") { $segments += "'$($parts[$i])'" }
        if ($i -lt ($parts.Count - 1)) { $segments += '"''"' }
    }
    "concat({0})" -f ($segments -join ", ")
}

# --- Load XML + namespaces ---
[xml]$doc = Get-Content -LiteralPath $settingsFile -Raw
$nsX = "http://schemas.microsoft.com/winfx/2006/xaml"
$nsS = "clr-namespace:System;assembly=mscorlib"
$nsWpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"

$nsmgr = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$nsmgr.AddNamespace("x", $nsX)
$nsmgr.AddNamespace("s", $nsS)
$nsmgr.AddNamespace("wpf", $nsWpf)

$root = $doc.SelectSingleNode("/wpf:ResourceDictionary", $nsmgr)
if (-not $root) { throw "[mise] Invalid DotSettings XML: missing wpf:ResourceDictionary root" }

# --- Helpers ---
function Find-NodeByKey([string]$localName, [string]$nsUri, [string]$key) {
    $keyLit = Convert-ToXPathLiteral $key
    $xpath = "//s:$localName[@x:Key=$keyLit]"
    $doc.SelectSingleNode($xpath, $nsmgr)
}

function New-Element([string]$localName, [string]$nsUri) {
    $doc.CreateElement($localName, $nsUri)
}

function Set-StringValue([string]$key, [string]$value) {
    $node = Find-NodeByKey -localName "String" -nsUri $nsS -key $key
    if (-not $node) {
        $node = New-Element -localName "String" -nsUri $nsS
        $null = $node.SetAttribute("Key", $nsX, $key)
        $null = $root.AppendChild($node)
    }
    $node.InnerText = $value
}

function Set-BoolValue([string]$key, [bool]$value) {
    $node = Find-NodeByKey -localName "Boolean" -nsUri $nsS -key $key
    if (-not $node) {
        $node = New-Element -localName "Boolean" -nsUri $nsS
        $null = $node.SetAttribute("Key", $nsX, $key)
        $null = $root.AppendChild($node)
    }
    $node.InnerText = $(if ($value) { "True" } else { "False" })
}


# --- Keys we update ---
$dotnetCliKey = "/Default/Environment/Hierarchy/Build/BuildTool/DotNetCliExePath/@EntryValue"
$customMsbuildKey = "/Default/Environment/Hierarchy/Build/BuildTool/CustomBuildToolPath/@EntryValue"
$recentPrefix = "/Default/Environment/Hierarchy/Build/BuildTool/RecentDotNetCliExePaths/="

# --- Apply updates ---
Set-StringValue $customMsbuildKey $msbuildDll
Set-StringValue $dotnetCliKey     $dotnetPath

# Remove existing recent entries
$nodesToRemove = @()
foreach ($n in $root.ChildNodes) {
    if ($n.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
    $k = $n.GetAttribute("Key", $nsX)
    if ([string]::IsNullOrWhiteSpace($k)) { continue }
    if ($k.StartsWith($recentPrefix)) { $nodesToRemove += $n }
}
foreach ($n in $nodesToRemove) { [void]$root.RemoveChild($n) }

# Recreate recents
$seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($p in $recentPaths) {
    if (-not $seen.Add($p)) { continue }
    $enc = Encode-JBKey $p
    Set-BoolValue "$recentPrefix$enc/@EntryIndexedValue" $true
}

# --- Write back (UTF-8 no BOM) ---
$backup = "$settingsFile.bak"
Copy-Item -LiteralPath $settingsFile -Destination $backup -Force
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$sw = New-Object System.IO.StreamWriter($settingsFile, $false, $utf8NoBom)
$doc.Save($sw)
$sw.Close()

Write-Host "[mise] Updated $settingsFile (backup -> $(Split-Path -Leaf $backup))"

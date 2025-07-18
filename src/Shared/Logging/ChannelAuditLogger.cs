using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Shared.Logging;


[ExcludeFromCodeCoverage(Justification = "No logic to test, just a wrapper around a channel. No need to test Feature Management.")]
public class ChannelAuditLogger(ILogger<ChannelAuditLogger> logger, Channel<AuditLogEntry> channel, IVariantFeatureManager featureManager) : IAuditLogger
{
    public async Task LogAsync(AuditLogEntry entry)
    {
        var auditEnabled = await featureManager.IsEnabledAsync("EnableAuditLogging");
        logger.LogInformation("[AUDIT] feature flag status {AuditEnabled}.", auditEnabled);
        if (auditEnabled)
        {
            await channel.Writer.WriteAsync(entry);
        }
    }
}
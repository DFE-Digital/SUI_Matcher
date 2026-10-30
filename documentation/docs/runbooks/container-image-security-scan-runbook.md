# Container image security scanning run book

## Purpose

This run book explains how to build container images locally using either **Docker** or **Podman** and scan them for
security vulnerabilities using Aqua Trivy (and optionally Anchore Grype) prior to publishing them to Azure Container
Registry (ACR). It also documents scanning remote images directly in ACR and gating deployment pipelines.

This run book supports:
- [Blob processor local run book](blob-processor-local-runbook.md#build-and-scan-container-images-locally-with-podman-or-docker)
- [GraphQL processor local run book](graphql-processor-local-runbook.md#build-and-scan-container-images-locally-with-podman-or-docker)

## Base image architecture

All application Dockerfiles use Microsoft's hardened Azure Linux 3.0 Distroless base image:
`mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless-extra`

Key security characteristics:
- **Distroless runtime:** Zero shell (`/bin/sh`, `/bin/bash`), zero package manager (`tdnf`, `apt`, `apk`), and no core utilities (`curl`, `wget`).
- **Minimal attack surface:** Contains only 13 minimal RPM packages required by .NET (compared to ~97 packages in standard Ubuntu/Debian images).
- **Non-root execution:** Runs as non-root user `$APP_UID` (UID `1654`).
- **Full globalization:** The `-extra` variant bundles ICU globalization and `tzdata`, ensuring accurate UK NHS patient record string comparison and date collation.
- **`glibc` compatibility:** Full compatibility with standard native C runtime libraries without the DNS or memory allocation quirks of `musl`.

## Prerequisites

### Docker users
- Docker Desktop or Docker Engine installed (`docker --version`).
- Docker daemon running (`docker info`).

### Podman users
- Podman installed (`podman --version`).
- Podman machine running on macOS:
  ```bash
  podman machine start
  ```

---

## Build images locally

Run these commands from the repository root:

### With Docker

Build the external API image:
```bash
docker build -t external-api:local -f src/external-api/Dockerfile .
```

Build the matching API image:
```bash
docker build -t matching-api:local -f src/matching-api/Dockerfile .
```

Build the storage processor job image:
```bash
docker build -t sui-client-storage-process-job:local -f src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile .
```

Build the GraphQL processor job image:
```bash
docker build -t sui-client-graphql-process-job:local -f src/SUI.Client/SUI.Client.GraphQLProcessJob/Dockerfile .
```

Build the PDS emulator image (optional test dependency):
```bash
docker build -t pds-emulator:local -f src/tools/pds-emulator/Dockerfile .
```

### With Podman

Build the external API image:
```bash
podman build -t external-api:local -f src/external-api/Dockerfile .
```

Build the matching API image:
```bash
podman build -t matching-api:local -f src/matching-api/Dockerfile .
```

Build the storage processor job image:
```bash
podman build -t sui-client-storage-process-job:local -f src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile .
```

Build the GraphQL processor job image:
```bash
podman build -t sui-client-graphql-process-job:local -f src/SUI.Client/SUI.Client.GraphQLProcessJob/Dockerfile .
```

Build the PDS emulator image (optional test dependency):
```bash
podman build -t pds-emulator:local -f src/tools/pds-emulator/Dockerfile .
```

---

## Scan images for vulnerabilities with Aqua Trivy

[Aqua Trivy](https://github.com/aquasecurity/trivy) is the project's standard vulnerability scanner, mirroring
`.github/workflows/gh-trivy.yml`. It scans both the minimal OS layer (Azure Linux RPM packages) and application-level language
dependencies (NuGet packages recorded in `.deps.json`).

Trivy runs directly inside a container without needing any host binaries installed.

### With Docker

```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v trivy-cache:/root/.cache/ \
  aquasec/trivy image external-api:local
```

### With Podman

```bash
podman run --rm \
  --security-opt label=disable \
  --user 0 \
  -v /run/podman/podman.sock:/var/run/docker.sock \
  -v trivy-cache:/root/.cache/ \
  aquasec/trivy image localhost/external-api:local
```

### Command options explained

- `-v /var/run/docker.sock:/var/run/docker.sock` *(Docker)* / `-v /run/podman/podman.sock:/var/run/docker.sock` *(Podman)*: Mounts the container engine socket into the container so Trivy can inspect local images without pushing them to a registry.
- `--security-opt label=disable` *(Podman only)*: Disables container SELinux separation so the container can access the mounted Podman socket.
- `--user 0` *(Podman only)*: Runs the scanner container as root inside the container to grant read permissions to the forwarded socket inside the Podman machine.
- `-v trivy-cache:/root/.cache/`: Caches the downloaded vulnerability database in a named volume so subsequent scans complete rapidly.

---

## Generate vulnerability reports

### Human-readable summary table (default)
Run the scan command above without output format flags to display the console table with vulnerability breakdown by target and severity.

### Export a structured JSON report
Export vulnerability scan results to a JSON file in the repository root:

#### With Docker:
```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$(pwd)":/reports \
  aquasec/trivy image \
    --format json \
    --output /reports/external-api-trivy-report.json \
    external-api:local
```

#### With Podman:
```bash
podman run --rm \
  --security-opt label=disable \
  --user 0 \
  -v /run/podman/podman.sock:/var/run/docker.sock \
  -v "$(pwd)":/reports \
  aquasec/trivy image \
    --format json \
    --output /reports/external-api-trivy-report.json \
    localhost/external-api:local
```

### Export an HTML report
Generate a self-contained HTML vulnerability report for sharing or auditing:

#### With Docker:
```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$(pwd)":/reports \
  aquasec/trivy image \
    --format template \
    --template "@/contrib/html.tpl" \
    --output /reports/external-api-trivy-report.html \
    external-api:local
```

#### With Podman:
```bash
podman run --rm \
  --security-opt label=disable \
  --user 0 \
  -v /run/podman/podman.sock:/var/run/docker.sock \
  -v "$(pwd)":/reports \
  aquasec/trivy image \
    --format template \
    --template "@/contrib/html.tpl" \
    --output /reports/external-api-trivy-report.html \
    localhost/external-api:local
```

### Automated quality gate (fail on High or Critical)
To use the scan as an automated gate in scripts or CI pipelines (returns exit code 1 if any HIGH or CRITICAL issues exist):

#### With Docker:
```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image \
    --exit-code 1 \
    --severity HIGH,CRITICAL \
    external-api:local
```

#### With Podman:
```bash
podman run --rm \
  --security-opt label=disable \
  --user 0 \
  -v /run/podman/podman.sock:/var/run/docker.sock \
  aquasec/trivy image \
    --exit-code 1 \
    --severity HIGH,CRITICAL \
    localhost/external-api:local
```

---

## Scan remote images in Azure Container Registry (ACR)

Trivy can also scan images published in ACR directly using a temporary OAuth token without downloading the entire image to your machine:

### With Docker:
```bash
ACR_TOKEN="$(az acr login --name "${ACR_NAME}" --expose-token --output tsv --query accessToken)"

docker run --rm \
  -e TRIVY_USERNAME=00000000-0000-0000-0000-000000000000 \
  -e TRIVY_PASSWORD="${ACR_TOKEN}" \
  -v trivy-cache:/root/.cache/ \
  aquasec/trivy image \
    --exit-code 1 \
    --severity HIGH,CRITICAL \
    "${ACR_LOGIN_SERVER}/${IMAGE_REPOSITORY}:${IMAGE_TAG}"
```

### With Podman:
```bash
ACR_TOKEN="$(az acr login --name "${ACR_NAME}" --expose-token --output tsv --query accessToken)"

podman run --rm \
  -e TRIVY_USERNAME=00000000-0000-0000-0000-000000000000 \
  -e TRIVY_PASSWORD="${ACR_TOKEN}" \
  -v trivy-cache:/root/.cache/ \
  aquasec/trivy image \
    --exit-code 1 \
    --severity HIGH,CRITICAL \
    "${ACR_LOGIN_SERVER}/${IMAGE_REPOSITORY}:${IMAGE_TAG}"
```

---

## Cross-validate with Anchore Grype (optional)

Optionally cross-validate vulnerability results using [Anchore Grype](https://github.com/anchore/grype):

### With Docker:
```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  anchore/grype:latest external-api:local
```

### With Podman:
```bash
podman run --rm \
  --security-opt label=disable \
  --user 0 \
  -v /run/podman/podman.sock:/var/run/docker.sock \
  anchore/grype:latest localhost/external-api:local
```

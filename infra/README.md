# IaC Stack Roots

The deployable infrastructure roots now live under `infra/stacks`.

Current structure:

- `client-agent`: the full DfE-hosted test architecture, composed from shared modules and client-agent-specific resources
- `blob-event-processor`: initial deployable event-driven stack foundation, composed from shared modules plus blob/queue storage resources
- `api-batch-processor`: placeholder isolated stack root for the future batch-oriented architecture

Shared Bicep modules live under `infra/modules`.

Supported deployment roots:

- `src/app-host/infra` still contains the existing application-layer deployment assets and the current laptop-driven deployment flow for existing client environments
- `src/SUI.Client/SUI.Client.Watcher/infra/client.bicep` remains a legacy dedicated client-infrastructure root used by the client infra workflow
- `infra/stacks/client-agent/main.bicep` is the full DfE-hosted test stack root

CI/CD:

- `.github/workflows/gh-deploy-infra.yml` remains the existing `src/app-host/infra` infrastructure workflow and still drives the current `azd provision` path
- `.github/workflows/gh-client-infra-deploy.yml` deploys the legacy dedicated client-infrastructure path under `src/SUI.Client/SUI.Client.Watcher/infra`
- `.github/workflows/gh-client-agent-infra-deploy.yml` deploys the full `client-agent` stack root
- `.github/workflows/gh-blob-event-processor-infra-deploy.yml` deploys the `blob-event-processor` stack root
- The placeholder/future stacks should gain dedicated workflows when their stack roots become deployable

The intent is that stack roots define environment topology, while application deployment consumes outputs from the selected stack.

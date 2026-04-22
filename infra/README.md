# IaC Stack Roots

The deployable infrastructure roots now live under `infra/stacks`.

Current structure:

- `client-agent`: the current deployable architecture, composed from shared modules and client-agent-specific resources
- `blob-event-processor`: placeholder isolated stack root for the next event-driven architecture
- `api-batch-processor`: placeholder isolated stack root for the future batch-oriented architecture

Shared Bicep modules live under `infra/modules`.

Transitional compatibility:

- `src/app-host/infra` still contains the existing application-layer deployment assets and compatibility entry points
- `src/SUI.Client/SUI.Client.Watcher/infra/client.bicep` remains as a compatibility root for the old client-only deployment path, but now delegates to the shared `client-agent` module

The intent is that stack roots define environment topology, while application deployment consumes outputs from the selected stack.

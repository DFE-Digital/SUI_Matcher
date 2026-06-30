# IaC Stack Roots

The deployable infrastructure roots now live under `infra/stacks`.

Current structure:

- `client-agent`: the full DfE-hosted test architecture, composed from shared modules and client-agent-specific resources
- `blob-event-processor`: initial deployable event-driven stack foundation, composed from shared modules plus blob/queue storage resources
- `api-batch-processor`: isolated stack root for the future batch-oriented architecture, currently provisioning blob storage for low-confidence match output

Shared Bicep modules live under `infra/modules`.

Reusable storage modules under `infra/modules/shared` separate storage account creation, blob containers, queues, and storage private endpoint/DNS wiring so new stacks can compose only the storage resources they need.

Each stack root under `infra/stacks` is paired with a subscription-scope wrapper that creates or updates the stack-owned resource group and then deploys the stack into it.

The `blob-event-processor` wrapper also supports deploying into an existing resource group and using an existing storage account. In that mode, the stack still creates its standard blob containers, storage queues, private endpoints, and private DNS wiring for the configured account, but it does not create or manage the storage account service defaults. Its deployed storage, queue, Event Grid, private endpoint, and DNS resource set is intentionally preserved.

Supported deployment roots:

- `src/app-host/infra` is a legacy application-layer deployment path for existing client environments. It is intentionally frozen: new infrastructure capability — including registry/RBAC modules such as `acr-pull-rbac` — is added to the stack roots under `infra/stacks`, not retrofitted onto `app-host`. The new stacks own the full container-registry → managed-identity → AcrPull chain in code; the legacy path does not, and is deliberately not partially converted.
- `src/SUI.Client/SUI.Client.Watcher/infra/client.bicep` remains a legacy dedicated client-infrastructure root used by the client infra workflow
- `infra/stacks/client-agent/main.bicep` is the full DfE-hosted test stack root
- `infra/stacks/*/subscription.bicep` are the stack-owned resource-group entrypoints for the stack roots

CI/CD:

- `.github/workflows/gh-deploy-infra.yml` remains the existing `src/app-host/infra` infrastructure workflow and still drives the current `azd provision` path
- `.github/workflows/gh-client-infra-deploy.yml` deploys the legacy dedicated client-infrastructure path under `src/SUI.Client/SUI.Client.Watcher/infra`
- `.github/workflows/gh-client-agent-infra-deploy.yml` deploys the full `client-agent` stack through its subscription-scope wrapper and stack-owned resource group
- `.github/workflows/gh-blob-event-processor-infra-deploy.yml` deploys the `blob-event-processor` stack through its subscription-scope wrapper and stack-owned resource group
- `.github/workflows/gh-stack-decommission.yml` is the generic manual decommission workflow for stack-owned resource groups under `infra/stacks`
- The placeholder/future stacks should gain dedicated workflows when their stack roots become deployable

The intent is that stack roots define environment topology, while application deployment consumes outputs from the selected stack.

Related design notes:

- [Deployment IaC Stack Strategy](../documentation/docs/design/infra-stack-segregation.md)
- [SUI-1838: Azure Deployment Stacks Spike](../documentation/docs/design/azure-deployment-stacks-spike.md)

Decommissioning:

- Stack decommissioning for `infra/stacks` means deleting the entire stack-owned resource group.
- `.github/workflows/gh-stack-decommission.yml` is the supported teardown path for stacks deployed through `infra/stacks/*/subscription.bicep`.
- Deployments made with `resourceGroupMode=existing` are outside the stack-owned resource group teardown contract and must be cleaned up selectively.
- The legacy `src/app-host/infra` and `src/SUI.Client/SUI.Client.Watcher/infra` deployment paths are outside that decommission contract.

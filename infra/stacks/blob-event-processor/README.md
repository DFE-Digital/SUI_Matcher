# Blob-Event-Processor Stack

This stack is the deployable foundation for the event-driven processing architecture.

It composes the shared platform modules directly and adds the blob/queue storage resources needed by the storage-driven processing path.

Current scope:

- shared platform resources for this stack: identity, ACR, observability, monitoring, secrets, CAE, egress firewall, and VNet peering
- storage account, either created by the stack or supplied as an existing account
- blob containers for incoming, processed, and success files
- primary and poison storage queues for the processing job contract
- blob and queue private endpoints for the configured storage account
- Event Grid system topic and blob-created subscription for incoming files
- storage process ACA job triggered from the primary storage queue
- matching API and external API container apps (internal ingress) deployed into the stack's CAE
- Key Vault private endpoint for application secrets access

Production egress allows `api.service.nhs.uk`. Non-production egress allows `int.api.service.nhs.uk`.

`infra/stacks/blob-event-processor/subscription.bicep` is the subscription-scope entry point for this stack. By default, `resourceGroupMode=create` creates or updates the stack-owned `blob-event-processor` resource group and then deploys `main.bicep` into it.

It can also deploy into an existing resource group by setting:

- `resourceGroupMode=existing`
- `targetResourceGroupName=<existing-resource-group-name>`

## Storage modes

The default storage mode is `storageAccountMode=create`. In this mode the stack creates the storage account, the blob/queue containers, and the blob/queue private endpoints and private DNS wiring.

The storage implementation uses shared composable modules internally, but the deployed resource set for this stack remains the full event-driven storage surface: storage account, containers, queues, Event Grid, blob and queue private endpoints, and blob and queue private DNS.

Created storage accounts use Standard LRS hot storage, disable blob public access, require TLS 1.2, and deny network access except for trusted Azure services.

For client-provided storage, set:

- `storageAccountMode=existing`
- `existingStorageAccountName=<existing-storage-account-name>`

Existing storage mode expects the storage account to already exist in the target resource group. The stack creates the SUI-owned containers and queues in that account:

- `incoming`
- `processed`
- `success`
- `storage-process-job`
- `storage-process-job-poison`

Both storage modes create blob and queue private endpoints plus private DNS wiring for the configured storage account. In existing storage mode, the private endpoints target the supplied account.

## Tag configuration

The stack applies a common tag set to managed resources. Two optional parameters support policy-shaped environments without hard-coding those policy tags into client infrastructure:

- `tagEnvironmentName` overrides the `Environment` tag value when Azure Policy expects a value that differs from `environmentName`.
- `additionalTags` adds environment-specific tags as a JSON object. For GitHub Actions, set the repo variable `AZURE_ADDITIONAL_TAGS` to a JSON object such as `{"Service Offering":"SUI"}`. Leave it unset for client environments that should not receive DfE-specific policy tags.

## GitHub workflow behaviour

The deploy workflow wraps the same subscription-scope Bicep template and adds a small amount of shell logic:

- logs in to Azure using GitHub OIDC
- validates `containerAppPeSubnet` is configured
- validates `targetResourceGroupName` when `resourceGroupMode=existing`
- validates `existingStorageAccountName` when `storageAccountMode=existing`
- passes `AZURE_TAG_ENVIRONMENT_NAME` to `tagEnvironmentName` when set
- passes `AZURE_ADDITIONAL_TAGS` to `additionalTags` when set
- derives the stack resource group as `${environmentPrefix}-${environmentName,,}-blob-event-processor`, unless an existing resource group is supplied
- derives the deployment name as `${resourceGroupName}-${location,,}-${mode}`
- runs `az deployment sub what-if` or `az deployment sub create`
- writes GitHub summaries, template outputs, and a post-deploy resource inventory

## Direct deployment

Some client environments may need to be deployed from an approved device rather than GitHub Actions. The same subscription-scope entry point can be used directly:

```bash
az login
az account set --subscription <subscription-id>

az deployment sub what-if \
  --name <deployment-name> \
  --location <location> \
  --subscription <subscription-id> \
  --template-file infra/stacks/blob-event-processor/subscription.bicep \
  --parameters \
    environmentName=<environment-name> \
    environmentPrefix=<environment-prefix> \
    location=<location> \
    monitoringActionGroupEmail=<email> \
    containerAppManagedEnvironmentNumber=<number> \
    containerAppVnet=<vnet-cidr> \
    containerAppEnvSubnet=<aca-subnet-cidr> \
    containerAppPeSubnet=<private-endpoint-subnet-cidr> \
    includeRoleAssignments=true \
    storageProcessJobImageTag=<image-tag> \
    matchingApiImageTag=<image-tag> \
    externalApiImageTag=<image-tag> \
    turnOnAlerts=false \
    tagEnvironmentName=<optional-environment-tag-value> \
    additionalTags='{"Service Offering":"SUI"}' \
    resourceGroupMode=existing \
    targetResourceGroupName=<existing-resource-group-name> \
    storageAccountMode=existing \
    existingStorageAccountName=<existing-storage-account-name>
```

Use `az deployment sub create` with the same parameters to deploy after reviewing the what-if output.

If Azure CLI cannot write to the default local config directory, set `AZURE_CONFIG_DIR` to a writable temporary directory before running the commands.

## Unique to blob stack infrastructure

### ACA Job - Azure Container App job

The ACA job is the blob event driven processor that runs alongside the Matching and External APIs in the same ACAE (Azure container apps environment).

It uses the shared managed identity, pulls `sui-client-storage-process-job:<tag>` from the stack ACR, and is granted Storage Blob Data Contributor and Storage Queue Data Contributor when `includeRoleAssignments=true`.

The job is configured with 2 vCPU, 4Gi memory, a 6 hour replica timeout, no automatic replica retries, and `StorageProcessJob__MaxDequeueCount=1`.

### Azure Storage blob and queues

Setup of azure storage blob for file upload for processing.

Setup of Azure storage queues for the processing job contract, one for primary messages and one for poison messages.

### Event Grid

Setup of Event Grid. The Event Grid subscription listens for `Microsoft.Storage.BlobCreated` events under the `incoming` container and sends messages to the configured storage queue.

### KEDA

KEDA is a Kubernetes-based event-driven auto scaler. It monitors the configured storage queue for new messages and scales the processing job up or down. The job is configured to run a maximum of one execution at a time, so only one queue message is processed at a time and later messages wait until the current execution finishes.

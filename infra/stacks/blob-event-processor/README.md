# Blob-Event-Processor Stack

This stack is the deployable foundation for the event-driven processing architecture.

It composes the shared platform modules directly and adds the initial blob/queue storage resources needed by the storage-driven processing path.

Current scope:

- shared platform resources for this stack
- storage account
- blob containers for incoming, processed, and success files
- primary and poison storage queues for the processing job contract

`infra/stacks/blob-event-processor/subscription.bicep` is the stack-owned resource-group entry point for this stack. It creates or updates the `blob-event-processor` resource group and then deploys `main.bicep` into it.

Follow up work:

- ACA - Deploying to the ACR
- Match/External/Yarp - Infrastructure and deployment to the ACR
- Any further network/infrastructure needs for the processing job

## Unique to blob stack infrastructure

### ACA Job - Azure Container App job

The ACA Job is the blob event driven processor that runs alongside the Matching, External API's in the same ACAE (Azure container apps environment)

### Azure Storage blob and queues

Setup of azure storage blob for file upload for processing.

Setup of Azure storage queues for the processing job contract, one for primary messages and one for poison messages.

### Event Grid

Setup of Event Grid, the EventGrid will trigger from a new blob file being created in the configured storage, it will then send a message to the configured storage queue.

### KEDA

KEDA is a Kubernetes-based event-driven auto scaler. It monitors the configured storage queue for new messages and scales the processing job up or down. The job is configured to run a maximum of one execution at a time, so only one queue message is processed at a time and later messages wait until the current execution finishes.

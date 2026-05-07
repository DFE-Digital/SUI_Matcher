# Blob-Event-Processor Stack

This stack is the deployable foundation for the event-driven processing architecture.

It composes the shared platform modules directly and adds the initial blob/queue storage resources needed by the storage-driven processing path.

Current scope:

- shared platform resources for this stack
- storage account
- blob containers for incoming, processed, and success files
- primary and poison storage queues for the processing job contract

`infra/stacks/blob-event-processor/subscription.bicep` is the stack-owned resource-group entrypoint for this stack. It creates or updates the `blob-event-processor` resource group and then deploys `main.bicep` into it.

Follow-up work should add the Event Grid wiring, ACA Job deployment path, and any further networking/security resources directly to this stack without introducing any dependency on `client-agent`.

# Api-Batch-Processor Stack

This is the root for the planned batch-oriented architecture.

It exists to reserve the stack boundary and deployment contract without introducing any dependency on `client-agent`.

`infra/stacks/api-batch-processor/subscription.bicep` is the stack-owned resource-group entrypoint for this stack, although no dedicated deploy workflow exists yet.

Current scope:

- storage account for low-confidence match output
- blob private endpoint for storage access from the stack virtual network
- blob private DNS zone and virtual network link

The stack deliberately does not create Event Grid resources, storage queues, queue private endpoints, or queue private DNS. High-confidence matches are expected to be sent directly back to a GraphQL API by later application work.

The subscription entrypoint requires `containerAppVnet` and `containerAppPeSubnet` so the blob private endpoint can be placed in the stack virtual network.

Follow-up work should add the scheduled processing, external API integration, identity, secret, and remaining networking resources required by this architecture directly to this stack.

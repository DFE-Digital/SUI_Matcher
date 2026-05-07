# Api-Batch-Processor Stack

This is the placeholder root for the planned batch-oriented architecture.

It exists to reserve the stack boundary and deployment contract without introducing any dependency on `client-agent`.

`infra/stacks/api-batch-processor/subscription.bicep` is the stack-owned resource-group entrypoint for this stack, although no dedicated deploy workflow exists yet.

Follow-up work should add the scheduled processing, external API integration, identity, secret, and networking resources required by this architecture directly to this stack.

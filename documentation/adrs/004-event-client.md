# ADR: Compute Model for Asynchronous CSV Processing Client

**Date:** April 13, 2026

**Status:** Accepted

**Author:** Stuart Maskell

### Context

We are expanding our backend Azure Container Apps (ACA) environment with a new event-driven 'Client' application to process batch CSV files (0.5MB to 15MB).

The architectural constraints present a unique profile: volume is exceptionally low (approx. 1 file per week), but processing time is exceptionally high (1 to 4 hours per file) due to sequential dependency on third-party backend integrations. Furthermore, the data extraction process is fully idempotent, meaning failures do not corrupt data and jobs can safely be restarted from the beginning. We have a strong preference to keep operational costs as low as possible, while retaining resilient error handling (row-level logging, file-level halting).

### Considered Options

1. **Azure Durable Functions:** An application-level orchestration framework that provides stateful workflows, automatic checkpointing, and advanced framework-level retry/poison queue mechanics.
2. **.NET Console Application (ACA Job):** A bare-bones, stateless run-to-completion processing model that relies on the Azure Container Apps Jobs infrastructure and Azure Storage Queues SDK for scaling and resilience.

### Proposed Decision

We will utilize a stateless **.NET Console Application** deployed specifically as an **Azure Container Apps Job** (Event-Driven).

### Rationale

* **Architectural Alignment (Complexity vs. Need):** Our workload profile is strictly linear, sequential, and idempotent. The primary value proposition of Durable Functions is complex state management, event sourcing, and parallel orchestration (fan-out/fan-in). Introducing a state machine for a purely linear task violates the principle of simplicity. We are choosing to avoid the architectural "tax" of a framework we do not fully utilize.
* **Native Run-to-Completion Lifecycle:** By deploying as an ACA Job rather than a continuously running service, the infrastructure natively handles the execution lifecycle. KEDA will trigger the Job based on queue depth, the application will process the file, and upon completion, the container terminates immediately. This perfectly matches the batch-processing nature of the task.
* **Sufficient Resilience via SDK:** While Durable Functions offer automated poison queue routing, the standard `Azure.Storage.Queues` .NET SDK provides all the necessary primitives (`DequeueCount`, message visibility) to handle failures. By choosing a localized application approach, we accept the minor implementation task of explicitly routing failed messages to a poison queue, recognizing that this localized logic is simpler than adopting an entire orchestration framework.
* **Cost Optimization:** While not a strict constraint, there is a strong objective to keep operational costs as low as possible. An ACA Job guarantees that we only pay for exact compute seconds used during the 1-4 hour run, scaling completely to zero without ambient costs upon termination. Conversely, Durable Functions require a dedicated Azure Storage Account to constantly manage orchestration history and lease blobs, creating persistent transaction costs even during idle periods—an unnecessary expense given our low processing volume.
* **Trade-off Acceptance:** We acknowledge that eschewing the Durable Task framework means accepting the responsibility of manually managing the queue message lease (visibility timeouts) during the 4-hour execution window, as well as manually routing messages that exceed retry limits. We accept this trade-off because the overarching architectural simplicity, infrastructural decoupling, and cost efficiency heavily outweigh the burden of these specific implementation details.

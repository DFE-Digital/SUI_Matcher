# ADR: Use Azure Table Storage for Audit Logging with Secure Access via Managed Identity

### Date
2025-07-14

### Status
Draft

### Author
Stuart Maskell

## Context

We need a solution for capturing and storing audit logs from our .NET Aspire APIs hosted in Azure. 
These logs include actions like user searches, login attempts et cetera. We expect a low number of queries and high amount of writes. The audit system must support:

- Secure storage with restricted access
- Scalability for up to 140+ clients, potentially generating hundreds of logs per day
- Low-cost and low-maintenance operation
- Ease of querying for occasional inspection
- Future-proofing in case of higher throughput or expanded logging requirements
- Local development parity, ideally using an emulator

## Considered Options

1. Azure SQL Database
    - Easy querying, Power BI support, familiar tech
    - Higher cost, less optimized for high-volume append-only writes

2. Azure Blob Storage
    - Cheap, flexible
    - Poor query ability as it has no structure or schema, requires custom parsing logic

3. Azure Table Storage
    - Cheap, scalable, structured but schema-less
    - Basic query capabilities, no complex relationships

4. Event Queue + Separate Logging Service
    - Very scalable, future-proof
    - Added complexity and maintenance. YAGNI risk at current scale

## Proposed Decision

Use Azure Table Storage as the backing store for audit logging, with:

- Hot storage tier for optimized write performance
- Private endpoint to restrict access to the internal Azure environment
- A structured but flexible log format with 'Metadata' for additional context
- Partition key design optimized for access patterns
- Table creation at runtime or via CI/CD script
- Simple local development supported via Azurite emulator and Aspire.

## Rationale

Azure Table Storage meets our needs for secure, scalable, low-cost, and easily accessible structured logging. 
It simplifies local development and aligns with Azure-native security practices like managed identities and private networking.

SQL would add cost and complexity not justified by our read/query frequency, while blobs would make querying harder. Azure storage also has queue capabilities we can leverage in the future if needed.

This decision balances simplicity with scalability. 

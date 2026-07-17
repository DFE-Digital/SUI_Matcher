# Api-Batch-Processor Stack

This is the root for the planned batch-oriented architecture.

It exists to reserve the stack boundary and deployment contract without introducing any dependency on `client-agent`.

`infra/stacks/api-batch-processor/subscription.bicep` is the stack-owned resource-group entrypoint for this stack.

## Architecture Overview

The stack is designed as a secure, isolated batch processing environment, running:

- **GraphQL Process Job (`SUI.Client.GraphQLProcessJob`):** A containerized worker job designed to pull patient demographic data and synchronize match updates directly with an external GraphQL API.
- **Storage Account:** For storing low-confidence match output and diagnostic data.

### Stack Isolation & Security Components
The stack is fully self-contained and deploys all required networking and security infrastructure:
- **Virtual Network & Subnets:** Isolated subnets for both Container Apps environments (`containerAppEnvSubnet`) and Private Endpoints (`containerAppPeSubnet`).
- **Egress Firewall:** An isolated network firewall with predefined rule collections to securely restrict external internet egress to allowed registry data endpoints, monitoring endpoints, and verified NHS API domains.
- **User-Assigned Managed Identity:** For secure, passwordless authentication to the Azure Container Registry and other stack resources.
- **Azure Container Registry:** Stack-specific container registry hosting the job images.
- **Azure Key Vault:** Used for managing secure secrets and certificates.
- **Log Analytics Workspace & App Insights:** Central observability and telemetry storage for the stack's applications and jobs.

---

## Deployments and Configuration

The Container Apps Job can be deployed in two different triggers/modes depending on the desired operation:

1. **`automatic` Mode:**
   - Timed to run on a recurring schedule.
   - Default schedule: **Every 3 hours during working hours** (`0 9,12,15 * * 1-5` UTC — Monday through Friday, 9:00 AM, 12:00 PM, and 3:00 PM).
   - Fully customizable using the `cronExpression` Bicep parameter.

2. **`manual` Mode:**
   - No automatic timer runs.
   - Deployed ready to be triggered on-demand via the Azure CLI, Azure Portal, or REST API for testing, validation, and debugging.

### Custom Job Configuration

Any job-specific environment variables or .NET configurations (such as GraphQL Url, TenantId, ClientId, ClientSecret, and Match API details) are supplied as a secure JSON object via the `graphqlProcessJobConfiguration` parameter. Keys in this dictionary are formatted as standard .NET environment variable names (e.g., `GraphQLProcessJob__Url`).

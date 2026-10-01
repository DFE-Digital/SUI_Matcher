# GraphQL processor local run book

## Purpose

This run book explains how to deploy the api-batch-processor stack from a laptop using Azure CLI (either creating a new resource group or deploying into an existing one). It is written for repeatable local deployments, including cases where you only need to publish the API images or the GraphQL processor event job image.

## Prerequisites

- Azure CLI (`az`) installed.
- Permission to deploy to the target Azure subscription and resource group.
- Bash, WSL, or Git Bash for the examples below.
- A clean checkout of the branch, tag, or commit you intend to deploy.

## Choose your task

- Full first-time deployment: follow [Full deployment flow](#full-deployment-flow).
- Infrastructure only: follow [Common setup](#common-setup), then
  [Infrastructure deployment variables](#infrastructure-deployment-variables),
  [Run the infrastructure what-if](#run-the-infrastructure-what-if) and
  [Run the infrastructure deploy](#run-the-infrastructure-deploy).
- API images only: follow [Publish the API images to Azure Container Registry](#publish-the-api-images-to-azure-container-registry).
- GraphQL processor event job image only: follow
  [Publish the GraphQL processor image to Azure Container Registry](#publish-the-graphql-processor-image-to-azure-container-registry).
- PDS emulator image only: follow
  [Publish the PDS emulator image to Azure Container Registry](#publish-the-pds-emulator-image-to-azure-container-registry).
- Final application deployment after publishing images: follow
  [Redeploy infrastructure after publishing images](#redeploy-infrastructure-after-publishing-images).
- Smoke test: follow [Smoke test the deployment](#smoke-test-the-deployment).
- Troubleshooting: follow [Troubleshooting policy or deployment errors](#troubleshooting-policy-or-deployment-errors).

## Full deployment flow

1. [Pick a branch, tag or commit to deploy](#pick-a-branch-tag-or-commit-to-deploy).
2. [Run dotnet restore, build and tests](#run-dotnet-restore-build-and-tests).
3. [Common setup](#common-setup).
4. [Infrastructure deployment variables](#infrastructure-deployment-variables).
5. [Run the infrastructure what-if](#run-the-infrastructure-what-if).
6. [Run the infrastructure deploy](#run-the-infrastructure-deploy).
7. [Add the secrets to Key Vault](#add-the-secrets-to-key-vault).
8. [Publish the API images to Azure Container Registry](#publish-the-api-images-to-azure-container-registry).
9. [Publish the GraphQL processor image to Azure Container Registry](#publish-the-graphql-processor-image-to-azure-container-registry).
10. [Publish the PDS emulator image to Azure Container Registry](#publish-the-pds-emulator-image-to-azure-container-registry) (if deploying in test mode).
11. [Redeploy infrastructure after publishing images](#redeploy-infrastructure-after-publishing-images).
12. [Smoke test the deployment](#smoke-test-the-deployment).

## Pick a branch, tag or commit to deploy

Ensure the branch is clean of any uncommitted changes. Example for `main`:

```bash
git checkout main
git pull
```

## Run dotnet restore, build and tests

```bash
dotnet restore
dotnet build --no-restore
dotnet test
```

## Common setup

Run these commands from the repository root.

The local commands mirror the GitHub workflows:

- `.github/workflows/gh-api-batch-processor-infra-deploy.yml`
- `.github/workflows/gh-api-batch-processor-api-images.yml`
- `.github/workflows/gh-api-batch-processor-graphql-job-image.yml`

The GitHub workflow's `AZURE_CLIENT_ID` value is only needed for GitHub OIDC login. It is not needed when running
commands locally with an interactive `az login`.

Native PowerShell does not support `source .env` or the Bash examples as written. Use WSL or Git Bash, or translate the
setup into PowerShell syntax before running the Azure CLI commands.

Create a `.env-api-batch-processor` file in the repository root based on this template and fill in your target values:

```bash
# Target values.
AZURE_ENV_NAME="Prod"
AZURE_ENV_PREFIX="<environment-prefix>"
AZURE_TENANT_ID="<tenant-id>"
AZURE_SUBSCRIPTION_ID="<subscription-id>"
AZURE_LOCATION="<azure-region>"

# Resource group mode: "create" (creates new stack RG) or "existing" (deploys into existing RG).
RESOURCE_GROUP_MODE="create"
TARGET_RESOURCE_GROUP_NAME="" # Required only when RESOURCE_GROUP_MODE="existing".

# Infrastructure values.
AZURE_MONITORING_ACTION_GROUP_EMAIL="<monitoring-alert-email-address>"
AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER="<managed-environment-number>"
AZURE_CONTAINER_APP_VNET="<container-app-vnet-cidr>"
AZURE_CONTAINER_APP_ENV_SUBNET="<container-app-environment-subnet-cidr>"
AZURE_CONTAINER_APP_PE_SUBNET="<private-endpoint-subnet-cidr>"

# Workflow defaults. Change these only when the target deployment requires it.
AZURE_INCLUDE_ROLE_ASSIGNMENTS="true"
AZURE_TURN_ON_ALERTS="false"
GRAPHQL_PROCESS_JOB_IMAGE_TAG="latest"
MATCHING_API_IMAGE_TAG="latest"
EXTERNAL_API_IMAGE_TAG="latest"
PDS_EMULATOR_IMAGE_TAG="latest"
USE_PDS_EMULATOR="false"
DEPLOYMENT_MODE="manual"
CRON_EXPRESSION="0 9,12,15 * * 1-5"
ALLOWED_GRAPHQL_FQDNS='[]'
GRAPHQL_URL="" # Optional URL for the GraphQL endpoint
GRAPHQL_USE_AUTH="false" # Optional toggle to use authentication for the GraphQL endpoint
AZURE_TAG_ENVIRONMENT_NAME="" # Optional override for the Environment tag.
AZURE_ADDITIONAL_TAGS="{}" # Optional additional tags as a JSON object string.
ODS_CODE="" # Optional ODS code.

# A JSON object of settings to override from the appsettings. Use '{}' if no overrides are needed.
GRAPHQL_PROCESS_JOB_CONFIGURATION='{}'
```

## Infrastructure deployment variables

The infrastructure what-if and deployment use the same variables. Check these values before running either command.

Required variables:

- All [common variables](#required-common-variables).
- `AZURE_MONITORING_ACTION_GROUP_EMAIL`
- `AZURE_CONTAINER_APP_VNET`
- `AZURE_CONTAINER_APP_ENV_SUBNET`
- `AZURE_CONTAINER_APP_PE_SUBNET`
- `AZURE_INCLUDE_ROLE_ASSIGNMENTS`
- `AZURE_TURN_ON_ALERTS`
- `GRAPHQL_PROCESS_JOB_IMAGE_TAG`
- `MATCHING_API_IMAGE_TAG`
- `EXTERNAL_API_IMAGE_TAG`
- `GRAPHQL_PROCESS_JOB_CONFIGURATION` (use `{}` if empty)

Optional variables:

- `AZURE_TAG_ENVIRONMENT_NAME`
- `AZURE_ADDITIONAL_TAGS`
- `PDS_EMULATOR_IMAGE_TAG`
- `USE_PDS_EMULATOR`
- `ODS_CODE`
- `DEPLOYMENT_MODE`
- `CRON_EXPRESSION`
- `ALLOWED_GRAPHQL_FQDNS`
- `GRAPHQL_URL`
- `GRAPHQL_USE_AUTH`

The following variables must be set before starting any task from the middle of this run book:

- **`AZURE_ENV_NAME`**: The target environment name (e.g. `Prod`, `Test`, `Dev`).
  - *How to get:* Defined by your team's deployment target. Used in resource naming conventions and injected into containers as `ASPNETCORE_ENVIRONMENT`.
- **`AZURE_ENV_PREFIX`**: The prefix identifier used across resource names and resource groups (e.g. `sui`).
  - *How to get:* Defined by project naming standards. In Azure Portal, check the prefix on existing resources within your target resource group.
- **`AZURE_TENANT_ID`**: The Microsoft Entra ID (Azure Active Directory) Tenant ID GUID.
  - *Azure Portal:* Navigate to **Microsoft Entra ID** > **Overview** > copy **Tenant ID** under *Basic information*.
  - *Azure CLI:* `az account show --query tenantId -o tsv`
- **`AZURE_SUBSCRIPTION_ID`**: The Azure Subscription ID GUID where the resources are deployed.
  - *Azure Portal:* Search for **Subscriptions** in the top search bar > select your subscription > copy **Subscription ID** from the *Overview* blade.
  - *Azure CLI:* `az account show --query id -o tsv` or `az account list --output table`
- **`AZURE_LOCATION`**: The Azure region code (e.g. `uksouth`, `ukwest`, `westeurope`).
  - *Azure Portal:* Open your target resource group > check the region name in the **Location** field in the *Overview* blade (convert display name to short code, e.g. "UK South" $\rightarrow$ `uksouth`).
  - *Azure CLI:* `az group show --name <RESOURCE_GROUP_NAME> --query location -o tsv` or `az account list-locations --output table`
- **`RESOURCE_GROUP_MODE`**: Determines whether deployment creates a new stack resource group (`create`) or targets an existing one (`existing`). Default is `create`.
- **`TARGET_RESOURCE_GROUP_NAME`**: The exact name of an existing target Azure Resource Group. Required only when `RESOURCE_GROUP_MODE="existing"`.
  - *Azure Portal:* Search for **Resource groups** > find and copy the name of the destination resource group.
  - *Azure CLI:* `az group list --query "[].name" -o table`
- **`AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER`**: The sequence or version suffix for the Container Apps Environment naming convention (e.g. `01`, `02`).
  - *How to get:* Typically `"01"`. In Azure Portal, check existing Container App Environment resource names in your resource group (e.g. `<prefix>-<env>-abp-cae-01`) to match the suffix.
- **`STACK_RESOURCE_GROUP`**: The shell variable representing the target resource group.
  - *How to get:* Computed automatically by the setup script block above from `TARGET_RESOURCE_GROUP_NAME` (when `RESOURCE_GROUP_MODE="existing"`) or derived from `${AZURE_ENV_PREFIX}-$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')-api-batch-processor`.

If you are starting from an API image, GraphQL processor image, or infrastructure redeploy section, run this first:

Load the configuration variables into your terminal session and resolve the target stack resource group:

```bash
set -euo pipefail
source .env-api-batch-processor

STACK_RESOURCE_GROUP="${AZURE_ENV_PREFIX}-$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')-api-batch-processor"

if [ "${RESOURCE_GROUP_MODE:-create}" = "existing" ]; then
  STACK_RESOURCE_GROUP="${TARGET_RESOURCE_GROUP_NAME}"
fi
```

Log in to the Azure CLI targeting your specific Microsoft Entra tenant:

```bash
az login --tenant "${AZURE_TENANT_ID}"
```

The az cli should ask you which subscription to set but if in doubt you can Set the active Azure subscription for all subsequent deployment commands:

```bash
az account set --subscription "${AZURE_SUBSCRIPTION_ID}"
```

Verify that the active subscription and tenant match your expected environment:

```bash
az account show --query "{subscription:name, tenantId:tenantId}" --output table
```

## Run the infrastructure what-if

Run the what-if:

```bash
if [ -z "${STACK_RESOURCE_GROUP:-}" ]; then
  echo "ERROR: STACK_RESOURCE_GROUP is not set. Please run the setup block first."
  exit 1
fi

TEMPLATE_FILE="infra/stacks/api-batch-processor/subscription.bicep"
DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]' | tr -d ' ')-what-if"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment sub what-if \
  --name "${DEPLOYMENT_NAME}" \
  --location "${AZURE_LOCATION}" \
  --template-file "${TEMPLATE_FILE}" \
  --parameters \
    environmentName="${AZURE_ENV_NAME}" \
    environmentPrefix="${AZURE_ENV_PREFIX}" \
    location="${AZURE_LOCATION}" \
    monitoringActionGroupEmail="${AZURE_MONITORING_ACTION_GROUP_EMAIL}" \
    containerAppManagedEnvironmentNumber="${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}" \
    containerAppVnet="${AZURE_CONTAINER_APP_VNET}" \
    containerAppEnvSubnet="${AZURE_CONTAINER_APP_ENV_SUBNET}" \
    containerAppPeSubnet="${AZURE_CONTAINER_APP_PE_SUBNET}" \
    includeRoleAssignments="${AZURE_INCLUDE_ROLE_ASSIGNMENTS}" \
    turnOnAlerts="${AZURE_TURN_ON_ALERTS}" \
    graphqlProcessJobImageTag="${GRAPHQL_PROCESS_JOB_IMAGE_TAG}" \
    matchingApiImageTag="${MATCHING_API_IMAGE_TAG}" \
    externalApiImageTag="${EXTERNAL_API_IMAGE_TAG}" \
    pdsEmulatorImageTag="${PDS_EMULATOR_IMAGE_TAG}" \
    usePdsEmulator="${USE_PDS_EMULATOR}" \
    deploymentMode="${DEPLOYMENT_MODE}" \
    cronExpression="${CRON_EXPRESSION}" \
    odsCode="${ODS_CODE:-}" \
    allowedGraphQLFqdns="${ALLOWED_GRAPHQL_FQDNS}" \
    resourceGroupMode="${RESOURCE_GROUP_MODE:-create}" \
    targetResourceGroupName="${TARGET_RESOURCE_GROUP_NAME:-}" \
    graphqlProcessJobConfiguration="${GRAPHQL_PROCESS_JOB_CONFIGURATION:-{\}}" \
    tagEnvironmentName="${AZURE_TAG_ENVIRONMENT_NAME:-}" \
    additionalTags="${AZURE_ADDITIONAL_TAGS:-{\}}" \
    graphQlUrl="${GRAPHQL_URL:-}" \
    graphQlUseAuth="${GRAPHQL_USE_AUTH:-false}"
```

## Run the infrastructure deploy

Use the values checked in [Infrastructure deployment variables](#infrastructure-deployment-variables).

Review the what-if output before running the deployment. This command deploys
`infra/stacks/api-batch-processor/subscription.bicep` directly into the target subscription.

The first infrastructure deployment can fail if the container app images do not exist in Azure Container Registry yet.
If that happens, publish the API and GraphQL processor images, then run
[Redeploy infrastructure after publishing images](#redeploy-infrastructure-after-publishing-images).

```bash
if [ -z "${STACK_RESOURCE_GROUP:-}" ]; then
  echo "ERROR: STACK_RESOURCE_GROUP is not set. Please run the setup block first."
  exit 1
fi

TEMPLATE_FILE="infra/stacks/api-batch-processor/subscription.bicep"
DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]' | tr -d ' ')-deploy"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment sub create \
  --name "${DEPLOYMENT_NAME}" \
  --location "${AZURE_LOCATION}" \
  --template-file "${TEMPLATE_FILE}" \
  --parameters \
    environmentName="${AZURE_ENV_NAME}" \
    environmentPrefix="${AZURE_ENV_PREFIX}" \
    location="${AZURE_LOCATION}" \
    monitoringActionGroupEmail="${AZURE_MONITORING_ACTION_GROUP_EMAIL}" \
    containerAppManagedEnvironmentNumber="${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}" \
    containerAppVnet="${AZURE_CONTAINER_APP_VNET}" \
    containerAppEnvSubnet="${AZURE_CONTAINER_APP_ENV_SUBNET}" \
    containerAppPeSubnet="${AZURE_CONTAINER_APP_PE_SUBNET}" \
    includeRoleAssignments="${AZURE_INCLUDE_ROLE_ASSIGNMENTS}" \
    turnOnAlerts="${AZURE_TURN_ON_ALERTS}" \
    graphqlProcessJobImageTag="${GRAPHQL_PROCESS_JOB_IMAGE_TAG}" \
    matchingApiImageTag="${MATCHING_API_IMAGE_TAG}" \
    externalApiImageTag="${EXTERNAL_API_IMAGE_TAG}" \
    pdsEmulatorImageTag="${PDS_EMULATOR_IMAGE_TAG}" \
    usePdsEmulator="${USE_PDS_EMULATOR}" \
    deploymentMode="${DEPLOYMENT_MODE}" \
    cronExpression="${CRON_EXPRESSION}" \
    odsCode="${ODS_CODE:-}" \
    allowedGraphQLFqdns="${ALLOWED_GRAPHQL_FQDNS}" \
    resourceGroupMode="${RESOURCE_GROUP_MODE:-create}" \
    targetResourceGroupName="${TARGET_RESOURCE_GROUP_NAME:-}" \
    graphqlProcessJobConfiguration="${GRAPHQL_PROCESS_JOB_CONFIGURATION:-{\}}" \
    tagEnvironmentName="${AZURE_TAG_ENVIRONMENT_NAME:-}" \
    additionalTags="${AZURE_ADDITIONAL_TAGS:-{\}}" \
    graphQlUrl="${GRAPHQL_URL:-}" \
    graphQlUseAuth="${GRAPHQL_USE_AUTH:-false}"
```

## Troubleshooting policy or deployment errors

If the `what-if` or `deploy` command is rejected due to a "RequestDisallowedByPolicy" error or fails unexpectedly, it might be due to missing tags or naming conventions on your Azure subscription.

To see exactly what is failing policy, you can view the errors directly using this script:

```bash
DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]' | tr -d ' ')-deploy"
az deployment sub operation list \
  --name "${DEPLOYMENT_NAME}" \
  --query "[?properties.provisioningState=='Failed'].{Resource:properties.targetResource.resourceName, Type:properties.targetResource.resourceType, Error:properties.statusMessage.error.message}" \
  --output table
```

Alternatively, to output the full debug trace payload and search for policy errors, append `--debug 2>&1 | grep "RequestDisallowedByPolicy"` to the end of the `az deployment sub create` command, which will filter the immense debug log to only standard policy rejections.

## Add the secrets to Key Vault

After the infrastructure deployment has created the Key Vault, manually add the secrets.

The deployment output includes `SECRETS_VAULT_NAME`. Add these secrets to that Key Vault:

- `nhs-digital-client-id`: the NHS Digital API key value.
- `nhs-digital-private-key`: the private key PEM that matches the public key uploaded to the NHS Digital application.
- `nhs-digital-kid`: the key ID for the uploaded NHS Digital public key.
- `GraphQlProcessJob--ClientId`: The client ID for connecting to the graphQL instance
- `GraphQlProcessJob--ClientSecret`: The client secret for connecting to the graphQL instance
- `GraphQlProcessJob--Scope`: The scope for connecting to the graphQL instance
- `GraphQlProcessJob--TenantId`: The tenant ID for connecting to the graphQL instance
- `GraphQLProcessJob--KnownSafeguardingConcernWorklistDefinitionId`: The Id to filter results by

These secrets can be added later if needed, but the external API app will not start successfully until all three secrets
exist in Key Vault.

## Publish the API images to Azure Container Registry

Use this section when you only need to publish the external API and matching API images.

If you are starting here, run [Common setup](#common-setup) first, or at least run the short setup block in
[Required common variables](#required-common-variables).

Required variables for this task:

- `AZURE_ENV_NAME`
- `AZURE_ENV_PREFIX`
- `AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER`
- `AZURE_SUBSCRIPTION_ID`
- `STACK_RESOURCE_GROUP`

The infrastructure deployment must already have created the Azure Container Registry and Container Apps environment.

This mirrors `.github/workflows/gh-api-batch-processor-api-images.yml`. The images are built in Azure Container
Registry using `az acr build`, and each API image is tagged with the current Git commit hash and `latest` tag.

```bash
LOWERCASE_ENVIRONMENT_NAME="$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')"
ACR_NAME="$(printf '%s' "${AZURE_ENV_PREFIX}${LOWERCASE_ENVIRONMENT_NAME}abpacr01" | tr -d '-')"
CONTAINER_APPS_ENVIRONMENT_NAME="${AZURE_ENV_PREFIX}-${LOWERCASE_ENVIRONMENT_NAME}-abp-cae-${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}"
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
EXTERNAL_API_IMAGE_REPOSITORY="external-api"
MATCHING_API_IMAGE_REPOSITORY="matching-api"

az config set extension.use_dynamic_install=yes_without_prompt


ACR_LOGIN_SERVER="$(az acr show \
  --name "${ACR_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query loginServer \
  --output tsv)"

CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN="$(az containerapp env show \
  --name "${CONTAINER_APPS_ENVIRONMENT_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query properties.defaultDomain \
  --output tsv)"

EXTERNAL_API_HTTP_ENDPOINT="http://external-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULTDomain:-${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}}"
EXTERNAL_API_HTTPS_ENDPOINT="https://external-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULTDomain:-${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}}"

echo "ACR: ${ACR_LOGIN_SERVER}"
echo "External API image tag: ${IMAGE_TAG}"
echo "Matching API image tag: ${IMAGE_TAG}"
echo "External API endpoint for matching API: ${EXTERNAL_API_HTTPS_ENDPOINT}"
```

Build the external API image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${EXTERNAL_API_IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${EXTERNAL_API_IMAGE_REPOSITORY}:latest" \
  --file src/external-api/Dockerfile \
  --build-arg ASPNETCORE_ENVIRONMENT="${AZURE_ENV_NAME}" \
  .
```

Build the matching API image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${MATCHING_API_IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${MATCHING_API_IMAGE_REPOSITORY}:latest" \
  --file src/matching-api/Dockerfile \
  --build-arg ASPNETCORE_ENVIRONMENT="${AZURE_ENV_NAME}" \
  --build-arg EXTERNAL_API_HTTP_ENDPOINT="${EXTERNAL_API_HTTP_ENDPOINT}" \
  --build-arg EXTERNAL_API_HTTPS_ENDPOINT="${EXTERNAL_API_HTTPS_ENDPOINT}" \
  .
```

Check that both repositories contain the current Git commit hash and `latest` tags:

```bash
az acr repository show-tags \
  --name "${ACR_NAME}" \
  --repository "${EXTERNAL_API_IMAGE_REPOSITORY}" \
  --output table

az acr repository show-tags \
  --name "${ACR_NAME}" \
  --repository "${MATCHING_API_IMAGE_REPOSITORY}" \
  --output table
```

Use this tag when you redeploy infrastructure after publishing images:

```bash
export EXTERNAL_API_IMAGE_TAG="${IMAGE_TAG}"
export MATCHING_API_IMAGE_TAG="${IMAGE_TAG}"
```

## Publish the GraphQL processor image to Azure Container Registry

Use this section when you only need to publish the GraphQL processor event job image.

If you are starting here, run [Common setup](#common-setup) first, or at least run the short setup block in
[Required common variables](#required-common-variables).

Required variables for this task:

- `AZURE_ENV_NAME`
- `AZURE_ENV_PREFIX`
- `AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER`
- `AZURE_SUBSCRIPTION_ID`
- `STACK_RESOURCE_GROUP`

The API images must already have been published, and the infrastructure deployment must already have created the Azure
Container Registry and Container Apps environment.

This mirrors `.github/workflows/gh-api-batch-processor-graphql-job-image.yml`. The image is built in Azure Container
Registry using `az acr build`, and the GraphQL processor image is tagged with the current Git commit hash and `latest`.

```bash
LOWERCASE_ENVIRONMENT_NAME="$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')"
ACR_NAME="$(printf '%s' "${AZURE_ENV_PREFIX}${LOWERCASE_ENVIRONMENT_NAME}abpacr01" | tr -d '-')"
CONTAINER_APPS_ENVIRONMENT_NAME="${AZURE_ENV_PREFIX}-${LOWERCASE_ENVIRONMENT_NAME}-abp-cae-${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}"
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
IMAGE_REPOSITORY="sui-client-graphql-process-job"

az config set extension.use_dynamic_install=yes_without_prompt


ACR_LOGIN_SERVER="$(az acr show \
  --name "${ACR_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query loginServer \
  --output tsv)"

echo "ACR: ${ACR_LOGIN_SERVER}"
echo "GraphQL processor image tag: ${IMAGE_TAG}"
```

Build the GraphQL processor image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${IMAGE_REPOSITORY}:latest" \
  --file src/SUI.Client/SUI.Client.GraphQLProcessJob/Dockerfile \
  .
```

`GRAPHQL_PROCESS_JOB_CONFIGURATION` is runtime deployment configuration. Obtain the protected configuration from the approved external runbook; do not add its values to this repository or image build command.

Check that the repository contains the current Git commit hash and `latest` tags:

```bash
az acr repository show-tags \

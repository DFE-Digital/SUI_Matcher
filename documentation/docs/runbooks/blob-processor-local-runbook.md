# Blob processor local run book

## Purpose

This run book explains how to deploy the blob processor stack from a laptop into an existing Azure resource group using
Azure CLI. It is written for repeatable local deployments, including cases where you only need to publish the API images
or the storage processor event job image.

## Prerequisites

- Azure CLI (`az`) installed.
- Permission to deploy to the target Azure subscription and existing resource group.
- Bash, WSL, or Git Bash for the examples below.
- A clean checkout of the branch, tag, or commit you intend to deploy.

## Choose your task

- Full first-time deployment: follow [Full deployment flow](#full-deployment-flow).
- Infrastructure only: follow [Common setup](#common-setup), then
  [Infrastructure deployment variables](#infrastructure-deployment-variables),
  [Run the infrastructure what-if](#run-the-infrastructure-what-if) and
  [Run the infrastructure deploy](#run-the-infrastructure-deploy).
- API images only: follow [Publish the API images to Azure Container Registry](#publish-the-api-images-to-azure-container-registry).
- Storage processor event job image only: follow
  [Publish the storage processor image to Azure Container Registry](#publish-the-storage-processor-image-to-azure-container-registry).
- Final application deployment after publishing images: follow
  [Redeploy infrastructure after publishing images](#redeploy-infrastructure-after-publishing-images).
- Add optional reconciliation properties to logs: follow
  [Add optional properties to storage processor logs](#add-optional-properties-to-storage-processor-logs).
- Smoke test: follow [Smoke test the deployment](#smoke-test-the-deployment).

## Full deployment flow

1. [Pick a branch, tag or commit to deploy](#pick-a-branch-tag-or-commit-to-deploy).
2. [Run dotnet restore, build and tests](#run-dotnet-restore-build-and-tests).
3. [Common setup](#common-setup).
4. [Add optional properties to storage processor logs](#add-optional-properties-to-storage-processor-logs), if required.
5. [Infrastructure deployment variables](#infrastructure-deployment-variables).
6. [Run the infrastructure what-if](#run-the-infrastructure-what-if).
7. [Run the infrastructure deploy](#run-the-infrastructure-deploy).
8. [Add the NHS Digital secrets to Key Vault](#add-the-nhs-digital-secrets-to-key-vault).
9. [Publish the API images to Azure Container Registry](#publish-the-api-images-to-azure-container-registry).
10. [Publish the storage processor image to Azure Container Registry](#publish-the-storage-processor-image-to-azure-container-registry).
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

- `.github/workflows/gh-blob-event-processor-infra-deploy.yml`
- `.github/workflows/gh-blob-event-processor-api-images.yml`
- `.github/workflows/gh-blob-event-processor-storage-job-image.yml`

The GitHub workflow's `AZURE_CLIENT_ID` value is only needed for GitHub OIDC login. It is not needed when running
commands locally with an interactive `az login`.

Native PowerShell does not support `source .env` or the Bash examples as written. Use WSL or Git Bash, or translate the
setup into PowerShell syntax before running the Azure CLI commands.

Create a `.env-blob-event-processor` file in the repository root based on this template and fill in your target values:

```bash
# Target values.
AZURE_ENV_NAME="Prod"
AZURE_ENV_PREFIX="<environment-prefix>"
AZURE_TENANT_ID="<tenant-id>"
AZURE_SUBSCRIPTION_ID="<subscription-id>"
AZURE_LOCATION="<azure-region>"

# Existing resource group target.
RESOURCE_GROUP_MODE="existing"
TARGET_RESOURCE_GROUP_NAME="<existing-resource-group-name>"

# Infrastructure values.
AZURE_MONITORING_ACTION_GROUP_EMAIL="<monitoring-alert-email-address>"
AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER="<managed-environment-number>"
AZURE_CONTAINER_APP_VNET="<container-app-vnet-cidr>"
AZURE_CONTAINER_APP_ENV_SUBNET="<container-app-environment-subnet-cidr>"
AZURE_CONTAINER_APP_PE_SUBNET="<private-endpoint-subnet-cidr>"

# Workflow defaults. Change these only when the target deployment requires it.
AZURE_INCLUDE_ROLE_ASSIGNMENTS="true"
AZURE_TURN_ON_ALERTS="false"
STORAGE_PROCESS_JOB_IMAGE_TAG="latest"
MATCHING_API_IMAGE_TAG="latest"
EXTERNAL_API_IMAGE_TAG="latest"
STORAGE_ACCOUNT_MODE="create" # "create" or "existing"
EXISTING_STORAGE_ACCOUNT_NAME="" # Leave blank if it does not exist.
AZURE_TAG_ENVIRONMENT_NAME="" # Optional override for the Environment tag.
AZURE_ADDITIONAL_TAGS="{}" # Optional additional tags as a JSON object string.
# Obtain this protected JSON object from the approved external runbook.
STORAGE_PROCESS_JOB_CONFIGURATION='<protected-storage-process-job-configuration-json>'
```

`STORAGE_PROCESS_JOB_CONFIGURATION` is passed to the ACA job as runtime environment configuration. It must include the storage job processing mode and CSV mapping keys before the storage processor can process files. Keep deployment-specific source column names in the approved external runbook, not in this repository.

## Add optional properties to storage processor logs

Use this section when the reconciliation processor must include selected optional CSV fields in its structured logs.

`OptionalPropertiesLog` is configured through the same protected `STORAGE_PROCESS_JOB_CONFIGURATION` JSON object as the
storage job processing mode and CSV mappings. Add one indexed `OptionalPropertiesLog__Fields__<index>` entry for each
optional CSV column that can be logged.

Example shape:

```bash
STORAGE_PROCESS_JOB_CONFIGURATION='{
  "StorageProcessJob__ProcessingMode": "Reconciliation",
  "OptionalPropertiesLog__Fields__0": "free_school_meals",
  "OptionalPropertiesLog__Fields__1": "case_status"
}'
```

The field names must match the source CSV column names after excluding columns already mapped under
`CsvMatchData__ColumnMappings`. Matching is case-insensitive. If they do not match, then they will not be logged.

When configured, the reconciliation processor writes the selected values to the
`RECONCILIATION_OPTIONAL_PROPERTIES` log entry and adds them to the logging scope with an `Optional_` prefix. Fields not
listed in `OptionalPropertiesLog__Fields` are not included in that optional-properties log entry.

Load the variables into your terminal session, derive the common deployment values, and select the Azure subscription:

```bash
set -euo pipefail

source .env-blob-event-processor

STACK_RESOURCE_GROUP="${AZURE_ENV_PREFIX}-$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')-blob-event-processor"

if [ "${RESOURCE_GROUP_MODE}" = "existing" ]; then
  STACK_RESOURCE_GROUP="${TARGET_RESOURCE_GROUP_NAME}"
fi

az login --tenant "${AZURE_TENANT_ID}"
az account set --subscription "${AZURE_SUBSCRIPTION_ID}"
az account show --query "{subscription:name, tenantId:tenantId}" --output table
```

### Required common variables

The following variables must be set before starting any task from the middle of this run book:

- `AZURE_ENV_NAME`
- `AZURE_ENV_PREFIX`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_LOCATION`
- `RESOURCE_GROUP_MODE`
- `TARGET_RESOURCE_GROUP_NAME`
- `AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER`
- `STACK_RESOURCE_GROUP`

If you are starting from an API image, storage processor image, or infrastructure redeploy section, run this first:

```bash
set -euo pipefail
source .env-blob-event-processor

STACK_RESOURCE_GROUP="${AZURE_ENV_PREFIX}-$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')-blob-event-processor"

if [ "${RESOURCE_GROUP_MODE}" = "existing" ]; then
  STACK_RESOURCE_GROUP="${TARGET_RESOURCE_GROUP_NAME}"
fi

az login --tenant "${AZURE_TENANT_ID}"
az account set --subscription "${AZURE_SUBSCRIPTION_ID}"
az account show --query "{subscription:name, tenantId:tenantId}" --output table
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
- `STORAGE_PROCESS_JOB_IMAGE_TAG`
- `MATCHING_API_IMAGE_TAG`
- `EXTERNAL_API_IMAGE_TAG`
- `STORAGE_ACCOUNT_MODE`
- `EXISTING_STORAGE_ACCOUNT_NAME` when `STORAGE_ACCOUNT_MODE` is `existing`
- `STORAGE_PROCESS_JOB_CONFIGURATION`

Optional variables:

- `AZURE_TAG_ENVIRONMENT_NAME`
- `AZURE_ADDITIONAL_TAGS`

```bash
if [ "${RESOURCE_GROUP_MODE}" != "existing" ]; then
  echo "This run book targets an existing resource group. Set RESOURCE_GROUP_MODE to existing."
  exit 1
fi

if [ -z "${TARGET_RESOURCE_GROUP_NAME}" ]; then
  echo "TARGET_RESOURCE_GROUP_NAME must be set."
  exit 1
fi

if [ -z "${AZURE_CONTAINER_APP_PE_SUBNET}" ]; then
  echo "AZURE_CONTAINER_APP_PE_SUBNET must be set for blob-event-processor infrastructure deployments."
  exit 1
fi

if [ "${STORAGE_ACCOUNT_MODE}" = "existing" ] && [ -z "${EXISTING_STORAGE_ACCOUNT_NAME}" ]; then
  echo "EXISTING_STORAGE_ACCOUNT_NAME must be set when STORAGE_ACCOUNT_MODE is existing."
  exit 1
fi
```

## Run the infrastructure what-if

Run the what-if:

```bash
TEMPLATE_FILE="infra/stacks/blob-event-processor/main.bicep"
DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]')-what-if"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment group what-if \
  --name "${DEPLOYMENT_NAME}" \
  --resource-group "${TARGET_RESOURCE_GROUP_NAME}" \
  --subscription "${AZURE_SUBSCRIPTION_ID}" \
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
    storageProcessJobImageTag="${STORAGE_PROCESS_JOB_IMAGE_TAG}" \
    matchingApiImageTag="${MATCHING_API_IMAGE_TAG}" \
    externalApiImageTag="${EXTERNAL_API_IMAGE_TAG}" \
    storageAccountMode="${STORAGE_ACCOUNT_MODE}" \
    existingStorageAccountName="${EXISTING_STORAGE_ACCOUNT_NAME}" \
    storageProcessJobConfiguration="${STORAGE_PROCESS_JOB_CONFIGURATION}" \
    tagEnvironmentName="${AZURE_TAG_ENVIRONMENT_NAME}" \
    additionalTags="${AZURE_ADDITIONAL_TAGS}"
```

## Run the infrastructure deploy

Use the values checked in [Infrastructure deployment variables](#infrastructure-deployment-variables).

Review the what-if output before running the deployment. This command deploys
`infra/stacks/blob-event-processor/main.bicep` directly into the existing target resource group.

The first infrastructure deployment can fail if the container app images do not exist in Azure Container Registry yet.
If that happens, publish the API and storage processor images, then run
[Redeploy infrastructure after publishing images](#redeploy-infrastructure-after-publishing-images).

```bash
TEMPLATE_FILE="infra/stacks/blob-event-processor/main.bicep"
DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]')-deploy"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment group create \
  --name "${DEPLOYMENT_NAME}" \
  --resource-group "${TARGET_RESOURCE_GROUP_NAME}" \
  --subscription "${AZURE_SUBSCRIPTION_ID}" \
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
    storageProcessJobImageTag="${STORAGE_PROCESS_JOB_IMAGE_TAG}" \
    matchingApiImageTag="${MATCHING_API_IMAGE_TAG}" \
    externalApiImageTag="${EXTERNAL_API_IMAGE_TAG}" \
    storageAccountMode="${STORAGE_ACCOUNT_MODE}" \
    existingStorageAccountName="${EXISTING_STORAGE_ACCOUNT_NAME}" \
    storageProcessJobConfiguration="${STORAGE_PROCESS_JOB_CONFIGURATION}" \
    tagEnvironmentName="${AZURE_TAG_ENVIRONMENT_NAME}" \
    additionalTags="${AZURE_ADDITIONAL_TAGS}"
```

## Add the NHS Digital secrets to Key Vault

After the infrastructure deployment has created the Key Vault, manually add the NHS Digital secrets used by the external
API.

The deployment output includes `SECRETS_VAULT_NAME`. Add these secrets to that Key Vault:

- `nhs-digital-client-id`: the NHS Digital API key value.
- `nhs-digital-private-key`: the private key PEM that matches the public key uploaded to the NHS Digital application.
- `nhs-digital-kid`: the key ID for the uploaded NHS Digital public key.

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

This mirrors `.github/workflows/gh-blob-event-processor-api-images.yml`. The images are built in Azure Container
Registry using `az acr build`, and each API image is tagged with the current Git commit hash and `latest`.

```bash
LOWERCASE_ENVIRONMENT_NAME="$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')"
ACR_NAME="${AZURE_ENV_PREFIX}${LOWERCASE_ENVIRONMENT_NAME}bepacr01"
CONTAINER_APPS_ENVIRONMENT_NAME="${AZURE_ENV_PREFIX}-${LOWERCASE_ENVIRONMENT_NAME}-bep-cae-${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}"
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
EXTERNAL_API_IMAGE_REPOSITORY="external-api"
MATCHING_API_IMAGE_REPOSITORY="matching-api"

az config set extension.use_dynamic_install=yes_without_prompt

# Remove any hyphens from the ACR name.
ACR_NAME="$(printf '%s' "$ACR_NAME" | tr -d '-')"

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

EXTERNAL_API_HTTP_ENDPOINT="http://external-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}"
EXTERNAL_API_HTTPS_ENDPOINT="https://external-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}"

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

## Publish the storage processor image to Azure Container Registry

Use this section when you only need to publish the storage processor event job image.

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

This mirrors `.github/workflows/gh-blob-event-processor-storage-job-image.yml`. The image is built in Azure Container
Registry using `az acr build`, and the storage processor image is tagged with the current Git commit hash and `latest`.

```bash
LOWERCASE_ENVIRONMENT_NAME="$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')"
ACR_NAME="${AZURE_ENV_PREFIX}${LOWERCASE_ENVIRONMENT_NAME}bepacr01"
CONTAINER_APPS_ENVIRONMENT_NAME="${AZURE_ENV_PREFIX}-${LOWERCASE_ENVIRONMENT_NAME}-bep-cae-${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}"
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
IMAGE_REPOSITORY="sui-client-storage-process-job"

az config set extension.use_dynamic_install=yes_without_prompt

# Remove any hyphens from the ACR name.
ACR_NAME="$(printf '%s' "$ACR_NAME" | tr -d '-')"

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

MATCH_API_BASE_ADDRESS="https://matching-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}"

echo "ACR: ${ACR_LOGIN_SERVER}"
echo "Storage processor image tag: ${IMAGE_TAG}"
echo "Matching API base address: ${MATCH_API_BASE_ADDRESS}"
```

Build the storage processor image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${IMAGE_REPOSITORY}:latest" \
  --file src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile \
  --build-arg MATCH_API_BASE_ADDRESS="${MATCH_API_BASE_ADDRESS}" \
  .
```

CSV mappings and processing mode are runtime deployment configuration. Obtain the protected configuration from the approved external runbook; do not add its values to this repository or image build command.

Do not smoke test a newly published storage processor image until `STORAGE_PROCESS_JOB_CONFIGURATION` has been set for the target environment. The image no longer contains default source column mappings.

Check that the repository contains the current Git commit hash and `latest` tags:

```bash
az acr repository show-tags \
  --name "${ACR_NAME}" \
  --repository "${IMAGE_REPOSITORY}" \
  --output table
```

Use this tag when you redeploy infrastructure after publishing images:

```bash
export STORAGE_PROCESS_JOB_IMAGE_TAG="${IMAGE_TAG}"
```

## Redeploy infrastructure after publishing images

Run this section after publishing API or storage processor images. This updates the deployed Container Apps and event job
to use the image tags you just published.

If you are starting here, run [Common setup](#common-setup) first, or at least run the short setup block in
[Required common variables](#required-common-variables).

Required variables for this task:

- All [common variables](#required-common-variables).
- `AZURE_MONITORING_ACTION_GROUP_EMAIL`
- `AZURE_CONTAINER_APP_VNET`
- `AZURE_CONTAINER_APP_ENV_SUBNET`
- `AZURE_CONTAINER_APP_PE_SUBNET`
- `AZURE_INCLUDE_ROLE_ASSIGNMENTS`
- `AZURE_TURN_ON_ALERTS`
- `STORAGE_ACCOUNT_MODE`
- `EXISTING_STORAGE_ACCOUNT_NAME` when `STORAGE_ACCOUNT_MODE` is `existing`
- `STORAGE_PROCESS_JOB_CONFIGURATION`
- `EXTERNAL_API_IMAGE_TAG`
- `MATCHING_API_IMAGE_TAG`
- `STORAGE_PROCESS_JOB_IMAGE_TAG`

If you published all images from the same commit in the current shell, the image sections above export these values for
you. If you are starting a new shell, set them to the tag you want to deploy:

```bash
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
export EXTERNAL_API_IMAGE_TAG="${IMAGE_TAG}"
export MATCHING_API_IMAGE_TAG="${IMAGE_TAG}"
export STORAGE_PROCESS_JOB_IMAGE_TAG="${IMAGE_TAG}"
```

Then run [Run the infrastructure what-if](#run-the-infrastructure-what-if), review the output, and run
[Run the infrastructure deploy](#run-the-infrastructure-deploy).

## Smoke test the deployment

Place a CSV file in the blob storage account and check the storage processor logs to confirm that the file has been
processed.

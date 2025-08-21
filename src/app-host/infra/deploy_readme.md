# Deploying Infra and App (Laptop Edition)

This guide explains how to deploy the app infra and app from your laptop.

## Prerequisites
* Access to Azure subscription: Ensure you have access to an Azure subscription where you can deploy resources.
* Access to use CLI commands: Ensure you have the necessary permissions to run CLI commands to Azure.
* Azure Developer CLI Installed: Ensure the Azure Developer CLI (`azd`) is installed and authenticated. You can install it [here](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd).
* Azure CLI Installed: Ensure the Azure CLI is installed. You can install it [here](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).
* Resource Group: Verify that the target Azure resource group exists.
* Be logged into Azure with the required credentials in order to authenticate the cli.

## Configuring the Deployment

1. Set the proper environment variables:
```bash
export AZURE_ENV_NAME="<Enter Env Name>"
export AZURE_ENV_PREFIX="<Enter Env Prefix>"
export AZURE_TENANT_ID="<Enter Tenant ID>"
export AZURE_SUBSCRIPTION_ID="<Enter Sub ID>"
export AZURE_LOCATION="<Enter Location>"
export AZURE_RESOURCE_GROUP="<Enter Resource Group>"
export AZURE_MONITORING_ACTION_GROUP_EMAIL="<Enter Email>"
export AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER="<Enter Env No>"
export AZURE_CONTAINER_APP_VNET="<Enter Network>"
export AZURE_CONTAINER_APP_ENV_SUBNET="<Enter Subnet for Env Deployment>"
```

## Running the Deployment

1. Open a terminal and navigate to the directory `app-host/infra` that contains the `main.bicep` file.
2. Log in using a service principal: [Documentation](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/reference#azd-auth-login)
   ```bash
   az login
   ```
Select the right env. Then login via azd for good measure...
   ```
   azd login
    ```
3. Use what-if to preview what will be changed/deployed [Documentation](https://learn.microsoft.com/en-us/cli/azure/deployment/group?view=azure-cli-latest#az-deployment-group-what-if)
   ```bash
   az deployment group what-if --resource-group "${AZURE_RESOURCE_GROUP}" --template-file main.bicep --parameters environmentName="${AZURE_ENV_NAME}" environmentPrefix="${AZURE_ENV_PREFIX}" location="${AZURE_LOCATION}" monitoringActionGroupEmail="${AZURE_MONITORING_ACTION_GROUP_EMAIL}" containerAppManagedEnvironmentNumber="${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}" containerAppVnet="${AZURE_CONTAINER_APP_VNET}" containerAppEnvSubnet="${AZURE_CONTAINER_APP_ENV_SUBNET}"
   ```
   You will see a preview of the changes that will be made to your Azure resources. This is a good way to verify that the parameters are set correctly and that the deployment will proceed as expected.

4. Run the following command to provision the infrastructure using `azd`:

    ```bash
    azd provision --no-prompt --environment <your-environment-name>
    ```
   Replace `<your-environment-name>` with the name of your environment (e.g., `Integration`, ).

5. Deploy the application using the following command (need to move back to the app-host dir):

    ```bash
    cd ..
    azd deploy --no-prompt --environment <your-environment-name>
    ```

   This command will deploy the application to the specified environment.

   If you haven't run the infra deploy then certain values may not be set in your environment. For instance AZURE_CONTAINER_REGISTRY_ENDPOINT. You can find these values via the console.

6. Monitor the deployment progress in the terminal. If successful, you will see a message indicating that the deployment was completed.
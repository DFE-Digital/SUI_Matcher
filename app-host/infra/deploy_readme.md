# Deploying `main.bicep` Using Azure CLI

This guide explains how to deploy the `main.bicep` file locally using the Azure Developer CLI (`azd`). 
It also covers using the `main.parameters.json` file for environment-specific configurations.

## Prerequisites
* Access to Azure subscription: Ensure you have access to an Azure subscription where you can deploy resources.
* Access to use CLI commands: Ensure you have the necessary permissions to run CLI commands to Azure.
* Azure Developer CLI Installed: Ensure the Azure Developer CLI (`azd`) is installed and authenticated. You can install it [here](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd).
* Azure CLI Installed: Ensure the Azure CLI is installed. You can install it [here](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).
* Resource Group: Verify that the target Azure resource group exists.

## Configuring the Deployment

1. Update `main.parameters.json`  
   The file contains placeholder parameters values that need to be replaced with actual values. See the main.bicep file parameters for more details on what each is required for.

## Running the Deployment

1. Open a terminal and navigate to the directory `app-host` that contains the `main.bicep` file.
2. Log in using a service principal: [Documentation](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/reference#azd-auth-login)
   ```bash
   azd auth login --client-id <CLIENT_ID> --tenant-id <TENANT_ID> --client-secret <CLIENT_SECRET>
    ```
3. Use what-if to preview what will be changed/deployed [Documentation](https://learn.microsoft.com/en-us/cli/azure/deployment/group?view=azure-cli-latest#az-deployment-group-what-if)
   ```bash
   az deployment group what-if --resource-group <name of resource group> --template-file main.bicep --parameters @main.parameters.json
   ```
   You will see a preview of the changes that will be made to your Azure resources. This is a good way to verify that the parameters are set correctly and that the deployment will proceed as expected.

4. Run the following command to provision the infrastructure using `azd`:

    ```bash
    azd provision --no-prompt --environment <your-environment-name>
    ```
   Replace `<your-environment-name>` with the name of your environment (e.g., `Integration`).

5. Deploy the application using the following command:

    ```bash
    azd deploy --no-prompt --environment <your-environment-name>
    ```

   This command will deploy the application to the specified environment.

6. Monitor the deployment progress in the terminal. If successful, you will see a message indicating that the deployment was completed.
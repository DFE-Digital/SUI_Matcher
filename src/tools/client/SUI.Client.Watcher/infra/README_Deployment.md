# This is for deployment of additional

This is for additional infrastructure that sits outside of the app-host infrastructure. Designed to be able to run the full project E2E.

This document is a work in progress. More information will be added as we update and discover

## Deployment autoamated.

Run the client.bicep file from the pipeline to deploy most of the required infrastructure. This does not include all aspects as some needs to be added manually due to not having implicit references to the app host.

## Deployment manual.

There are a few steps to be able to get all the infrastructure running.

- Add a private endpoint between the client VM VNET and the managedEnvrionment (where the API containers are running).
- Add the route table '-rt-01' to the 'cae' VNETs subnet




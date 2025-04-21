# Infrastructure Files Explanation

## apiservice.tmpl.yaml

The `apiservice.tmpl.yaml` file is a template configuration file for deploying an API service to Azure Container Apps within an .NET Aspire application. It defines the configuration for a container app service named "apiservice" with the following key sections:

1. **API Version and Location**:

   - Uses the Azure Container Apps API version 2024-02-02-preview
   - Azure location is set from environment variables: `{{ .Env.AZURE_LOCATION }}`

2. **Identity Configuration**:

   - Sets up a UserAssigned managed identity for the container app
   - Uses `{{ .Env.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}` to reference a managed identity ID for accessing the Azure Container Registry
   - This managed identity is created in the Bicep infrastructure files (resources.bicep) and its ID is passed as an environment variable
   - The container also has access to this identity through the `AZURE_CLIENT_ID` environment variable: `{{ .Env.MANAGED_IDENTITY_CLIENT_ID }}`
   - This allows the application to authenticate to other Azure services without storing credentials in the application

3. **Environment Configuration**:

   - Links to an Azure Container Apps environment using `{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}`
   - Configures single revision mode
   - Sets up .NET runtime with data protection enabled

4. **Ingress Configuration**:

   - `external: false` means the API is not publicly accessible
   - Uses port 8080 by default
   - HTTP transport with insecure traffic allowed

5. **Registry Configuration**:

   - Configures access to an Azure Container Registry using environment variables
   - Uses managed identity for authentication to pull container images

6. **Container Configuration**:

   - Specifies the container image source using `{{ .Image }}`
   - Sets several environment variables for the container including managed identity, HTTP forwarding, and OpenTelemetry settings

7. **Scaling Configuration**:

   - Sets minimum replica count to 1

8. **Tags**:
   - Adds tags for identifying the service in Azure and in the Aspire dashboard

When deployed, the template placeholders are replaced with actual values from the deployment environment.

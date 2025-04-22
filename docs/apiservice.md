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

   ### Using an External User-Assigned Managed Identity Along with Existing One

   If you have a User-Assigned Managed Identity created outside this Aspire solution, you can assign it to your API service **in addition to** the existing managed identity:

   1. **Create or identify your external User-Assigned Managed Identity** in the Azure portal or using Azure CLI

   2. **Modify the `resources.bicep` file** to accept the external identity ID and client ID as parameters:

      ```bicep
      @description('Resource ID of an external User-Assigned Managed Identity to use alongside the default one')
      param externalManagedIdentityId string = ''
      @description('Client ID of the external User-Assigned Managed Identity')
      param externalManagedIdentityClientId string = ''

      // Add these as outputs
      output EXTERNAL_MANAGED_IDENTITY_ID string = externalManagedIdentityId
      output EXTERNAL_MANAGED_IDENTITY_CLIENT_ID string = externalManagedIdentityClientId
      ```

   3. **Update the `main.bicep` file** to accept and pass through the external identity parameters:

      ```bicep
      @description('Resource ID of an external User-Assigned Managed Identity to use alongside the default one')
      param externalManagedIdentityId string = ''
      @description('Client ID of the external User-Assigned Managed Identity')
      param externalManagedIdentityClientId string = ''

      // Add to resources module params
      module resources 'resources.bicep' = {
        // ...existing module properties...
        params: {
          // ...existing params...
          externalManagedIdentityId: externalManagedIdentityId
          externalManagedIdentityClientId: externalManagedIdentityClientId
        }
      }

      // Add these as outputs
      output EXTERNAL_MANAGED_IDENTITY_ID string = resources.outputs.EXTERNAL_MANAGED_IDENTITY_ID
      output EXTERNAL_MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.EXTERNAL_MANAGED_IDENTITY_CLIENT_ID
      ```

   4. **Update the `main.parameters.json` file** to include the external identity parameters:

      ```json
      {
        "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
        "contentVersion": "1.0.0.0",
        "parameters": {
          // ...existing parameters...
          "externalManagedIdentityId": {
            "value": "${EXTERNAL_MANAGED_IDENTITY_ID}"
          },
          "externalManagedIdentityClientId": {
            "value": "${EXTERNAL_MANAGED_IDENTITY_CLIENT_ID}"
          }
        }
      }
      ```

   5. **Modify the `apiservice.tmpl.yaml` file** to include your external identity:

      ```yaml
      identity:
        type: UserAssigned
        userAssignedIdentities:
          "{{ .Env.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}": {}
          "{{ .Env.EXTERNAL_MANAGED_IDENTITY_ID }}": {}
      ```

   6. **Add the environment variable** for your external identity:

      ```yaml
      env:
        - name: AZURE_CLIENT_ID
          value: { { .Env.MANAGED_IDENTITY_CLIENT_ID } }
        - name: EXTERNAL_IDENTITY_CLIENT_ID
          value: { { .Env.EXTERNAL_MANAGED_IDENTITY_CLIENT_ID } }
      ```

   7. **Set the environment variables** before running `azd up`:

      ```bash
      # Windows PowerShell
      $env:EXTERNAL_MANAGED_IDENTITY_ID="/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}"
      $env:EXTERNAL_MANAGED_IDENTITY_CLIENT_ID="{your-external-identity-client-id}"

      # Linux/macOS
      export EXTERNAL_MANAGED_IDENTITY_ID="/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}"
      export EXTERNAL_MANAGED_IDENTITY_CLIENT_ID="{your-external-identity-client-id}"
      ```

   8. **In your application code**, use the appropriate credential for each service:

      ```csharp
      // For services that need the default identity
      var defaultCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
      {
          ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
      });

      // For services that need the external identity
      var externalCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
      {
          ManagedIdentityClientId = Environment.GetEnvironmentVariable("EXTERNAL_IDENTITY_CLIENT_ID")
      });
      ```

   This approach allows you to leverage both the Aspire-created managed identity and your existing managed identity for different Azure resources.

   ### Using Only an External User-Assigned Managed Identity

   If you have a User-Assigned Managed Identity created outside this Aspire solution, you can still assign it to your API service by:

   1. Modifying the `resources.bicep` file to accept the external identity ID as a parameter instead of creating a new one
   2. Updating the `main.parameters.json` to include the resource ID of your external managed identity
   3. In the `apiservice.tmpl.yaml` file, you'll need to ensure the following sections use the external identity:
      ```yaml
      identity:
        type: UserAssigned
        userAssignedIdentities:
          [YOUR_EXTERNAL_IDENTITY_RESOURCE_ID]: {}
      ```
   4. You'll also need to update the container environment variables to use the Client ID of your external identity:
      ```yaml
      env:
        - name: AZURE_CLIENT_ID
          value: [YOUR_EXTERNAL_IDENTITY_CLIENT_ID]
      ```

   This approach allows you to leverage existing managed identities that might have already been granted permissions to other Azure resources in your environment.

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

using AspireDemo1.ServiceDefaults;
using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add Azure client factory with both managed identities
builder.Services.AddAzureClients(clientBuilder =>
{
    // Get the client IDs from environment variables
    var defaultManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
    var externalManagedIdentityClientId = Environment.GetEnvironmentVariable("EXTERNAL_IDENTITY_CLIENT_ID");

    // Configure the default identity credential options (used for Container Registry and other default resources)
    clientBuilder.UseCredential(options =>
    {
        if (!string.IsNullOrEmpty(defaultManagedIdentityClientId))
        {
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = defaultManagedIdentityClientId
            });
        }
        
        return new DefaultAzureCredential();
    });

    // If we have an external identity, register it as a credential that can be used by name
    if (!string.IsNullOrEmpty(externalManagedIdentityClientId))
    {
        // Register a named TokenCredential for the external identity
        builder.Services.AddSingleton(serviceProvider => 
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = externalManagedIdentityClientId
            }));
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Add a new endpoint to show the managed identity information
app.MapGet("/identities", () =>
{
    var defaultManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "Not configured";
    var externalManagedIdentityClientId = Environment.GetEnvironmentVariable("EXTERNAL_IDENTITY_CLIENT_ID") ?? "Not configured";

    return new
    {
        DefaultIdentity = new
        {
            ClientId = defaultManagedIdentityClientId,
            IsConfigured = !string.IsNullOrEmpty(defaultManagedIdentityClientId) && defaultManagedIdentityClientId != "Not configured"
        },
        ExternalIdentity = new
        {
            ClientId = externalManagedIdentityClientId,
            IsConfigured = !string.IsNullOrEmpty(externalManagedIdentityClientId) && externalManagedIdentityClientId != "Not configured"
        }
    };
})
.WithName("GetIdentities");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

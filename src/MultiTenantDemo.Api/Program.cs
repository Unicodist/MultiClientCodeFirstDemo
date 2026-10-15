using MultiTenantDemo.Api;
using MultiTenantDemo.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment()
    && !builder.Configuration.GetValue<bool>("Security:AcknowledgeUnauthenticatedTenantHeader"))
{
    throw new InvalidOperationException(
        "This POC API has no authentication and trusts the X-Tenant-Id header. Run it in the Development " +
        "environment, or set Security:AcknowledgeUnauthenticatedTenantHeader=true for an isolated test network.");
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddTenancy(builder.Configuration);

var app = builder.Build();

app.Logger.LogWarning(
    "Unauthenticated POC: tenant is taken from the X-Tenant-Id header without authorization. Do not expose this API publicly.");

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

public partial class Program;

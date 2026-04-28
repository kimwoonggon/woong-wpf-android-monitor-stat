using Woong.MonitorStack.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Devices;

var builder = WebApplication.CreateBuilder(args);
string monitorDbConnectionString = builder.Configuration.GetConnectionString("MonitorDb")
    ?? "Host=localhost;Database=woong_monitor;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<MonitorDbContext>(options => options.UseNpgsql(monitorDbConnectionString));
}

builder.Services.AddScoped<DeviceRegistrationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/devices/register", async (
    RegisterDeviceRequest request,
    DeviceRegistrationService registrations) =>
{
    DeviceRegistrationResponse response = await registrations.RegisterAsync(request, DateTimeOffset.UtcNow);

    return Results.Ok(response);
});

app.Run();

public partial class Program;

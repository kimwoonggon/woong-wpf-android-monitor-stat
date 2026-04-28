using Woong.MonitorStack.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Devices;

var builder = WebApplication.CreateBuilder(args);
string monitorDbConnectionString = builder.Configuration.GetConnectionString("MonitorDb")
    ?? "Host=localhost;Database=woong_monitor;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddDbContext<MonitorDbContext>(options => options.UseNpgsql(monitorDbConnectionString));
builder.Services.AddSingleton<DeviceRegistrationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/devices/register", (
    RegisterDeviceRequest request,
    DeviceRegistrationService registrations) =>
{
    DeviceRegistrationResponse response = registrations.Register(request, DateTimeOffset.UtcNow);

    return Results.Ok(response);
});

app.Run();

public partial class Program;

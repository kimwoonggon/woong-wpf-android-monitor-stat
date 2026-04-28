using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Devices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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

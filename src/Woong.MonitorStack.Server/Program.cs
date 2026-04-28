using System.Globalization;
using Woong.MonitorStack.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Devices;
using Woong.MonitorStack.Server.Events;
using Woong.MonitorStack.Server.Sessions;
using Woong.MonitorStack.Server.Summaries;

var builder = WebApplication.CreateBuilder(args);
string monitorDbConnectionString = builder.Configuration.GetConnectionString("MonitorDb")
    ?? "Host=localhost;Database=woong_monitor;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<MonitorDbContext>(options => options.UseNpgsql(monitorDbConnectionString));
}

builder.Services.AddScoped<DeviceRegistrationService>();
builder.Services.AddScoped<FocusSessionUploadService>();
builder.Services.AddScoped<WebSessionUploadService>();
builder.Services.AddScoped<RawEventUploadService>();
builder.Services.AddScoped<DailySummaryQueryService>();

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

app.MapPost("/api/focus-sessions/upload", async (
    UploadFocusSessionsRequest request,
    FocusSessionUploadService uploads) =>
{
    UploadBatchResult response = await uploads.UploadAsync(request);

    return Results.Ok(response);
});

app.MapPost("/api/web-sessions/upload", async (
    UploadWebSessionsRequest request,
    WebSessionUploadService uploads) =>
{
    UploadBatchResult response = await uploads.UploadAsync(request);

    return Results.Ok(response);
});

app.MapPost("/api/raw-events/upload", async (
    UploadRawEventsRequest request,
    RawEventUploadService uploads) =>
{
    UploadBatchResult response = await uploads.UploadAsync(request);

    return Results.Ok(response);
});

app.MapGet("/api/daily-summaries/{summaryDate}", async (
    string summaryDate,
    string userId,
    string timezoneId,
    DailySummaryQueryService summaries) =>
{
    var parsedDate = DateOnly.Parse(summaryDate, CultureInfo.InvariantCulture);
    var response = await summaries.GetAsync(userId, parsedDate, timezoneId);

    return Results.Ok(response);
});

app.Run();

public partial class Program;

using System.Globalization;
using Microsoft.Extensions.Options;
using Woong.MonitorStack.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Components;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Devices;
using Woong.MonitorStack.Server.Dashboard;
using Woong.MonitorStack.Server.Events;
using Woong.MonitorStack.Server.Locations;
using Woong.MonitorStack.Server.Sessions;
using Woong.MonitorStack.Server.Summaries;

var builder = WebApplication.CreateBuilder(args);
string monitorDbConnectionString = builder.Configuration.GetConnectionString("MonitorDb")
    ?? "Host=localhost;Database=woong_monitor;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddRazorComponents();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<MonitorDbContext>(options => options.UseNpgsql(monitorDbConnectionString));
}

builder.Services.AddScoped<DeviceRegistrationService>();
builder.Services.AddSingleton<IValidateOptions<DeviceRegistrationAuthOptions>, DeviceRegistrationAuthOptionsValidator>();
builder.Services.AddOptions<DeviceRegistrationAuthOptions>()
    .Bind(builder.Configuration.GetSection(DeviceRegistrationAuthOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddHostedService<DeviceRegistrationAuthStartupGuard>();
builder.Services.AddScoped<HeaderRegistrationUserIdentitySource>();
builder.Services.AddScoped<ClaimsPrincipalRegistrationUserIdentitySource>();
builder.Services.AddScoped<IRegistrationUserIdentitySource, ConfiguredRegistrationUserIdentitySource>();
builder.Services.AddScoped<DeviceTokenAuthenticationService>();
builder.Services.AddScoped<FocusSessionUploadService>();
builder.Services.AddScoped<WebSessionUploadService>();
builder.Services.AddScoped<RawEventUploadService>();
builder.Services.Configure<RawEventRetentionOptions>(
    builder.Configuration.GetSection("RawEventRetention"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRawEventRetentionAlertSink, LoggingRawEventRetentionAlertSink>();
builder.Services.AddScoped<IRawEventRetentionService, RawEventRetentionService>();
builder.Services.AddScoped<IRawEventRetentionMaintenanceService, RawEventRetentionMaintenanceService>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<RawEventRetentionBackgroundService>();
}
builder.Services.AddScoped<LocationContextUploadService>();
builder.Services.AddScoped<DailySummaryQueryService>();
builder.Services.AddScoped<DailySummaryAggregationService>();
builder.Services.AddScoped<IntegratedDashboardQueryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAntiforgery();
app.MapRazorComponents<App>();

app.MapPost("/api/devices/register", async (
    RegisterDeviceRequest request,
    HttpRequest httpRequest,
    IRegistrationUserIdentitySource registrationUserIdentity,
    IOptions<DeviceRegistrationAuthOptions> registrationAuthOptions,
    DeviceRegistrationService registrations) =>
{
    string? authenticatedUserId = registrationUserIdentity.GetAuthenticatedUserId(httpRequest);
    if (registrationAuthOptions.Value.RequireAuthenticatedUser &&
        string.IsNullOrWhiteSpace(authenticatedUserId))
    {
        return Results.Unauthorized();
    }

    string effectiveUserId = authenticatedUserId ?? request.UserId;
    var effectiveRequest = new RegisterDeviceRequest(
        effectiveUserId,
        request.Platform,
        request.DeviceKey,
        request.DeviceName,
        request.TimezoneId);
    DeviceRegistrationResponse response = await registrations.RegisterAsync(effectiveRequest, DateTimeOffset.UtcNow);

    return Results.Ok(response);
});

app.MapPost("/api/devices/{deviceId}/token/rotate", async (
    string deviceId,
    HttpRequest httpRequest,
    DeviceTokenAuthenticationService deviceTokens,
    DeviceRegistrationService registrations) =>
{
    IResult? unauthorized = await RejectUnauthorizedDeviceTokenAsync(httpRequest, deviceId, deviceTokens);
    if (unauthorized is not null)
    {
        return unauthorized;
    }

    DeviceTokenRotationResponse? response = await registrations.RotateTokenAsync(deviceId);

    return response is null
        ? Results.NotFound()
        : Results.Ok(response);
});

app.MapPost("/api/devices/{deviceId}/token/revoke", async (
    string deviceId,
    HttpRequest httpRequest,
    DeviceTokenAuthenticationService deviceTokens,
    DeviceRegistrationService registrations) =>
{
    IResult? unauthorized = await RejectUnauthorizedDeviceTokenAsync(httpRequest, deviceId, deviceTokens);
    if (unauthorized is not null)
    {
        return unauthorized;
    }

    bool revoked = await registrations.RevokeTokenAsync(deviceId);

    return revoked
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapPost("/api/focus-sessions/upload", async (
    HttpRequest httpRequest,
    UploadFocusSessionsRequest request,
    DeviceTokenAuthenticationService deviceTokens,
    FocusSessionUploadService uploads) =>
{
    IResult? unauthorized = await RejectUnauthorizedDeviceTokenAsync(httpRequest, request.DeviceId, deviceTokens);
    if (unauthorized is not null)
    {
        return unauthorized;
    }

    UploadBatchResult response = await uploads.UploadAsync(request);

    return Results.Ok(response);
});

app.MapPost("/api/web-sessions/upload", async (
    HttpRequest httpRequest,
    UploadWebSessionsRequest request,
    DeviceTokenAuthenticationService deviceTokens,
    WebSessionUploadService uploads) =>
{
    IResult? unauthorized = await RejectUnauthorizedDeviceTokenAsync(httpRequest, request.DeviceId, deviceTokens);
    if (unauthorized is not null)
    {
        return unauthorized;
    }

    UploadBatchResult response = await uploads.UploadAsync(request);

    return Results.Ok(response);
});

app.MapPost("/api/raw-events/upload", async (
    HttpRequest httpRequest,
    UploadRawEventsRequest request,
    DeviceTokenAuthenticationService deviceTokens,
    RawEventUploadService uploads) =>
{
    IResult? unauthorized = await RejectUnauthorizedDeviceTokenAsync(httpRequest, request.DeviceId, deviceTokens);
    if (unauthorized is not null)
    {
        return unauthorized;
    }

    UploadBatchResult response = await uploads.UploadAsync(request);

    return Results.Ok(response);
});

app.MapPost("/api/location-contexts/upload", async (
    HttpRequest httpRequest,
    UploadLocationContextsRequest request,
    DeviceTokenAuthenticationService deviceTokens,
    LocationContextUploadService uploads) =>
{
    IResult? unauthorized = await RejectUnauthorizedDeviceTokenAsync(httpRequest, request.DeviceId, deviceTokens);
    if (unauthorized is not null)
    {
        return unauthorized;
    }

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

app.MapGet("/api/dashboard/integrated", async (
    string userId,
    string from,
    string to,
    string timezoneId,
    IntegratedDashboardQueryService dashboard) =>
{
    if (string.IsNullOrWhiteSpace(userId))
    {
        return BadRequest("Query parameter 'userId' is required.");
    }

    if (string.IsNullOrWhiteSpace(timezoneId))
    {
        return BadRequest("Query parameter 'timezoneId' is required.");
    }

    if (!TryParseIsoDate(from, out DateOnly fromDate))
    {
        return BadRequest("Query parameter 'from' must be an ISO date in yyyy-MM-dd format.");
    }

    if (!TryParseIsoDate(to, out DateOnly toDate))
    {
        return BadRequest("Query parameter 'to' must be an ISO date in yyyy-MM-dd format.");
    }

    if (toDate < fromDate)
    {
        return BadRequest("Query parameter 'to' must be on or after 'from'.");
    }

    if (!IsSupportedTimeZoneId(timezoneId))
    {
        return BadRequest("Query parameter 'timezoneId' is not supported.");
    }

    IntegratedDashboardSnapshot snapshot = await dashboard.GetAsync(userId, fromDate, toDate, timezoneId);

    return Results.Ok(snapshot);
});

static bool TryParseIsoDate(string value, out DateOnly date)
    => DateOnly.TryParseExact(
        value,
        "yyyy-MM-dd",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out date);

static bool IsSupportedTimeZoneId(string timezoneId)
{
    if (string.Equals(timezoneId, "UTC", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(timezoneId, "Etc/UTC", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    try
    {
        TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

        return true;
    }
    catch (TimeZoneNotFoundException) when (string.Equals(timezoneId, "Asia/Seoul", StringComparison.OrdinalIgnoreCase))
    {
        TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

        return true;
    }
    catch (InvalidTimeZoneException) when (string.Equals(timezoneId, "Asia/Seoul", StringComparison.OrdinalIgnoreCase))
    {
        TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

        return true;
    }
    catch (TimeZoneNotFoundException)
    {
        return false;
    }
    catch (InvalidTimeZoneException)
    {
        return false;
    }
}

static IResult BadRequest(string message)
    => Results.BadRequest(new { error = message });

static async Task<IResult?> RejectUnauthorizedDeviceTokenAsync(
    HttpRequest httpRequest,
    string deviceId,
    DeviceTokenAuthenticationService deviceTokens)
{
    string? deviceToken = httpRequest.Headers[DeviceTokenAuthenticationService.HeaderName].SingleOrDefault();
    var registrationUserIdentity = httpRequest.HttpContext.RequestServices.GetRequiredService<IRegistrationUserIdentitySource>();
    var registrationAuthOptions = httpRequest.HttpContext.RequestServices.GetRequiredService<IOptions<DeviceRegistrationAuthOptions>>();
    string? authenticatedUserId = registrationUserIdentity.GetAuthenticatedUserId(httpRequest);

    DeviceTokenAuthenticationStatus status = await deviceTokens.AuthenticateAsync(
        deviceId,
        deviceToken,
        authenticatedUserId,
        registrationAuthOptions.Value.RequireAuthenticatedUser);

    return status switch
    {
        DeviceTokenAuthenticationStatus.Authorized => null,
        DeviceTokenAuthenticationStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        _ => Results.Unauthorized()
    };
}

app.MapGet("/api/statistics/range", async (
    string userId,
    string from,
    string to,
    string timezoneId,
    DailySummaryQueryService summaries) =>
{
    if (string.IsNullOrWhiteSpace(userId))
    {
        return BadRequest("Query parameter 'userId' is required.");
    }

    if (string.IsNullOrWhiteSpace(timezoneId))
    {
        return BadRequest("Query parameter 'timezoneId' is required.");
    }

    if (!TryParseIsoDate(from, out DateOnly fromDate))
    {
        return BadRequest("Query parameter 'from' must be an ISO date in yyyy-MM-dd format.");
    }

    if (!TryParseIsoDate(to, out DateOnly toDate))
    {
        return BadRequest("Query parameter 'to' must be an ISO date in yyyy-MM-dd format.");
    }

    if (toDate < fromDate)
    {
        return BadRequest("Query parameter 'to' must be on or after 'from'.");
    }

    if (!IsSupportedTimeZoneId(timezoneId))
    {
        return BadRequest("Query parameter 'timezoneId' is not supported.");
    }

    var response = await summaries.GetRangeAsync(userId, fromDate, toDate, timezoneId);

    return Results.Ok(response);
});

app.Run();

public partial class Program;

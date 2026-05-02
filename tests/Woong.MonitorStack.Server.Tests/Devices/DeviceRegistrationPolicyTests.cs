using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Devices;

namespace Woong.MonitorStack.Server.Tests.Devices;

public sealed class DeviceRegistrationPolicyTests
{
    private const string AuthenticatedUserHeaderName = "X-Woong-User-Id";

    [Fact]
    public async Task RegisterDevice_WhenUserSessionAuthIsMissingAndStrictModeIsEnabled_ReturnsUnauthorized()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "payload-user",
            platform: Platform.Android,
            deviceKey: "android-policy-device-key",
            deviceName: "Phone",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.Devices.ToListAsync());
    }

    [Fact]
    public async Task RegisterDevice_UsesAuthenticatedUserIdInsteadOfPayloadUserId()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: false);
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "payload-user",
            platform: Platform.Android,
            deviceKey: "android-policy-auth-device-key",
            deviceName: "Phone",
            timezoneId: "Asia/Seoul");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/devices/register")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add(AuthenticatedUserHeaderName, "authenticated-user");

        HttpResponseMessage response = await client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DeviceRegistrationResponse body = (await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
        Assert.Equal("authenticated-user", body.UserId);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        DeviceEntity device = Assert.Single(await dbContext.Devices.ToListAsync());
        Assert.Equal("authenticated-user", device.UserId);
    }

    [Fact]
    public async Task RegisterDevice_WhenSameDeviceKeyIsRegisteredByDifferentAuthenticatedUsers_CreatesSeparateDevices()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();

        DeviceRegistrationResponse first = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "payload-user-a",
            deviceKey: "shared-android-device-key");
        DeviceRegistrationResponse second = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-b",
            payloadUserId: "payload-user-a",
            deviceKey: "shared-android-device-key");

        Assert.NotEqual(first.DeviceId, second.DeviceId);
        Assert.Equal("user-a", first.UserId);
        Assert.Equal("user-b", second.UserId);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Equal(2, await dbContext.Devices.CountAsync());
    }

    [Fact]
    public async Task RegisterDevice_WhenPayloadUserIdTargetsAnotherUser_DoesNotReturnExistingDeviceToken()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();

        DeviceRegistrationResponse first = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "user-a",
            deviceKey: "shared-android-device-key");
        DeviceRegistrationResponse second = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-b",
            payloadUserId: "user-a",
            deviceKey: "shared-android-device-key");

        Assert.NotEqual(first.DeviceId, second.DeviceId);
        Assert.NotEqual(first.DeviceToken, second.DeviceToken);
        Assert.Equal("user-b", second.UserId);
    }

    [Fact]
    public async Task UploadFocusSessions_WhenStrictModeDeviceTokenIsInvalid_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();
        DeviceRegistrationResponse registration = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "payload-user-a",
            deviceKey: "user-a-android-device-key");

        HttpResponseMessage response = await PostFocusUploadAsync(
            client,
            authenticatedUserId: "user-a",
            registration.DeviceId,
            "not-the-issued-device-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadFocusSessions_WhenStrictModeAuthenticatedUserDoesNotOwnValidToken_ReturnsForbiddenAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();
        DeviceRegistrationResponse registration = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "payload-user-a",
            deviceKey: "user-a-android-device-key-forbidden");

        HttpResponseMessage response = await PostFocusUploadAsync(
            client,
            authenticatedUserId: "user-b",
            registration.DeviceId,
            registration.DeviceToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public void Startup_WhenProductionStrictAuthUsesDefaultHeaderStubProvider_FailsConfigurationValidation()
    {
        ValidateOptionsResult result = ValidateProductionAuthOptions(new DeviceRegistrationAuthOptions
        {
            RequireAuthenticatedUser = true,
            UserIdentityProviderMode = DeviceRegistrationAuthOptions.HeaderStubProviderMode
        });
        string failureMessage = GetFailureMessage(result);

        Assert.True(result.Failed);
        Assert.Contains("real user/session provider", failureMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HeaderStub", failureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_WhenProductionStrictAuthUsesUnwiredUserIdentityProviderMode_FailsConfigurationValidation()
    {
        ValidateOptionsResult result = ValidateProductionAuthOptions(new DeviceRegistrationAuthOptions
        {
            RequireAuthenticatedUser = true,
            UserIdentityProviderMode = "Oidc"
        });
        string failureMessage = GetFailureMessage(result);

        Assert.True(result.Failed);
        Assert.Contains("not wired", failureMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Oidc", failureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_WhenProductionStrictAuthUsesClaimsPrincipalProviderMode_PassesConfigurationValidation()
    {
        ValidateOptionsResult result = ValidateProductionAuthOptions(new DeviceRegistrationAuthOptions
        {
            RequireAuthenticatedUser = true,
            UserIdentityProviderMode = DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
            AuthenticatedUserClaimType = "sub",
            RequiredAuthenticationScheme = "Bearer"
        });

        Assert.False(result.Failed, GetFailureMessage(result));
    }

    [Fact]
    public void Startup_WhenProductionStrictAuthUsesClaimsPrincipalWithoutAuthenticationScheme_FailsConfigurationValidation()
    {
        ValidateOptionsResult result = ValidateProductionAuthOptions(new DeviceRegistrationAuthOptions
        {
            RequireAuthenticatedUser = true,
            UserIdentityProviderMode = DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
            AuthenticatedUserClaimType = "sub"
        });
        string failureMessage = GetFailureMessage(result);

        Assert.True(result.Failed);
        Assert.Contains("RequiredAuthenticationScheme", failureMessage, StringComparison.Ordinal);
        Assert.Contains("ClaimsPrincipal", failureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ClaimsPrincipalIdentitySource_ExtractsStableUserIdFromConfiguredClaim()
    {
        var source = new ClaimsPrincipalRegistrationUserIdentitySource(
            Options.Create(new DeviceRegistrationAuthOptions
            {
                UserIdentityProviderMode = DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
                AuthenticatedUserClaimType = "sub"
            }));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-123")],
                authenticationType: "test-auth"))
        };

        string? userId = source.GetAuthenticatedUserId(context.Request);

        Assert.Equal("user-123", userId);
    }

    [Fact]
    public void ClaimsPrincipalIdentitySource_WhenConfiguredClaimIsMissing_ReturnsNull()
    {
        var source = new ClaimsPrincipalRegistrationUserIdentitySource(
            Options.Create(new DeviceRegistrationAuthOptions
            {
                UserIdentityProviderMode = DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
                AuthenticatedUserClaimType = "sub"
            }));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("email", "user@example.com")],
                authenticationType: "test-auth"))
        };

        string? userId = source.GetAuthenticatedUserId(context.Request);

        Assert.Null(userId);
    }

    [Fact]
    public void ConfiguredIdentitySource_WhenClaimsModeIsSelected_IgnoresHeaderStubValue()
    {
        var options = Options.Create(new DeviceRegistrationAuthOptions
        {
            UserIdentityProviderMode = DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
            AuthenticatedUserHeaderName = AuthenticatedUserHeaderName,
            AuthenticatedUserClaimType = "sub"
        });
        var source = new ConfiguredRegistrationUserIdentitySource(
            options,
            new HeaderRegistrationUserIdentitySource(options),
            new ClaimsPrincipalRegistrationUserIdentitySource(options));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "claims-user")],
                authenticationType: "test-auth"))
        };
        context.Request.Headers.Append(AuthenticatedUserHeaderName, "header-stub-user");

        string? userId = source.GetAuthenticatedUserId(context.Request);

        Assert.Equal("claims-user", userId);
    }

    [Fact]
    public void ServerRegistrationPolicy_DoesNotReferenceWindowsOrAndroidLocalDatabases()
    {
        string repositoryRoot = FindRepositoryRoot();
        string serverSourceRoot = Path.Combine(repositoryRoot, "src", "Woong.MonitorStack.Server");
        string combinedServerSource = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(serverSourceRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("Room", combinedServerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPreferences", combinedServerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AndroidSyncSettings", combinedServerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Windows local", combinedServerSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sqlite", combinedServerSource, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<DeviceRegistrationResponse> RegisterDeviceAsync(
        HttpClient client,
        string authenticatedUserId,
        string payloadUserId,
        string deviceKey)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/devices/register")
        {
            Content = JsonContent.Create(new RegisterDeviceRequest(
                userId: payloadUserId,
                platform: Platform.Android,
                deviceKey: deviceKey,
                deviceName: "Phone",
                timezoneId: "Asia/Seoul"))
        };
        httpRequest.Headers.Add(AuthenticatedUserHeaderName, authenticatedUserId);

        HttpResponseMessage response = await client.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
    }

    private static async Task<HttpResponseMessage> PostFocusUploadAsync(
        HttpClient client,
        string authenticatedUserId,
        string deviceId,
        string deviceToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/focus-sessions/upload")
        {
            Content = JsonContent.Create(new UploadFocusSessionsRequest(
                deviceId,
                [
                    new FocusSessionUploadItem(
                        clientSessionId: "cross-user-focus-session",
                        platformAppKey: "com.android.chrome",
                        startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
                        endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 5, 0, TimeSpan.Zero),
                        durationMs: 300_000,
                        localDate: new DateOnly(2026, 4, 28),
                        timezoneId: "Asia/Seoul",
                        isIdle: false,
                        source: "android_usage_stats")
                ]))
        };
        request.Headers.Add(AuthenticatedUserHeaderName, authenticatedUserId);
        request.Headers.Add(DeviceTokenAuthenticationService.HeaderName, deviceToken);

        return await client.SendAsync(request);
    }

    private static WebApplicationFactory<Program> CreateFactoryWithInMemoryDatabase(
        bool requireAuthenticatedUser,
        string environmentName = "Testing",
        string? userIdentityProviderMode = null)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environmentName);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var values = new Dictionary<string, string?>
                    {
                        ["DeviceRegistrationAuth:RequireAuthenticatedUser"] = requireAuthenticatedUser.ToString()
                    };
                    if (!string.IsNullOrWhiteSpace(userIdentityProviderMode))
                    {
                        values["DeviceRegistrationAuth:UserIdentityProviderMode"] = userIdentityProviderMode;
                    }

                    configuration.AddInMemoryCollection(values);
                });
                builder.ConfigureServices(services =>
                {
                    string databaseName = $"server-registration-policy-tests-{Guid.NewGuid():N}";
                    services.RemoveAll<DbContextOptions<MonitorDbContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.AddDbContext<MonitorDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });

    private static ValidateOptionsResult ValidateProductionAuthOptions(DeviceRegistrationAuthOptions options)
    {
        var validator = new DeviceRegistrationAuthOptionsValidator(new TestHostEnvironment("Production"));

        return validator.Validate(null, options);
    }

    private static string GetFailureMessage(ValidateOptionsResult result)
        => string.Join(" ", result.Failures ?? Array.Empty<string>());

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "Woong.MonitorStack.Server.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; }
    }
}

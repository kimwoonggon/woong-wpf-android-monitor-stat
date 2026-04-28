using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;

namespace Woong.MonitorStack.Server.Tests.Devices;

public sealed class DeviceRegistrationApiTests
{
    [Fact]
    public async Task RegisterDevice_ReturnsStableDeviceIdForSameDeviceKey()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Windows,
            deviceKey: "windows-device-key",
            deviceName: "Workstation",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/devices/register", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        string firstDeviceId = firstJson.RootElement.GetProperty("deviceId").GetString()!;
        string secondDeviceId = secondJson.RootElement.GetProperty("deviceId").GetString()!;

        Assert.False(string.IsNullOrWhiteSpace(firstDeviceId));
        Assert.Equal(firstDeviceId, secondDeviceId);
        Assert.Equal("windows-device-key", firstJson.RootElement.GetProperty("deviceKey").GetString());
        Assert.Equal("windows", firstJson.RootElement.GetProperty("platform").GetString());
        Assert.True(firstJson.RootElement.GetProperty("isNew").GetBoolean());
        Assert.False(secondJson.RootElement.GetProperty("isNew").GetBoolean());
    }
}

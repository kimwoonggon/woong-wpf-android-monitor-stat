using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;

namespace Woong.MonitorStack.Domain.Tests.Contracts;

public sealed class DeviceRegistrationContractTests
{
    [Fact]
    public void Constructor_RequiresStableDeviceKey()
    {
        Assert.Throws<ArgumentException>(() => new RegisterDeviceRequest(
            userId: "local-user",
            platform: Platform.Windows,
            deviceKey: "",
            deviceName: "Dev PC",
            timezoneId: "Asia/Seoul"));
    }
}

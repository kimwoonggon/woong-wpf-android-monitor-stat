using System.Text.Json;

namespace Woong.MonitorStack.Windows.Sync;

public interface IWindowsSyncRegistrationStore
{
    WindowsSyncRegistration? GetRegistration();

    void SaveRegistration(WindowsSyncRegistration registration);

    void ClearRegistration();
}

public sealed record WindowsSyncRegistration(string ServerDeviceId)
{
    public string ServerDeviceId { get; } = string.IsNullOrWhiteSpace(ServerDeviceId)
        ? throw new ArgumentException("Server device id must not be empty.", nameof(ServerDeviceId))
        : ServerDeviceId.Trim();
}

public sealed class FileWindowsSyncRegistrationStore : IWindowsSyncRegistrationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public FileWindowsSyncRegistrationStore(string registrationFilePath)
    {
        RegistrationFilePath = string.IsNullOrWhiteSpace(registrationFilePath)
            ? throw new ArgumentException("Registration file path must not be empty.", nameof(registrationFilePath))
            : registrationFilePath;
    }

    public string RegistrationFilePath { get; }

    public WindowsSyncRegistration? GetRegistration()
    {
        if (!File.Exists(RegistrationFilePath))
        {
            return null;
        }

        string json = File.ReadAllText(RegistrationFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        WindowsSyncRegistrationFile? file = JsonSerializer.Deserialize<WindowsSyncRegistrationFile>(
            json,
            JsonOptions);
        return string.IsNullOrWhiteSpace(file?.ServerDeviceId)
            ? null
            : new WindowsSyncRegistration(file.ServerDeviceId);
    }

    public void SaveRegistration(WindowsSyncRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        string? directory = Path.GetDirectoryName(RegistrationFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var file = new WindowsSyncRegistrationFile(registration.ServerDeviceId);
        File.WriteAllText(RegistrationFilePath, JsonSerializer.Serialize(file, JsonOptions));
    }

    public void ClearRegistration()
    {
        if (File.Exists(RegistrationFilePath))
        {
            File.Delete(RegistrationFilePath);
        }
    }

    private sealed record WindowsSyncRegistrationFile(string ServerDeviceId);
}

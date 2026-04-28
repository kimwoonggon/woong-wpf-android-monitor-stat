using Woong.MonitorStack.Windows.Tracking;

var clock = new SystemClock();
var foregroundCollector = new ForegroundWindowCollector(
    new WindowsForegroundWindowReader(),
    clock);
var lastInputReader = new WindowsLastInputReader();
var idleDetector = new IdleDetector(TimeSpan.FromMinutes(5));

var snapshot = foregroundCollector.Capture();
var lastInputAtUtc = lastInputReader.ReadLastInputAtUtc(snapshot.TimestampUtc);
var isIdle = idleDetector.IsIdle(snapshot.TimestampUtc, lastInputAtUtc);

Console.WriteLine($"TimestampUtc: {snapshot.TimestampUtc:O}");
Console.WriteLine($"Hwnd: {snapshot.Hwnd}");
Console.WriteLine($"ProcessId: {snapshot.ProcessId}");
Console.WriteLine($"ProcessName: {snapshot.ProcessName}");
Console.WriteLine($"ExecutablePath: {snapshot.ExecutablePath}");
Console.WriteLine($"WindowTitle: {snapshot.WindowTitle}");
Console.WriteLine($"LastInputAtUtc: {lastInputAtUtc:O}");
Console.WriteLine($"IsIdle: {isIdle}");

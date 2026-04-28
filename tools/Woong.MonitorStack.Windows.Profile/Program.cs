using System.Diagnostics;
using Woong.MonitorStack.Windows.Tracking;

var durationSeconds = args.Length > 0 && int.TryParse(args[0], out var parsedDuration)
    ? parsedDuration
    : 30;
var intervalMilliseconds = args.Length > 1 && int.TryParse(args[1], out var parsedInterval)
    ? parsedInterval
    : 500;

var clock = new SystemClock();
var poller = new TrackingPoller(
    new ForegroundWindowCollector(new WindowsForegroundWindowReader(), clock),
    new WindowsLastInputReader(),
    new IdleDetector(TimeSpan.FromMinutes(5)),
    new FocusSessionizer("profile-device", TimeZoneInfo.Local.Id));

using var process = Process.GetCurrentProcess();
var startCpu = process.TotalProcessorTime;
var stopwatch = Stopwatch.StartNew();
var deadline = TimeSpan.FromSeconds(durationSeconds);
var polls = 0;
var closedSessions = 0;
long peakWorkingSet = process.WorkingSet64;

while (stopwatch.Elapsed < deadline)
{
    var result = poller.Poll();
    polls++;
    if (result.ClosedSession is not null)
    {
        closedSessions++;
    }

    process.Refresh();
    peakWorkingSet = Math.Max(peakWorkingSet, process.WorkingSet64);
    Thread.Sleep(intervalMilliseconds);
}

process.Refresh();
var cpuMs = (process.TotalProcessorTime - startCpu).TotalMilliseconds;
var peakWorkingSetMb = peakWorkingSet / 1024d / 1024d;

Console.WriteLine($"DurationSeconds: {durationSeconds}");
Console.WriteLine($"IntervalMilliseconds: {intervalMilliseconds}");
Console.WriteLine($"Polls: {polls}");
Console.WriteLine($"ClosedSessions: {closedSessions}");
Console.WriteLine($"CpuMs: {cpuMs:F2}");
Console.WriteLine($"PeakWorkingSetMb: {peakWorkingSetMb:F2}");

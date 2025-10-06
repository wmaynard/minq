using System;
using System.Timers;
using Maynard.Json;
using Maynard.Logging;
using Maynard.Minq.Models;

namespace Maynard.Minq.Singletons;

public abstract class MongoTimerSingleton<T> : MongoSingleton<T>, IDisposable where T : MinqDocument
{
    private readonly Timer _timer;
    protected double IntervalMs { get; init; }
    public bool IsRunning => _timer.Enabled;
    public string Status => IsRunning ? "running" : "stopped";

    protected MongoTimerSingleton(string collection, double intervalMs, bool startImmediately = true) : base(collection)
    {
        IntervalMs = intervalMs;
        _timer = new(IntervalMs);
        _timer.Elapsed += (_, _) =>
        {
            Pause();
            try
            {
                OnElapsed();
            }
            catch (Exception e)
            {
                Log.Error($"{GetType().Name}.OnElapsed failed.", exception: e);
            }
            Resume();
        };
        if (startImmediately)
            _timer.Start();
    }

    protected void Pause() => _timer.Stop();
    protected void Resume() => _timer.Start();
    protected abstract void OnElapsed();

    public override FlexJson HealthStatus => new FlexJson
    {
        { Name, Status }
    };

    public void Dispose() => _timer?.Dispose();
}
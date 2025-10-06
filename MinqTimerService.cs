using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Maynard.Logging;
using Maynard.Minq.Models;
using Maynard.Time;

namespace Maynard.Minq;

public abstract class MinqTimerService<T> : Minq<T> where T : MinqDocument, new()
{
    private const int ONE_HOUR = 3_600_000;
    private const int FAILURE_TOLERANCE = 100;
    private Timer Timer { get; init; }
    private List<long> Failures { get; set; }
    
    private bool CoolingOff { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="interval">The interval, in MS, for how often the service should run.</param>
    /// <param name="collection"></param>
    protected MinqTimerService(string collection, double interval = 300_000) : base(collection)
    {
        Failures = new List<long>();
        Timer = new Timer(interval);
        Timer.Elapsed += OnElapsed;
        Timer.Start();
    }

    private void OnElapsed(object sender, ElapsedEventArgs args)
    {
        Timer.Stop();
        try
        {
            Failures.RemoveAll(failure => failure < Timestamp.Now - ONE_HOUR);
            if (CoolingOff)
                CoolingOff = Failures.Any();
            if (!CoolingOff)
                OnElapsed();
        }
        catch (Exception e)
        {
            Log.Error("MinqTimerService.OnElapsed failed.", exception: e);

            Failures.Add(Timestamp.Now);
            CoolingOff = Failures.Count > FAILURE_TOLERANCE;

            if (CoolingOff)
                Log.Error($"{GetType().Name} has hit enough errors to enter a cooldown!  It will restart in one hour.");
        }
        Timer.Start();
    }

    protected abstract void OnElapsed();
}
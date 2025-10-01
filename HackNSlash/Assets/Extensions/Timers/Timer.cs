using System;
using UnityEngine;

namespace Extensions.Timers
{
    public abstract class Timer : IDisposable
    {
        public float CurrentTime { get; protected set; }
        public bool IsRunning { get; private set; }

        protected float initialTime;
        
        protected bool UseUnscaledTime;
        protected float DeltaTime => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        public float Progress => Mathf.Clamp(CurrentTime / initialTime, 0, 1);

        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };

        protected Timer(float value, bool unscaled = false)
        {
            initialTime = value;
            UseUnscaledTime = unscaled;
        }

        public void Start()
        {
            CurrentTime = initialTime;
            if (!IsRunning)
            {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                OnTimerStart.Invoke();
            }
        }

        public void Stop()
        {
            CurrentTime = 0;
            
            if (IsRunning)
            {
                IsRunning = false;
                TimerManager.DeregisterTimer(this);
                OnTimerStop.Invoke();
            }
        }

        public abstract void Tick();
        public abstract bool IsFinished { get; }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public virtual void Reset() => CurrentTime = initialTime;

        public virtual void Reset(float newTime)
        {
            initialTime = newTime;
            Reset();
        }

        bool disposed;

        ~Timer()
        {
            Dispose(false);
        }

        // Call Dispose to ensure de-registration of the timer from the TimerManager
        // when the consumer is done with the timer or being destroyed
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                TimerManager.DeregisterTimer(this);
            }

            disposed = true;
        }
    }

    /**
     * <summary>
     * A countdown timer that counts down from a specified time to zero.
     * </summary>
     */
    public class CountdownTimer : Timer
    {
        public float ElapsedTime => initialTime - CurrentTime;
        
        public CountdownTimer(float value, bool unscaled = false) : base(value, unscaled)
        {
        }

        public override void Tick()
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= DeltaTime;
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0;
    }


    /**
     * <summary>
     * A frequency timer that triggers an action at a specified number of ticks per second.
     * </summary>
     */
    public class FrequencyTimer : Timer
    {
        public int TicksPerSecond { get; private set; }

        public Action OnTick = delegate { };

        float timeThreshold;

        public FrequencyTimer(int ticksPerSecond, bool unscaled = false) : base(0, unscaled)
        {
            CalculateTimeThreshold(ticksPerSecond);
        }

        public override void Tick()
        {
            if (IsRunning && CurrentTime >= timeThreshold)
            {
                CurrentTime -= timeThreshold;
                OnTick.Invoke();
            }

            if (IsRunning && CurrentTime < timeThreshold)
            {
                CurrentTime += DeltaTime;
            }
        }

        public override bool IsFinished => !IsRunning;

        public override void Reset()
        {
            CurrentTime = 0;
        }

        public void Reset(int newTicksPerSecond)
        {
            CalculateTimeThreshold(newTicksPerSecond);
            Reset();
        }

        void CalculateTimeThreshold(int ticksPerSecond)
        {
            TicksPerSecond = ticksPerSecond;
            timeThreshold = 1f / TicksPerSecond;
        }
    }


    /**
     * <summary>
     * A timer that triggers an action at regular intervals while counting down from a specified time to zero.
     * </summary>
     */
    public class IntervalTimer : Timer
    {
        readonly float interval;
        float nextInterval;

        public Action OnInterval = delegate { };

        public IntervalTimer(float totalTime, float intervalSeconds, bool unscaled = false) : base(totalTime, unscaled)
        {
            interval = intervalSeconds;
            nextInterval = totalTime - interval;
        }

        public override void Tick()
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= DeltaTime;

                // Fire interval events as long as thresholds are crossed
                while (CurrentTime <= nextInterval && nextInterval >= 0)
                {
                    OnInterval.Invoke();
                    nextInterval -= interval;
                }
            }

            if (IsRunning && CurrentTime <= 0)
            {
                CurrentTime = 0;
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0;
    }

    /**
     * <summary>
     * A stopwatch timer that counts up indefinitely from zero or a specified start time.
     * </summary>
     */
    public class StopwatchTimer : Timer
    {
        public StopwatchTimer() : base(0)
        {
        }
        
        public StopwatchTimer(float startTime, bool unscaled = false) : base(0, unscaled)
        {
            CurrentTime = startTime;
        }

        public override void Tick()
        {
            if (IsRunning)
            {
                CurrentTime += DeltaTime;
            }
        }

        public override bool IsFinished => false;
    }
}
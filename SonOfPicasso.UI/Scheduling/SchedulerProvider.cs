﻿using System.Reactive.Concurrency;

namespace SonOfPicasso.UI.Scheduling
{
    public sealed class SchedulerProvider : ISchedulerProvider
    {
        public IScheduler CurrentThread => Scheduler.CurrentThread;

        public IScheduler Immediate => Scheduler.Immediate;

        public IScheduler NewThread => NewThreadScheduler.Default;

        public IScheduler ThreadPool => ThreadPoolScheduler.Instance;

        public IScheduler TaskPool => TaskPoolScheduler.Default;
    }
}
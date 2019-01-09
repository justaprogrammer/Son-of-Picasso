using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Tests.Scheduling
{
    public sealed class TestSchedulers : ISchedulerProvider
    {
        private readonly TestScheduler _currentThread = new TestScheduler();
        private readonly TestScheduler _dispatcher = new TestScheduler();
        private readonly TestScheduler _immediate = new TestScheduler();
        private readonly TestScheduler _newThread = new TestScheduler();
        private readonly TestScheduler _threadPool = new TestScheduler();
        #region Explicit implementation of ISchedulerService
        IScheduler ISchedulerProvider.CurrentThread => _currentThread;
        IScheduler ISchedulerProvider.Dispatcher => _dispatcher;
        IScheduler ISchedulerProvider.Immediate => _immediate;
        IScheduler ISchedulerProvider.NewThread => _newThread;
        IScheduler ISchedulerProvider.ThreadPool => _threadPool;

        #endregion
        public TestScheduler CurrentThread => _currentThread;
        public TestScheduler Dispatcher => _dispatcher;
        public TestScheduler Immediate => _immediate;
        public TestScheduler NewThread => _newThread;
        public TestScheduler ThreadPool => _threadPool;
    }
}
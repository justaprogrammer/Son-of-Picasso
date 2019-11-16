using System;
using System.Threading;
using Bogus;
using FluentAssertions;
using Serilog;
using Xunit.Abstractions;
using SonOfPicasso.Core;
using SonOfPicasso.Core.Logging;

namespace SonOfPicasso.Testing.Common
{
    public abstract class TestsBase: IDisposable
    {
        protected readonly ILogger Logger;
        protected readonly Faker Faker;
        protected readonly AutoResetEvent AutoResetEvent;

        public TestsBase(ITestOutputHelper testOutputHelper)
        {
            Faker = new Faker();

            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.With<CustomEnrichers>() 
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}");

            if (Core.Common.IsVerboseLoggingEnabled)
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose();
            }
            else if (Core.Common.IsDebug)
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
            }
            Log.Logger = loggerConfiguration
                .CreateLogger();

            Logger = Log.Logger.ForContext(GetType());
            AutoResetEvent = new AutoResetEvent(false);
        }

        protected void WaitOne(TimeSpan? timespan = null)
        {
            AutoResetEvent.WaitOne(timespan ?? TimeSpan.FromSeconds(0.5)).Should().BeTrue();
        }

        public virtual void Dispose()
        {
            AutoResetEvent?.Dispose();
        }
    }
}

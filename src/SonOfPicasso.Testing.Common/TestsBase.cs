using System;
using System.Diagnostics;
using System.Threading;
using Bogus;
using FluentAssertions;
using Serilog;
using SonOfPicasso.Core.Logging;
using Xunit.Abstractions;

namespace SonOfPicasso.Testing.Common
{
    public abstract class TestsBase : IDisposable
    {
        protected readonly AutoResetEvent AutoResetEvent;
        protected readonly Faker Faker;

        protected readonly ILogger Logger;

        protected TestsBase(LoggerConfiguration loggerConfiguration)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));

            Faker = new Faker();

            Log.Logger = loggerConfiguration
                .CreateLogger();

            Logger = Log.Logger.ForContext(GetType());
            AutoResetEvent = new AutoResetEvent(false);
        }

        protected TestsBase(ITestOutputHelper testOutputHelper) : this(GetLoggerConfiguration(testOutputHelper))
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                AutoResetEvent?.Dispose();
            }
        }

        public static LoggerConfiguration GetLoggerConfiguration(ITestOutputHelper testOutputHelper,
            Func<LoggerConfiguration, LoggerConfiguration> customLogConfiguration = null)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.With<CustomEnrichers>();

            if (Core.Common.IsVerboseLoggingEnabled)
                loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose();
            else if (Core.Common.IsDebug) loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();

            loggerConfiguration = loggerConfiguration
                .WriteTo.Logger(configuration =>
                {
                    if (customLogConfiguration != null) configuration = customLogConfiguration.Invoke(configuration);

                    configuration.WriteTo.TestOutput(testOutputHelper,
                        outputTemplate:
                        "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}");
                });

            return loggerConfiguration;
        }

        protected bool Set()
        {
            return AutoResetEvent.Set();
        }

        protected void WaitOne(int timespanSeconds)
        {
            WaitOne(TimeSpan.FromSeconds(timespanSeconds));
        }

        protected void WaitOne(TimeSpan? timespan = null)
        {
            if (Debugger.IsAttached)
                timespan = TimeSpan.FromMinutes(3);
            else
                timespan ??= TimeSpan.FromSeconds(0.5);

            try
            {
                AutoResetEvent.WaitOne(timespan.Value).Should()
                    .BeTrue($"Set() was called in {timespan.Value.TotalSeconds:0.00}s");
            }
            catch
            {
                Logger.Error("WaitOne Timeout");
                throw;
            }
        }
    }
}
using System;
using System.Threading;
using Bogus;
using FluentAssertions;
using Serilog;
using Xunit.Abstractions;
using SonOfPicasso.Core.Logging;

namespace SonOfPicasso.Testing.Common
{
    public abstract class TestsBase: IDisposable
    {

        protected static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        protected static bool IsTracingEnabled
        {
            get
            {
                var environmentVariable = Environment.GetEnvironmentVariable("SonOfPicasso.Testing_Tracing");
                if (string.IsNullOrWhiteSpace(environmentVariable))
                    return false;

                environmentVariable = environmentVariable.ToLower();
                if (environmentVariable == "false" || environmentVariable == "1")
                {
                    return false;
                }

                return true;
            }
        }

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

            if (IsTracingEnabled)
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose();
            }
            else if (IsDebug)
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
            }

            Log.Logger = loggerConfiguration
                .CreateLogger();

            Logger = Log.Logger.ForContext(GetType());
            AutoResetEvent = new AutoResetEvent(false);
        }

        protected void WaitOne(int timeout = 500)
        {
            AutoResetEvent.WaitOne(timeout).Should().BeTrue();
        }

        public virtual void Dispose()
        {
            AutoResetEvent?.Dispose();
        }
    }
}

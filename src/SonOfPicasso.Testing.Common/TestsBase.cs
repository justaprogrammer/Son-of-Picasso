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
        protected readonly ILogger Logger;
        protected readonly Faker Faker;
        protected readonly AutoResetEvent AutoResetEvent;

        public TestsBase(ITestOutputHelper testOutputHelper)
        {
            Faker = new Faker();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithThreadId()
                .Enrich.With<CustomEnrichers>() 
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}")
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

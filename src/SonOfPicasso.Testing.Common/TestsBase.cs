using System;
using Autofac;
using Autofac.Core.Registration;
using AutofacSerilogIntegration;
using Bogus;
using Serilog;
using Xunit.Abstractions;
using Serilog.Core;
using Serilog.Events;
using SonOfPicasso.Core.Logging;

namespace SonOfPicasso.Testing.Common
{
    public abstract class TestsBase<T>
    {
        protected readonly ILogger Logger;
        protected readonly Faker Faker;

        public TestsBase(ITestOutputHelper testOutputHelper)
        {
            Faker = new Faker();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithThreadId()
                .Enrich.With<CustomEnrichers>() 
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}")
                .CreateLogger();

            Logger = Log.Logger.ForContext(GetType());
        }
    }
}

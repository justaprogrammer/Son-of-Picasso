﻿using System;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;
using Serilog.Core;
using Serilog.Events;
using SonOfPicasso.Core.Logging;

namespace SonOfPicasso.Testing.Common
{
    public abstract class TestsBase<T>
    {
        protected readonly ILogger<T> Logger;
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

            var serviceProvider = GetServiceCollection().BuildServiceProvider();
            Logger = serviceProvider.GetRequiredService<ILogger<T>>();
        }

        public IServiceCollection GetServiceCollection()
        {
            return new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog());
        }
    }
}

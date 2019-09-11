using Bogus;
using Serilog;
using Xunit.Abstractions;
using SonOfPicasso.Core.Logging;

namespace SonOfPicasso.Testing.Common
{
    public abstract class TestsBase
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
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u4}] ({PaddedThreadId}) {ShortSourceContext} {Message}{NewLineIfException}{Exception}")
                .CreateLogger();

            Logger = Log.Logger.ForContext(GetType());
        }
    }
}

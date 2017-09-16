using System;
using NUnit.Framework;
using Serilog.Core;
using Serilog.Events;

namespace PicasaReboot.Tests.Logging
{
    public class NUnitEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            if (TestContext.CurrentContext == null)
                return;

            logEvent.AddPropertyIfAbsent(new LogEventProperty("TestName", new ScalarValue(TestContext.CurrentContext.Test.Name)));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("TestClassName", new ScalarValue(TestContext.CurrentContext.Test.ClassName)));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("TestMethodName", new ScalarValue(TestContext.CurrentContext.Test.MethodName)));
        }
    }
}
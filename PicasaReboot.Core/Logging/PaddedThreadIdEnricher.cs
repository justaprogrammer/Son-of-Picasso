using System;
using Serilog.Core;
using Serilog.Events;

namespace PicasaReboot.Core.Logging
{
    public class PaddedThreadIdEnricher : ILogEventEnricher
    {
        public const string ThreadIdPropertyName = "ThreadId";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty(ThreadIdPropertyName, 
                new ScalarValue(Environment.CurrentManagedThreadId.ToString().PadLeft(2))));
        }
    }
}
using System;

namespace SonOfPicasso.Tests.Logging
{
    public static class NUnitConfigurationExtensions
    {
        /// <summary>
        /// Enrich log events with a ThreadId property containing the <see cref="P:System.Environment.CurrentManagedThreadId" />.
        /// </summary>
        /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="T:System.ArgumentNullException"></exception>
        public static LoggerConfiguration WithNUnit(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            return enrichmentConfiguration.With<NUnitEnricher>();
        }
    }
}
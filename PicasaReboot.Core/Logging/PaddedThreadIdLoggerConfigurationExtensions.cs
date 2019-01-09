using System;

namespace SonOfPicasso.Core.Logging
{
    public static class PaddedThreadIdLoggerConfigurationExtensions
    {
        /// <summary>
        /// Enrich log events with a ThreadId property containing the <see cref="Environment.CurrentManagedThreadId"/>.
        /// </summary>
        /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static LoggerConfiguration WithPaddedThreadId(
            this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
            return enrichmentConfiguration.With<PaddedThreadIdEnricher>();
        }
    }
}
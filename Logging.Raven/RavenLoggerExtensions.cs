// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Logging.Raven;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILoggingBuilder"/> to add raven logger.
    /// </summary>
    public static class RavenLoggerExtensions
    {
        /// <summary>
        /// Adds a console logger named 'Raven' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddRaven(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();
            builder.Services.AddHttpContextAccessor();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, RavenLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<RavenLoggerOptions, RavenLoggerProvider>(builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds a console logger named 'Raven' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="RavenLogger"/>.</param>
        public static ILoggingBuilder AddRaven(this ILoggingBuilder builder, Action<RavenLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddRaven();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}

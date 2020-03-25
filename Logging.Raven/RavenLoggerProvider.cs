// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Logging.Raven
{

    /// <summary>
    /// A provider of <see cref="RavenLogger"/> instances.
    /// </summary>
    [ProviderAlias("Raven")]
    public class RavenLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        ConcurrentDictionary<string, RavenLogger> loggers;
        IExternalScopeProvider scopeProvider;
        private readonly RavenLoggerProcessor loggerProcessor;
        private readonly IDocumentStore store;
        private IDisposable optionsReloadToken;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IOptionsMonitor<RavenLoggerOptions> optionsMonitor;

        /// <summary>
        /// Creates an instance of <see cref="RavenLoggerProvider"/>
        /// </summary>
        /// <param name="store">The RavenDB document store</param>
        /// <param name="httpContextAccessor">The http context accessor</param>
        /// <param name="optionsMonitor">The options to create <see cref="RavenLogger"/> instances with.</param>
        public RavenLoggerProvider(IDocumentStore store, IHttpContextAccessor httpContextAccessor, IOptionsMonitor<RavenLoggerOptions> optionsMonitor)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.scopeProvider = NullExternalScopeProvider.Instance;
            this.loggers = new ConcurrentDictionary<string, RavenLogger>();
            this.store = store;
            this.loggerProcessor = new RavenLoggerProcessor(store);
            this.optionsMonitor = optionsMonitor;
            SetOptions(optionsMonitor.CurrentValue);
            optionsReloadToken = optionsMonitor.OnChange(SetOptions);
        }

        private void SetOptions(RavenLoggerOptions options)
        {
            this.loggerProcessor.Options = options;
            foreach (var kvLogger in loggers)
            {
                kvLogger.Value.Options = options;
            }
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string category)
        {
            return loggers.GetOrAdd(category, loggerName => new RavenLogger(category, loggerProcessor, this.httpContextAccessor)
            {
                ScopeProvider = scopeProvider,
                Options = this.optionsMonitor.CurrentValue
            });
        }

        /// <inheritdoc />
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            this.scopeProvider = scopeProvider;
            foreach (var kvLogger in loggers)
            {
                kvLogger.Value.ScopeProvider = scopeProvider;
            }
        }

        /// <summary>
        /// If the instance is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            optionsReloadToken.Dispose();
            this.loggerProcessor.Dispose();
        }
    }
}

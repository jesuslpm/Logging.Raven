// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logging.Raven
{
    internal class RavenLogger : ILogger
    {
        private readonly RavenLoggerProcessor loggerProcessor;
        private readonly IHttpContextAccessor httpContextAccessor;
        public string Category { get; private set; }

        internal IExternalScopeProvider ScopeProvider { get; set; }
        internal RavenLoggerOptions Options { get; set; }

        public RavenLogger(string category, RavenLoggerProcessor loggerProcessor, IHttpContextAccessor httpContextAccessor)
        {
            this.loggerProcessor = loggerProcessor;
            this.Category = category;
            this.httpContextAccessor = httpContextAccessor;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return ScopeProvider?.Push(state) ?? NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        static Dictionary<string, object> ToDictionary(object obj)
        {
            if (obj is IEnumerable<KeyValuePair<string, object>> properties)
            {
                return properties.ToDictionary(x => x.Key,  x =>
                {
                    if (x.Value == null) return null;
                    if (x.Value is System.Reflection.MethodInfo)
                    {
                        return x.Value.ToString();
                    }
                    try
                    {
                        return (object) JToken.FromObject(x.Value);
                    }
                    catch
                    {
                        return (object) x.Value?.ToString();
                    }
                });
            }
            else
            {
                return new Dictionary<string, object>
                {
                    ["Value"] = obj
                };
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                RavenLogEntry entry = new RavenLogEntry();
                entry.Category = this.Category;
                entry.Level = logLevel;
                entry.Text = formatter(state, exception);
                entry.Exception = exception;
                entry.EventId = eventId;
                var httpContext = this.httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    entry.TraceIdentifier = httpContext.TraceIdentifier;
                    entry.IdentityName = httpContext.User.Identity.Name;
                    entry.RequestPath = httpContext.Request.Path;
                }
                entry.State = ToDictionary(state);

                if (Options.IncludeScopes)
                {
                    ScopeProvider.ForEachScope((scope, _) =>
                    {
                        if (entry.Scopes == null) entry.Scopes = new List<Dictionary<string, object>>();
                        entry.Scopes.Add(ToDictionary(scope));
                    }, null as object);
                }

                loggerProcessor.EnqueueLogEntry(entry);
            }
        }
    }
}

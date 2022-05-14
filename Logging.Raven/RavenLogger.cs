// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Logging.Raven
{
    internal class RavenLogger : ILogger
    {
        private readonly RavenLoggerWriter loggerProcessor;
        private readonly IHttpContextAccessor httpContextAccessor;
        public string Category { get; private set; }

        internal IExternalScopeProvider ScopeProvider { get; set; }
        internal RavenLoggerOptions Options { get; set; }

        public RavenLogger(string category, RavenLoggerWriter loggerProcessor, IHttpContextAccessor httpContextAccessor)
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
                var result = new Dictionary<string, object>();
                foreach (var kv in properties)
                {
                    //if (kv.Key == "ModelBinderProviders")
                    //{
                    //}
                    var originalValue = kv.Value;
                    object value = null;
                    if (originalValue != null)
                    {
                        if (originalValue is MethodInfo || originalValue is RouteEndpoint)
                        {
                            value = originalValue.ToString();
                        }
                        else
                        {
                            try
                            {
                                value = JToken.FromObject(originalValue);
                            }
                            catch
                            {
                                value = originalValue.ToString();
                            }
                        }
                    }
                    result.Add(kv.Key, value);
                }
                return result;
            }
            return new Dictionary<string, object>
            {
                ["Value"] = obj
            };
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

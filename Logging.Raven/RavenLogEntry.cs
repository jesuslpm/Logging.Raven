// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Logging.Raven
{
    /// <summary>
    /// Log entries to be stored in a RavenDB database using <see cref="RavenLogger"/>
    /// </summary>
    public class RavenLogEntry
    {
        /// <summary>
        /// Creates an instance of <see cref="RavenLogEntry"/>
        /// </summary>
        public RavenLogEntry()
        {
            TimeStamp = DateTime.UtcNow;
            UserName = Environment.UserName;
            Id = "RavenLogEntries/" + RT.Comb.Provider.PostgreSql.Create().ToString("N");
        }

        /// <summary>
        /// The host name where the log entry is created
        /// </summary>
        static public readonly string StaticHostName = System.Net.Dns.GetHostName();
        /// <summary>
        /// Identifies the log entry. It is in the form RavenLogEntry/guid, where guid is a sequential guid generated using RT.Comb.Provider.PostgreSql.Create().ToString("N")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The category or name of the logger, this is typically the name of the class the produces log entries.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The level of the log entry
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// The text message of the log entry
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The exception associated to the log entry
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The identity name of the http request when available: HttpContext.Request.Identity.Name
        /// </summary>
        public string IdentityName { get; set; }

        /// <summary>
        /// The time stamp when the log entry was created
        /// </summary>
        public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// The http context trace identifier when available: HttpContext.TraceIdentifier
        /// </summary>
        public string TraceIdentifier { get; set; }
        /// <summary>
        /// The http request path when available 
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// The log state. It contains properties of structured logging.
        /// </summary>
        public Dictionary<string, object> State { get; set; }

        /// <summary>
        /// The scopes that were active when the log was produced
        /// </summary>
        public List<Dictionary<string, object>> Scopes { get; set; }

        /// <summary>
        /// The environment user name: Environment.UserName
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// The host name of the machine where the log was created
        /// </summary>
        public string HostName { get { return StaticHostName; } }

        /// <summary>
        /// The EventId
        /// </summary>
        public EventId EventId { get; set; }
    }
}

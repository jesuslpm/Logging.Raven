// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Text;

namespace Logging.Raven
{
    /// <summary>
    /// Options for <see cref="RavenLogger"/>
    /// </summary>
    public class RavenLoggerOptions
    {
        /// <summary>
        /// Creates an instance of <see cref="RavenLoggerOptions"/> that includes scopes and has 4 days expiration
        /// </summary>
        public RavenLoggerOptions()
        {
            this.Expiration = TimeSpan.FromDays(4);
            this.IncludeScopes = true;
        }

        /// <summary>
        /// The database name where to store log entries. Default: null 
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Log entries expiration time. Default 4 days.
        /// </summary>
        public TimeSpan Expiration { get; set; }

        /// <summary>
        /// If scopes will be included in log entries. Default: true
        /// </summary>
        public bool IncludeScopes { get; set; }
    }
}

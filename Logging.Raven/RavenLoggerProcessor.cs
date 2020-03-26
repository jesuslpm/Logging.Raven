// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Logging.Raven
{
    internal sealed class RavenLoggerProcessor : IDisposable
    {
        private const int _maxQueueLength = 8192;

        private readonly IDocumentStore store;
        internal RavenLoggerOptions Options { get; set; }
        private string Database => Options.Database;
        private TimeSpan Expiration => Options.Expiration;

        private readonly BlockingCollection<RavenLogEntry> logEntryQueue = new BlockingCollection<RavenLogEntry>(_maxQueueLength);

        private readonly Thread bulkInsertWorkerThread;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            logEntryQueue.CompleteAdding();
            bulkInsertWorkerThread.Join();
            logEntryQueue.Dispose();
        }

        public RavenLoggerProcessor(IDocumentStore store)
        {
            this.store = store;
            this.bulkInsertWorkerThread = new Thread(BulkInsertFromQueue)
            {
                IsBackground = true,
                Name = "Raven logger queue worker thread"
            };
            this.bulkInsertWorkerThread.Start();
        }

        public void EnqueueLogEntry(RavenLogEntry logEntry)
        {
            if (!logEntryQueue.IsAddingCompleted)
            {
                try
                {
                    logEntryQueue.Add(logEntry);
                }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to enqueue log entry. \n" + ex.ToString());
                }
            }
        }

       
        


        private void TryDispose(ref BulkInsertOperation bulkInsert)
        {
            if (bulkInsert == null) return;
            try
            {
                bulkInsert.Dispose();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to dispose bulk insert operation. \n" + ex.ToString());
            }
            bulkInsert = null;
        }

        private void BulkInsertFromQueue()
        {
            const int timout = 5000;
            bool shouldStop = false;
            BulkInsertOperation bulkInsert = null;

            while (logEntryQueue.IsCompleted == false)
            {
                RavenLogEntry logEntry = null;
                try
                {
                    var taken = logEntryQueue.TryTake(out logEntry, timout);
                    if (taken)
                    {
                        if (bulkInsert == null)
                        {
                            bulkInsert = store.BulkInsert(this.Database);
                        }
                    }
                    else
                    {
                        TryDispose(ref bulkInsert);
                        continue;
                    }
                }
                catch (InvalidOperationException)
                {
                    shouldStop = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to take log entry from queue \n" + ex.ToString());
                    shouldStop = true;
                }
                if (shouldStop)
                {
                    TryDispose(ref bulkInsert);
                    return;
                }
                try
                {
                    var metadata = new Dictionary<string, object>
                    {
                        [global::Raven.Client.Constants.Documents.Metadata.Expires] = DateTime.UtcNow.Add(this.Expiration)
                    };
                    bulkInsert.Store(logEntry, logEntry.Id, new MetadataAsDictionary(metadata));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to store log entry into bulk insert operation\n" + ex.ToString());
                    TryDispose(ref bulkInsert);
                }
            }
        }
    }
}

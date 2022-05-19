// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Exceptions.Database;
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
        private const int MaxAllowedQueueLength = 8192;

        private readonly IDocumentStore store;
        internal RavenLoggerOptions Options { get; set; }
        private string Database => Options.Database;
        private TimeSpan Expiration => Options.Expiration;

        private readonly BlockingCollection<RavenLogEntry> logEntryQueue = new BlockingCollection<RavenLogEntry>(MaxAllowedQueueLength);

        private readonly Thread bulkInsertWorkerThread;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            TryCompleteAdding();
            bulkInsertWorkerThread.Join();
            try { logEntryQueue.Dispose(); } catch { }
        }

        public RavenLoggerProcessor(IDocumentStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
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

        private static TimeSpan tenSeconds = TimeSpan.FromSeconds(10);



        private BulkInsertOperation CreateBulkInsertOperation()
        {
            var startTime = DateTime.UtcNow;
            while (true)
            {
                try
                {
                    // I haven't ever see this to fail, but I don't know, so I implement retry logic.
                    return this.store.BulkInsert(this.Database);
                }
                catch 
                {
                    if (logEntryQueue.IsCompleted) throw;
                    if (DateTime.UtcNow.Subtract(startTime) > tenSeconds)
                    {
                        try { logEntryQueue.CompleteAdding(); } catch { };
                        throw;
                    }
                    Thread.Sleep(200);
                }
            }
        }

        private void RewriteBatch(List<RavenLogEntry> batch)
        {
            var startTime = DateTime.UtcNow;
            BulkInsertOperation bulkInsert = null;
            try
            {
                while (true)
                {
                    bulkInsert = CreateBulkInsertOperation();
                    try
                    {
                        foreach (var entry in batch)
                        {
                            StoreLogEntry(bulkInsert, entry);
                        }
                        bulkInsert.Dispose();
                        bulkInsert = null;
                        return;
                    }
                    catch
                    {
                        TryDispose(ref bulkInsert);
                        if (logEntryQueue.IsCompleted) throw;
                        if (DateTime.UtcNow.Subtract(startTime) > tenSeconds) throw;
                        Thread.Sleep(200);
                    }
                }
            }
            finally
            {
                TryDispose(ref bulkInsert);
                batch.Clear();
            }
        }


        private void TryCompleteAdding()
        {
            try 
            { 
                if (!logEntryQueue.IsAddingCompleted) logEntryQueue.CompleteAdding(); 
            } 
            catch { }
        }
        private void BulkInsertFromQueue()
        {
            const int timeout = 100_000;
            const int batchSize = 2048;
            bool shouldStop = false;
            BulkInsertOperation bulkInsert = null;
            var batch = new List<RavenLogEntry>(batchSize);

            int count = 0;

            while (logEntryQueue.IsCompleted == false)
            {
                RavenLogEntry logEntry = null;
                bool taken = false;
                try
                {
                    taken = logEntryQueue.TryTake(out logEntry, timeout);
                }
                catch (Exception)
                {
                    shouldStop = true;
                }
                if (taken)
                {
                    if (bulkInsert == null)
                    {
                        try
                        {
                            bulkInsert = CreateBulkInsertOperation();
                        }
                        catch (Exception ex)
                        {
                            shouldStop = true;
                            Console.Error.WriteLine("Failed to create bulk insert operation\n" + ex.ToString());
                        }
                    }
                }
                else
                {
                    TryDispose(ref bulkInsert);
                    batch.Clear();
                    continue;
                }
                if (shouldStop)
                {
                    TryDispose(ref bulkInsert);
                    TryCompleteAdding();
                    batch.Clear();
                    return;
                }
                try
                {
                    // batch.Add(logEntry);
                    StoreLogEntry(bulkInsert, logEntry);
                    if (batch.Count >= batchSize || ++count >= 65536)
                    {
                        bulkInsert.Dispose();
                        bulkInsert = null;
                        batch.Clear();
                        count = 0;
                    }
                }
                catch (DatabaseDoesNotExistException ex)
                {
                    Console.Error.WriteLine("Failed to store log entry into bulk insert operation\n" + ex.ToString());
                    TryDispose(ref bulkInsert);
                    try { if (!logEntryQueue.IsAddingCompleted) logEntryQueue.CompleteAdding(); } catch { }
                    batch.Clear();
                    return;
                }
                catch
                {
                    TryDispose(ref bulkInsert);
                    try
                    {
                        RewriteBatch(batch);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed to store log entries into bulk insert operation\n" + ex.ToString());
                    }
                }
            }
        }

        private void StoreLogEntry(BulkInsertOperation bulkInsert, RavenLogEntry logEntry)
        {
            var metadata = new Dictionary<string, object>
            {
                [global::Raven.Client.Constants.Documents.Metadata.Expires] = DateTime.UtcNow.Add(this.Expiration)
            };
            bulkInsert.Store(logEntry, logEntry.Id, new MetadataAsDictionary(metadata));
        }
    }
}

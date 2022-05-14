// Copyright (c) i-nercya intelligent software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions.Database;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Logging.Raven
{
    internal sealed class RavenLoggerWriter : IDisposable
    {
        private const int MaxAllowedQueueLength = 8192;
        private const int SafeMaxAllowedQueueLength = MaxAllowedQueueLength - 4;

        private readonly IDocumentStore store;
        internal RavenLoggerOptions Options { get; set; }
        private string Database => Options.Database;
        private TimeSpan Expiration => Options.Expiration;

        private readonly BlockingCollection<RavenLogEntry> logEntryQueue = new BlockingCollection<RavenLogEntry>(MaxAllowedQueueLength);

        private readonly Thread logEntriesWriterThread;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            logEntryQueue.CompleteAdding();
            logEntriesWriterThread.Join();
            logEntryQueue.Dispose();
        }

        public RavenLoggerWriter(IDocumentStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            this.store = store;
            this.logEntriesWriterThread = new Thread(WriteLogEntriesInQueue)
            {
                IsBackground = true,
                Name = "Raven log entries writer thread"
            };
            this.logEntriesWriterThread.Start();
        }

        public void EnqueueLogEntry(RavenLogEntry logEntry)
        {
            if (!logEntryQueue.IsAddingCompleted && logEntryQueue.Count < SafeMaxAllowedQueueLength)
            {
                try
                {
                    logEntryQueue.Add(logEntry);
                }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to enqueue log entry: \n" + ex.ToString());
                }
            }
        }

        private void Store(IDocumentSession session, RavenLogEntry logEntry)
        {
            const string expires = global::Raven.Client.Constants.Documents.Metadata.Expires;
            session.Store(logEntry, logEntry.Id);
            session.Advanced.GetMetadataFor(logEntry)[expires] = DateTime.UtcNow.Add(this.Expiration);
        }

        bool TryTake(out RavenLogEntry logEntry)
        {
            if (logEntryQueue.IsCompleted)
            {
                logEntry = null;
                return false;
            }
            try
            {
                return logEntryQueue.TryTake(out logEntry, 500);
            }
            catch
            {
                logEntry = null;
                return false;
            }
        }

        static TimeSpan eightSeconds = TimeSpan.FromSeconds(8);

        void TrySaveChanges(IDocumentSession session)
        {
            var attempt = 1;
            var timeout = 50;
            var startTime = DateTime.UtcNow;
            while (true)
            {
                try
                {
                    session.SaveChanges();
                    return;
                }
                catch (DatabaseDoesNotExistException ex)
                {
                    Console.Error.WriteLine($"Failed to save RavenLogEntries:\n{ex}");
                    try { logEntryQueue.CompleteAdding(); } catch { }
                    throw;
                }
                catch (Exception ex)
                {
                    if (++attempt > 8 || logEntryQueue.IsAddingCompleted || DateTime.UtcNow.Subtract(startTime) > eightSeconds)
                    {
                        Console.Error.WriteLine($"Failed to save RavenLogEntries:\n{ex}");
                        return;
                    }
                    Thread.Sleep(timeout);
                    timeout *= 2;
                }
            }
        }

        static TimeSpan fiveSeconds = TimeSpan.FromSeconds(5);

        private void WriteLogEntriesInQueue()
        {
            while (logEntryQueue.IsCompleted == false)
            {
                RavenLogEntry logEntry;
                try
                {
                    logEntry = logEntryQueue.Take();
                }
                catch 
                {
                    return;
                }
                using (var session = store.OpenSession(this.Database))
                {
                    int count = 0;
                    var startTime = DateTime.UtcNow;
                    do
                    {
                        Store(session, logEntry);
                        count++;
                    } while (count < 500 && DateTime.UtcNow.Subtract(startTime) < fiveSeconds && TryTake(out logEntry));
                    try
                    {
                        TrySaveChanges(session);
                        // Console.WriteLine($"{count} log entries written in {DateTime.UtcNow.Subtract(startTime)}");
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }
    }
}

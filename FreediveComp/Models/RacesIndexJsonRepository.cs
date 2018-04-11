using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace MilanWilczak.FreediveComp.Models
{
    public class RacesIndexJsonRepository : IRacesIndexRepository, IDisposable
    {
        private readonly IDataFolder dataFolder;
        private readonly ReaderWriterLockSlim mutex;
        private readonly JsonSerializer serializer;
        private List<RaceIndexEntry> entries;

        public RacesIndexJsonRepository(IDataFolder dataFolder)
        {
            this.dataFolder = dataFolder;
            this.mutex = new ReaderWriterLockSlim();
            this.serializer = JsonSerializer.Create();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                mutex.Dispose();
            }
        }

        public List<RaceIndexEntry> Search(HashSet<string> search, DateTimeOffset? date)
        {
            return GetData(entries => FindInternal(entries, search, date));
        }

        private static List<RaceIndexEntry> FindInternal(List<RaceIndexEntry> entries, HashSet<string> search, DateTimeOffset? date)
        {
            return entries
                    .Select(e => Tuple.Create(e, e.Match(search, date)))
                    .Where(t => t.Item2 > 0)
                    .OrderByDescending(t => t.Item2)
                    .Select(t => t.Item1)
                    .ToList();
        }

        public List<RaceIndexEntry> GetAll()
        {
            return GetData(e => e);
        }

        public void SaveRace(RaceIndexEntry entry)
        {
            ChangeData(entries => SaveRaceInternal(entries, entry));
        }

        private static void SaveRaceInternal(List<RaceIndexEntry> entries, RaceIndexEntry entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].RaceId == entry.RaceId)
                {
                    entries[i] = entry;
                    return;
                }
            }
            entries.Add(entry);
        }

        private TResult GetData<TResult>(Func<List<RaceIndexEntry>, TResult> reader)
        {
            try
            {
                mutex.EnterReadLock();
                if (entries != null) return reader(entries);
            }
            finally
            {
                mutex.ExitReadLock();
            }
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                return reader(entries);
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        private void ChangeData(Action<List<RaceIndexEntry>> writer)
        {
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                writer(entries);
                SaveData();
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void EnsureDataLoaded()
        {
            if (entries != null) return;

            try
            {
                using (Stream stream = new MemoryStream(dataFolder.Open("races.json"), false))
                using (TextReader textReader = new StreamReader(stream, true))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                {
                    entries = serializer.Deserialize<List<RaceIndexEntry>>(jsonReader);
                }
            }
            catch (Exception)
            {
                entries = new List<RaceIndexEntry>();
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void SaveData()
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (TextWriter textWriter = new StreamWriter(stream, Encoding.UTF8))
                    using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                    {
                        serializer.Serialize(jsonWriter, entries);
                    }
                    dataFolder.Create("races.json", stream.ToArray());
                }
            }
            catch (Exception)
            {
                entries = null;
            }
        }
    }
}
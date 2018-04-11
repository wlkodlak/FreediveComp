using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;

namespace MilanWilczak.FreediveComp.Models
{
    public class StartingListJsonRepository : IStartingListRepository, IDisposable
    {
        private IDataFolder dataFolder;
        private ReaderWriterLockSlim mutex;
        private JsonSerializer serializer;
        private List<StartingListEntry> startingList;

        public StartingListJsonRepository(IDataFolder dataFolder)
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

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public List<StartingListEntry> GetStartingList()
        {
            try
            {
                mutex.EnterReadLock();
                if (startingList != null) return startingList;
            }
            finally
            {
                mutex.ExitReadLock();
            }
            try
            {
                mutex.EnterWriteLock();
                if (startingList != null) return startingList;

                try
                {
                    using (Stream stream = new MemoryStream(dataFolder.Open("startinglist.json"), false))
                    using (TextReader textReader = new StreamReader(stream, true))
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        this.startingList = serializer.Deserialize<List<StartingListEntry>>(jsonReader);
                        return this.startingList;
                    }
                }
                catch (IOException)
                {
                    return new List<StartingListEntry>();
                }
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public void SaveStartingList(List<StartingListEntry> startingList)
        {
            try
            {
                mutex.EnterWriteLock();
                this.startingList = startingList;
                using (MemoryStream stream = new MemoryStream())
                {
                    using (TextWriter textWriter = new StreamWriter(stream, Encoding.UTF8))
                    using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                    {
                        serializer.Serialize(jsonWriter, startingList);
                    }
                    dataFolder.Create("startinglist.json", stream.ToArray());
                }
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }
    }
}
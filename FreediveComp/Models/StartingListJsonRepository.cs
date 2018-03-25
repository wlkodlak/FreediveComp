using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FreediveComp.Models
{
    public class StartingListJsonRepository : IStartingListRepository
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
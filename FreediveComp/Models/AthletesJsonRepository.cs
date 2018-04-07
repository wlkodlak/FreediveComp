using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace FreediveComp.Models
{
    public class AthletesJsonRepository : IAthletesRepository, IDisposable
    {
        private ReaderWriterLockSlim mutex;
        private IDataFolder dataFolder;
        private HashSet<string> allAthletesIds;
        private Dictionary<string, Athlete> individualAthletes;
        private JsonSerializer serializer;

        public AthletesJsonRepository(IDataFolder dataFolder)
        {
            this.dataFolder = dataFolder;
            this.mutex = new ReaderWriterLockSlim();
            this.allAthletesIds = null;
            this.individualAthletes = new Dictionary<string, Athlete>();
            this.serializer = JsonSerializer.Create();
            this.serializer.Converters.Add(new SexJsonConverter());
        }

        public void Dispose()
        {
            mutex.Dispose();
        }

        public Athlete FindAthlete(string athleteId)
        {
            Athlete athlete;
            if (TryGetCachedAthlete(athleteId, out athlete)) return athlete;
            try
            {
                mutex.EnterWriteLock();
                EnsureAllAthleteIdsLoaded();
                if (!allAthletesIds.Contains(athleteId)) return null;
                return LoadAthlete(athleteId);
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        private bool TryGetCachedAthlete(string athleteId, out Athlete athlete)
        {
            try
            {
                mutex.EnterReadLock();
                if (individualAthletes.TryGetValue(athleteId, out athlete)) return true;
                if (allAthletesIds != null && !allAthletesIds.Contains(athleteId)) return true;
                return false;
            }
            finally
            {
                mutex.ExitReadLock();
            }
        }

        private void EnsureAllAthleteIdsLoaded()
        {
            if (allAthletesIds != null) return;
            allAthletesIds = new HashSet<string>(dataFolder.GetFiles().Where(IsAthleteFileName).Select(ExtractAthleteId));
        }

        private Athlete LoadAthlete(string athleteId)
        {
            Athlete athlete;
            if (individualAthletes.TryGetValue(athleteId, out athlete)) return athlete;

            try
            {
                using (Stream stream = new MemoryStream(dataFolder.Open(GetAthleteFileName(athleteId))))
                using (TextReader textReader = new StreamReader(stream, true))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                {
                    athlete = serializer.Deserialize<Athlete>(jsonReader);
                    individualAthletes[athleteId] = athlete;
                    return athlete;
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static bool IsAthleteFileName(string fileName)
        {
            if (!fileName.StartsWith("athlete-")) return false;
            if (!fileName.EndsWith(".json")) return false;
            return true;
        }

        private static string GetAthleteFileName(string athleteId)
        {
            return "athlete-" + athleteId + ".json";
        }

        private static string ExtractAthleteId(string fileName)
        {
            if (!fileName.StartsWith("athlete-")) return null;
            if (!fileName.EndsWith(".json")) return null;
            return fileName.Substring(8, fileName.Length - 13);
        }

        public List<Athlete> GetAthletes()
        {
            List<Athlete> athletes = GetCachedAthletes();
            if (athletes != null) return athletes;
            try
            {
                mutex.EnterWriteLock();
                EnsureAllAthleteIdsLoaded();
                athletes = new List<Athlete>(allAthletesIds.Count);
                foreach (string athleteId in allAthletesIds)
                {
                    athletes.Add(LoadAthlete(athleteId));
                }
                return athletes;
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        private List<Athlete> GetCachedAthletes()
        {
            try
            {
                mutex.EnterReadLock();
                if (allAthletesIds == null) return null;
                List<Athlete> allAthletes = new List<Athlete>(allAthletesIds.Count);
                foreach (string athleteId in allAthletesIds)
                {
                    Athlete athlete;
                    if (!individualAthletes.TryGetValue(athleteId, out athlete)) return null;
                    allAthletes.Add(athlete);
                }
                return allAthletes;
            }
            finally
            {
                mutex.ExitReadLock();
            }
        }

        public void SaveAthlete(Athlete athlete)
        {
            try
            {
                mutex.EnterWriteLock();
                string athleteId = athlete.AthleteId;
                if (allAthletesIds != null) allAthletesIds.Add(athleteId);
                individualAthletes[athleteId] = athlete;

                using (MemoryStream stream = new MemoryStream()) {
                    using (TextWriter textWriter = new StreamWriter(stream, Encoding.UTF8))
                    using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                    {
                        serializer.Serialize(jsonWriter, athlete);
                    }
                    dataFolder.Create(GetAthleteFileName(athleteId), stream.ToArray());
                }
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        public void ClearAthletes()
        {
            try
            {
                mutex.EnterWriteLock();
                allAthletesIds.Clear();
                individualAthletes.Clear();
                foreach (string filename in dataFolder.GetFiles().Where(IsAthleteFileName).ToList())
                {
                    dataFolder.Delete(filename);
                }
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }
    }
}
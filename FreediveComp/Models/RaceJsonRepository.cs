using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace MilanWilczak.FreediveComp.Models
{
    public class RaceJsonRepository : IRaceSettingsRepository, IStartingLanesRepository, IDisciplinesRepository, IResultsListsRepository, IDisposable
    {
        private IDataFolder dataFolder;
        private ReaderWriterLockSlim mutex;
        private JsonSerializer serializer;
        private RaceData raceData;

        public RaceJsonRepository(IDataFolder dataFolder)
        {
            this.dataFolder = dataFolder;
            this.mutex = new ReaderWriterLockSlim();
            this.serializer = JsonSerializer.CreateDefault();
            this.serializer.Converters.Add(new SexJsonConverter());
            this.raceData = null;
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

        private TResult GetData<TResult>(Func<RaceData, TResult> reader)
        {
            try
            {
                mutex.EnterReadLock();
                if (raceData != null) return reader(raceData);
            }
            finally
            {
                mutex.ExitReadLock();
            }
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                return reader(raceData);
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        private void ChangeData(Action<RaceData> writer)
        {
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                writer(raceData);
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
            if (raceData != null) return;

            try
            {
                using (Stream stream = new MemoryStream(dataFolder.Open("race.json"), false))
                using (TextReader textReader = new StreamReader(stream, true))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                {
                    raceData = serializer.Deserialize<RaceData>(jsonReader) ?? new RaceData();
                }
            }
            catch (Exception)
            {
                raceData = new RaceData();
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
                        serializer.Serialize(jsonWriter, raceData);
                    }
                    dataFolder.Create("race.json", stream.ToArray());
                }
            }
            catch (Exception)
            {
                raceData = null;
            }

        }

        public void ClearResultLists()
        {
            ChangeData(r => r.ResultsLists.Clear());
        }

        public Discipline FindDiscipline(string disciplineId)
        {
            return GetData(r => r.Disciplines.FirstOrDefault(d => d.DisciplineId == disciplineId));
        }

        public ResultsList FindResultsList(string resultsListId)
        {
            return GetData(r => r.ResultsLists.FirstOrDefault(l => l.ResultsListId == resultsListId));
        }

        public StartingLane FindStartingLane(string startingLaneId)
        {
            return GetData(r => FindStartingLaneInternal(r.StartingLanes, startingLaneId));
        }

        private StartingLane FindStartingLaneInternal(List<StartingLane> lanes, string id)
        {
            if (lanes == null) return null;
            foreach (var lane in lanes)
            {
                if (lane.StartingLaneId == id) return lane;
                var found = FindStartingLaneInternal(lane.SubLanes, id);
                if (found != null) return found;
            }
            return null;
        }

        public List<Discipline> GetDisciplines()
        {
            return GetData(r => r.Disciplines);
        }

        public RaceSettings GetRaceSettings()
        {
            return GetData(r => r.Race);
        }

        public List<ResultsList> GetResultsLists()
        {
            return GetData(r => r.ResultsLists);
        }

        public List<StartingLane> GetStartingLanes()
        {
            return GetData(r => r.StartingLanes);
        }

        public void SetDisciplines(List<Discipline> disciplines)
        {
            ChangeData(r => r.Disciplines = disciplines);
        }

        public void SetRaceSettings(RaceSettings raceSettings)
        {
            ChangeData(r => r.Race = raceSettings);
        }

        public void SetResultsList(ResultsList resultsList)
        {
            ChangeData(r => SetResultsListInternal(r, resultsList));
        }

        private static void SetResultsListInternal(RaceData raceData, ResultsList resultsList)
        {
            for (int i = 0; i < raceData.ResultsLists.Count; i++)
            {
                if (raceData.ResultsLists[i].ResultsListId == resultsList.ResultsListId)
                {
                    raceData.ResultsLists[i] = resultsList;
                    return;
                }
            }
            raceData.ResultsLists.Add(resultsList);
        }

        public void SetStartingLanes(List<StartingLane> startingLanes)
        {
            ChangeData(r => r.StartingLanes = startingLanes);
        }

        public class RaceData
        {
            public RaceSettings Race { get; set; }
            public List<StartingLane> StartingLanes { get; set; }
            public List<Discipline> Disciplines { get; set; }
            public List<ResultsList> ResultsLists { get; set; }

            public RaceData()
            {
                Race = new RaceSettings();
                StartingLanes = new List<StartingLane>();
                Disciplines = new List<Discipline>();
                ResultsLists = new List<ResultsList>();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreediveComp.Models
{
    public class RaceSettingsMemoryRepository : IRaceSettingsRepository
    {
        private RaceSettings raceSettings = new RaceSettings();

        public RaceSettings GetRaceSettings()
        {
            lock (this)
            {
                return raceSettings;
            }
        }

        public void SetRaceSettings(RaceSettings raceSettings)
        {
            lock (this)
            {
                this.raceSettings = raceSettings;
            }
        }
    }

    public class StartingListMemoryRepository : IStartingListRepository
    {
        private List<StartingListEntry> startingList = new List<StartingListEntry>();

        public List<StartingListEntry> GetStartingList()
        {
            lock (this)
            {
                return startingList;
            }
        }

        public void SaveStartingList(List<StartingListEntry> startingList)
        {
            lock (this)
            {
                this.startingList = startingList;
            }
        }
    }

    public class StartingLanesMemoryRepository : IStartingLanesRepository
    {
        private List<StartingLane> data = new List<StartingLane>();

        public StartingLane FindStartingLane(string startingLaneId)
        {
            lock (this)
            {
                return FindStartingLaneInternal(data, startingLaneId);
            }
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

        public List<StartingLane> GetStartingLanes()
        {
            lock (this)
            {
                return data;
            }
        }

        public void SetStartingLanes(List<StartingLane> startingLanes)
        {
            lock (this)
            {
                this.data = startingLanes;
            }
        }
    }

    public class DisciplinesMemoryRepository : IDisciplinesRepository
    {
        private List<Discipline> data = new List<Discipline>();

        public Discipline FindDiscipline(string disciplineId)
        {
            lock (this)
            {
                foreach (Discipline discipline in data)
                {
                    if (discipline.DisciplineId == disciplineId) return discipline;
                }
                return null;
            }
        }

        public List<Discipline> GetDisciplines()
        {
            lock (this)
            {
                return data;
            }
        }

        public void SetDisciplines(List<Discipline> disciplines)
        {
            lock (this)
            {
                this.data = disciplines;
            }
        }
    }

    public class ResultsListsMemoryRepository : IResultsListsRepository
    {
        private Dictionary<string, ResultsList> data = new Dictionary<string, ResultsList>();

        public void ClearResultLists()
        {
            lock (this)
            {
                data.Clear();
            }
        }

        public ResultsList FindResultsList(string resultsListId)
        {
            lock (this)
            {
                ResultsList result;
                data.TryGetValue(resultsListId, out result);
                return result;
            }
        }

        public List<ResultsList> GetResultsLists()
        {
            lock (this)
            {
                return new List<ResultsList>(data.Values);
            }
        }

        public void SetResultsList(ResultsList resultsList)
        {
            lock (this)
            {
                data[resultsList.ResultsListId] = resultsList;
            }
        }
    }

    public class AthletesMemoryRepository : IAthletesRepository
    {
        private Dictionary<string, Athlete> data = new Dictionary<string, Athlete>();

        public void ClearAthletes()
        {
            lock (this)
            {
                data.Clear();
            }
        }

        public Athlete FindAthlete(string athleteId)
        {
            lock (this)
            {
                Athlete athlete;
                data.TryGetValue(athleteId, out athlete);
                return athlete;
            }
        }

        public List<Athlete> GetAthletes()
        {
            lock (this)
            {
                return new List<Athlete>(data.Values);
            }
        }

        public void SaveAthlete(Athlete athlete)
        {
            lock (this)
            {
                data[athlete.AthleteId] = athlete;
            }
        }
    }

    public class JudgesMemoryRepository : IJudgesRepository
    {
        private Dictionary<string, Judge> judgesById = new Dictionary<string, Judge>();
        private Dictionary<string, string> authenticationMap = new Dictionary<string, string>();
        private Dictionary<string, JudgeDevice> devicesById = new Dictionary<string, JudgeDevice>();
        private Dictionary<string, JudgeDevice> devicesByCode = new Dictionary<string, JudgeDevice>();

        public Judge AuthenticateJudge(string authenticationToken)
        {
            lock (this)
            {
                string judgeId;
                Judge judge;
                if (!authenticationMap.TryGetValue(authenticationToken, out judgeId)) return null;
                judgesById.TryGetValue(judgeId, out judge);
                return judge;
            }
        }

        public JudgeDevice FindConnectCode(string connectCode)
        {
            lock (this)
            {
                JudgeDevice device;
                devicesByCode.TryGetValue(connectCode, out device);
                return device;
            }
        }

        public Judge FindJudge(string judgeId)
        {
            lock (this)
            {
                Judge judge;
                judgesById.TryGetValue(judgeId, out judge);
                return judge;
            }
        }

        public JudgeDevice FindJudgeDevice(string deviceId)
        {
            lock (this)
            {
                JudgeDevice device;
                devicesById.TryGetValue(deviceId, out device);
                return device;
            }
        }

        public List<JudgeDevice> FindJudgesDevices(string judgeId)
        {
            lock (this)
            {
                return devicesById.Values.Where(d => d.JudgeId == judgeId).ToList();
            }
        }

        public List<Judge> GetJudges()
        {
            lock (this)
            {
                return judgesById.Values.ToList();
            }
        }

        public void SaveJudge(Judge judge)
        {
            lock (this)
            {
                judgesById[judge.JudgeId] = judge;
            }
        }

        public void SaveJudgeDevice(JudgeDevice device)
        {
            lock (this)
            {
                devicesById[device.DeviceId] = device;
                authenticationMap.Clear();
                devicesByCode.Clear();
                foreach (JudgeDevice existing in devicesById.Values)
                {
                    authenticationMap[existing.AuthenticationToken] = existing.JudgeId;
                    if (existing.ConnectCode != null)
                    {
                        devicesByCode[existing.ConnectCode] = existing;
                    }
                }
            }
        }
    }

    public class RacesIndexMemoryRepository : IRacesIndexRepository
    {
        private Dictionary<string, RaceIndexEntry> entries = new Dictionary<string, RaceIndexEntry>();

        public List<RaceIndexEntry> Search(HashSet<string> search, DateTimeOffset? date)
        {
            lock (this)
            {
                return entries.Values
                    .Select(e => Tuple.Create(e, e.Match(search, date)))
                    .Where(t => t.Item2 > 0)
                    .OrderByDescending(t => t.Item2)
                    .Select(t => t.Item1)
                    .ToList();
            }
        }

        public List<RaceIndexEntry> GetAll()
        {
            lock (this)
            {
                return entries.Values.ToList();
            }
        }

        public void SaveRace(RaceIndexEntry entry)
        {
            lock (this)
            {
                entries[entry.RaceId] = entry;
            }
        }
    }

}
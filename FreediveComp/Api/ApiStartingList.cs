using FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreediveComp.Api
{
    public interface IApiStartingList
    {
        List<StartingListEntry> GetStartingList(string raceId, string startingLaneId);
        void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntry> startingList);
    }

    public class ApiStartingList : IApiStartingList
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly StartingLanesFlatBuilder flattener;

        public ApiStartingList(IRepositorySetProvider repositorySetProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
            this.flattener = new StartingLanesFlatBuilder();
        }

        public List<StartingListEntry> GetStartingList(string raceId, string startingLaneId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");

            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var rootStartingLanes = repositorySet.StartingLanes.GetStartingLanes();
            var allowedStartingLanes = new HashSet<string>(flattener.GetLeaves(rootStartingLanes, startingLaneId).Select(l => l.StartingLaneId));
            var startingList = repositorySet.StartingList.GetStartingList();
            var dtos = startingList.Where(e => allowedStartingLanes.Contains(e.StartingLaneId)).ToList();
            return dtos;
        }

        public void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntry> entries)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(startingLaneId)) throw new ArgumentNullException("Missing StartingLaneId");

            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var rootStartingLanes = repositorySet.StartingLanes.GetStartingLanes();
            var allowedStartingLanes = new HashSet<string>(flattener.GetLeaves(rootStartingLanes, startingLaneId).Select(l => l.StartingLaneId));
            var allowedDisciplines = new HashSet<string>(repositorySet.Disciplines.GetDisciplines().Select(d => d.DisciplineId));
            var allowedAthletes = new HashSet<string>(repositorySet.Athletes.GetAthletes().Select(a => a.AthleteId));

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.AthleteId)) throw new ArgumentNullException("Missing Entry.AthleteId");
                if (string.IsNullOrEmpty(entry.StartingLaneId)) throw new ArgumentNullException("Missing Entry.StartingLaneId");
                if (string.IsNullOrEmpty(entry.DisciplineId)) throw new ArgumentNullException("Missing Entry.DisciplineId");
                if (entry.OfficialTop == DateTimeOffset.MinValue) throw new ArgumentNullException("Missing Entry.OfficialTop");
                if (!allowedAthletes.Contains(entry.AthleteId)) throw new ArgumentOutOfRangeException("Unknown Entry.AthleteId " + entry.AthleteId);
                if (!allowedStartingLanes.Contains(entry.StartingLaneId)) throw new ArgumentOutOfRangeException("Unknown Entry.StartingLaneId " + entry.StartingLaneId);
                if (!allowedDisciplines.Contains(entry.DisciplineId)) throw new ArgumentOutOfRangeException("Unknown Entry.DisciplineId " + entry.DisciplineId);
            }

            var fullList = repositorySet.StartingList.GetStartingList();
            fullList.RemoveAll(e => allowedStartingLanes.Contains(e.StartingLaneId));
            fullList.AddRange(entries);
            fullList.Sort(CompareStartingListEntry);
            repositorySet.StartingList.SaveStartingList(fullList);
        }

        private int CompareStartingListEntry(StartingListEntry x, StartingListEntry y)
        {
            int officialTopComparision = DateTimeOffset.Compare(x.OfficialTop, y.OfficialTop);
            if (officialTopComparision != 0) return officialTopComparision;

            return string.Compare(x.StartingLaneId, y.StartingLaneId);
        }
    }
}
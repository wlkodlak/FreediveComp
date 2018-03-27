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

        public ApiStartingList(IRepositorySetProvider repositorySetProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
        }

        public List<StartingListEntry> GetStartingList(string raceId, string startingLaneId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");

            var allowedStartingLanes = GetLeafStartingLanesIds(raceId, startingLaneId);
            var startingList = repositorySetProvider.GetRepositorySet(raceId).StartingList.GetStartingList();
            var dtos = startingList.Where(e => allowedStartingLanes.Contains(e.StartingLaneId)).ToList();
            return dtos;
        }

        private HashSet<string> GetLeafStartingLanesIds(string raceId, string startingLaneId)
        {
            var startingLanes = repositorySetProvider.GetRepositorySet(raceId).StartingLanes.GetStartingLanes();
            var leafIds = new HashSet<string>();

            // first add topmost allowed lanes
            if (string.IsNullOrEmpty(startingLaneId))
            {
                foreach (var startingLane in startingLanes)
                {
                    if (startingLane.ParentLaneId == null)
                    {
                        leafIds.Add(startingLane.StartingLaneId);
                    }
                }
            }
            else
            {
                foreach (var startingLane in startingLanes)
                {
                    if (startingLane.StartingLaneId == startingLaneId)
                    {
                        leafIds.Add(startingLane.StartingLaneId);
                    }
                }
            }

            // go though the list and find children
            bool anythingAdded = leafIds.Count > 0;
            while (anythingAdded)
            {
                var usedParents = new HashSet<string>();
                anythingAdded = false;
                foreach (var startingLane in startingLanes)
                {
                    if (leafIds.Contains(startingLane.ParentLaneId))
                    {
                        bool childAdded = leafIds.Add(startingLane.StartingLaneId);
                        if (childAdded)
                        {
                            usedParents.Add(startingLane.ParentLaneId);
                            anythingAdded = true;
                        }
                    }
                }
                if (anythingAdded)
                {
                    foreach (var parentId in usedParents)
                    {
                        leafIds.Remove(parentId);
                    }
                }
            }

            return leafIds;
        }

        public void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntry> entries)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(startingLaneId)) throw new ArgumentNullException("Missing StartingLaneId");

            var allowedStartingLanes = GetLeafStartingLanesIds(raceId, startingLaneId);
            var allowedDisciplines = new HashSet<string>(repositorySetProvider.GetRepositorySet(raceId).Disciplines.GetDisciplines().Select(d => d.DisciplineId));
            var allowedAthletes = new HashSet<string>(repositorySetProvider.GetRepositorySet(raceId).Athletes.GetAthletes().Select(a => a.AthleteId));

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

            IStartingListRepository startingListRepository = repositorySetProvider.GetRepositorySet(raceId).StartingList;
            var fullList = startingListRepository.GetStartingList();
            fullList.RemoveAll(e => allowedStartingLanes.Contains(e.StartingLaneId));
            fullList.AddRange(entries);
            fullList.Sort(CompareStartingListEntry);
            startingListRepository.SaveStartingList(fullList);
        }

        private int CompareStartingListEntry(StartingListEntry x, StartingListEntry y)
        {
            int officialTopComparision = DateTimeOffset.Compare(x.OfficialTop, y.OfficialTop);
            if (officialTopComparision != 0) return officialTopComparision;

            return string.Compare(x.StartingLaneId, y.StartingLaneId);
        }
    }
}
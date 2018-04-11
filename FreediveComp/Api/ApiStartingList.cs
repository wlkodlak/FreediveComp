using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MilanWilczak.FreediveComp.Api
{
    public interface IApiStartingList
    {
        List<StartingListEntryDto> GetStartingList(string raceId, string startingLaneId);
        void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntryDto> startingList);
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

        public List<StartingListEntryDto> GetStartingList(string raceId, string startingLaneId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");

            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var rootStartingLanes = repositorySet.StartingLanes.GetStartingLanes();
            var allowedStartingLanes = new HashSet<string>(flattener.GetLeaves(rootStartingLanes, startingLaneId).Select(l => l.StartingLaneId));
            var startingList = repositorySet.StartingList.GetStartingList();
            var dtos = startingList.Where(e => allowedStartingLanes.Contains(e.StartingLaneId)).Select(BuildStartingList).ToList();
            return dtos;
        }

        private StartingListEntryDto BuildStartingList(StartingListEntry model)
        {
            return new StartingListEntryDto
            {
                AthleteId = model.AthleteId,
                DisciplineId = model.DisciplineId,
                StartingLaneId = model.StartingLaneId,
                OfficialTop = model.OfficialTop,
                WarmUpTime = model.WarmUpTime,
            };
        }

        public void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntryDto> dtos)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(startingLaneId)) throw new ArgumentNullException("Missing StartingLaneId");

            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var rootStartingLanes = repositorySet.StartingLanes.GetStartingLanes();
            var allowedStartingLanes = new HashSet<string>(flattener.GetLeaves(rootStartingLanes, startingLaneId).Select(l => l.StartingLaneId));
            var allowedDisciplines = new HashSet<string>(repositorySet.Disciplines.GetDisciplines().Select(d => d.DisciplineId));
            var allowedAthletes = new HashSet<string>(repositorySet.Athletes.GetAthletes().Select(a => a.AthleteId));

            var entries = new List<StartingListEntry>();
            foreach (var dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.AthleteId)) throw new ArgumentNullException("Missing Entry.AthleteId");
                if (string.IsNullOrEmpty(dto.StartingLaneId)) throw new ArgumentNullException("Missing Entry.StartingLaneId");
                if (string.IsNullOrEmpty(dto.DisciplineId)) throw new ArgumentNullException("Missing Entry.DisciplineId");
                if (dto.OfficialTop == DateTimeOffset.MinValue) throw new ArgumentNullException("Missing Entry.OfficialTop");
                if (!allowedAthletes.Contains(dto.AthleteId)) throw new ArgumentOutOfRangeException("Unknown Entry.AthleteId " + dto.AthleteId);
                if (!allowedStartingLanes.Contains(dto.StartingLaneId)) throw new ArgumentOutOfRangeException("Unknown Entry.StartingLaneId " + dto.StartingLaneId);
                if (!allowedDisciplines.Contains(dto.DisciplineId)) throw new ArgumentOutOfRangeException("Unknown Entry.DisciplineId " + dto.DisciplineId);
                entries.Add(new StartingListEntry
                {
                    AthleteId = dto.AthleteId,
                    DisciplineId = dto.DisciplineId,
                    StartingLaneId = dto.StartingLaneId,
                    OfficialTop = dto.OfficialTop,
                    WarmUpTime = dto.WarmUpTime,
                });
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
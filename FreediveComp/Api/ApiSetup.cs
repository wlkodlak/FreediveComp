using FreediveComp.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiSetup
    {
        RaceSetup GetSetup(string raceId);
        void SetupRace(string raceId, RaceSetup raceSetup);
    }

    public class ApiSetup : IApiSetup
    {
        private readonly IRepositorySetProvider repositorySetProvider;

        public ApiSetup(IRepositorySetProvider repositorySetProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
        }

        public RaceSetup GetSetup(string raceId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var raceSetup = new RaceSetup();
            raceSetup.Race = repositorySet.RaceSettings.GetRaceSettings();
            raceSetup.StartingLanes = repositorySet.StartingLanes.GetStartingLanes();
            raceSetup.ResultsLists = repositorySet.ResultsLists.GetResultsLists();
            raceSetup.Disciplines = repositorySet.Disciplines.GetDisciplines();
            return raceSetup;
        }

        public void SetupRace(string raceId, RaceSetup raceSetup)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (raceSetup == null) throw new ArgumentNullException("Missing RaceSetup");
            if (raceSetup.Race == null) throw new ArgumentNullException("Missing RaceSetup.Race");
            if (raceSetup.Disciplines == null) throw new ArgumentNullException("Missing RaceSetup.Disciplines");
            if (raceSetup.ResultsLists == null) throw new ArgumentNullException("Missing RaceSetup.ResultsLists");
            if (raceSetup.StartingLanes == null) throw new ArgumentNullException("Missing RaceSetup.StartingLanes");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            VerifyRaceSettings(raceSetup.Race);
            HashSet<string> disciplineIds = new HashSet<string>();
            foreach (var discipline in raceSetup.Disciplines)
            {
                VerifyDiscipline(discipline, disciplineIds);
            }
            HashSet<string> startingLaneIds = new HashSet<string>();
            foreach (var startingLane in raceSetup.StartingLanes)
            {
                VerifyStartingLane(startingLane, disciplineIds);
            }
            HashSet<string> resultsListsIds = new HashSet<string>();
            foreach (var resultsList in raceSetup.ResultsLists)
            {
                VerifyResultsList(resultsList, resultsListsIds, disciplineIds);
            }

            repositorySet.RaceSettings.SetRaceSettings(raceSetup.Race);
            repositorySet.Disciplines.SetDisciplines(raceSetup.Disciplines);
            repositorySet.StartingLanes.SetStartingLanes(raceSetup.StartingLanes);
            repositorySet.ResultsLists.ClearResultLists();
            foreach (var resultsList in raceSetup.ResultsLists)
            {
                repositorySet.ResultsLists.SetResultsList(resultsList);
            }
        }

        private void VerifyRaceSettings(RaceSettings race)
        {
            if (string.IsNullOrEmpty(race.Name)) throw new ArgumentNullException("Missing race name");
        }

        private void VerifyDiscipline(Discipline discipline, HashSet<string> disciplineIds)
        {
            if (string.IsNullOrEmpty(discipline.DisciplineId)) throw new ArgumentNullException("Missing DisciplineId");
            if (string.IsNullOrEmpty(discipline.LongName)) throw new ArgumentNullException("Missing Discipline.Name");
            if (discipline.Rules == DisciplineRules.Unspecified) throw new ArgumentNullException("Missing Discipline.Rules");
            if (!disciplineIds.Add(discipline.DisciplineId)) throw new ArgumentOutOfRangeException("Duplicate DisciplineId " + discipline.DisciplineId);
        }

        private void VerifyResultsList(ResultsList resultsList, HashSet<string> resultsListsIds, HashSet<string> disciplineIds)
        {
            if (string.IsNullOrEmpty(resultsList.ResultsListId)) throw new ArgumentNullException("Missing ResultsList.ResultsListId");
            if (string.IsNullOrEmpty(resultsList.Title)) throw new ArgumentNullException("Missing ResultsList.Title");
            if (!resultsListsIds.Add(resultsList.ResultsListId)) throw new ArgumentOutOfRangeException("Duplicate ResultsList.ResultsListId " + resultsList.ResultsListId);
            if (resultsList.Columns == null || resultsList.Columns.Count == 0) throw new ArgumentNullException("Missing ResultsList.Columns");
            foreach (var column in resultsList.Columns)
            {
                if (string.IsNullOrEmpty(column.Title)) throw new ArgumentNullException("Missing ResultsList.Column.Title");
                if (column.Components == null || column.Components.Count == 0) throw new ArgumentNullException("Missing ResultsList.Column.Components");
                foreach (var component in column.Components)
                {
                    if (string.IsNullOrEmpty(component.DisciplineId)) throw new ArgumentNullException("Missing ResultsList.Column.Component.DisciplineId");
                    if (!disciplineIds.Contains(component.DisciplineId)) throw new ArgumentOutOfRangeException("Unknown ResultsList.Column.Component.DisciplineId " + component.DisciplineId);
                }
            }
        }

        private void VerifyStartingLane(StartingLane startingLane, HashSet<string> startingLaneIds)
        {
            if (string.IsNullOrEmpty(startingLane.StartingLaneId)) throw new ArgumentNullException("Missing StartingLaneId");
            if (string.IsNullOrEmpty(startingLane.ShortName)) throw new ArgumentNullException("Missing StartingLane.ShortName");
            if (!startingLaneIds.Add(startingLane.StartingLaneId)) throw new ArgumentOutOfRangeException("Duplicate StartingLaneId " + startingLane.StartingLaneId);

            if (startingLane.SubLanes != null)
            {
                foreach (var subLane in startingLane.SubLanes)
                {
                    VerifyStartingLane(subLane, startingLaneIds);
                }
            }
        }
    }
}
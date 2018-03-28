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
            raceSetup.StartingLanes = ConvertStartingLanes(repositorySet.StartingLanes.GetStartingLanes());
            raceSetup.ResultsLists = repositorySet.ResultsLists.GetResultsLists();
            raceSetup.Disciplines = repositorySet.Disciplines.GetDisciplines();
            return raceSetup;
        }

        private List<StartingLane> ConvertStartingLanes(List<Models.StartingLane> models)
        {
            var rootDtos = new List<StartingLane>();
            var parentsMap = models.ToDictionary(m => m.StartingLaneId, m => m.ParentLaneId);
            var dtos = models.ToDictionary(m => m.StartingLaneId, m => new StartingLane
            {
                StartingLaneId = m.StartingLaneId,
                ShortName = m.ShortName,
                SubLanes = new List<StartingLane>()
            });
            foreach (var dto in dtos.Values)
            {
                string parentId = parentsMap[dto.StartingLaneId];
                StartingLane parentDto;
                if (parentId == null)
                {
                    rootDtos.Add(dto);
                }
                else if (dtos.TryGetValue(parentId, out parentDto))
                {
                    parentDto.SubLanes.Add(dto);
                }
            }

            return rootDtos;
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
            HashSet<string> resultsListsIds = new HashSet<string>();
            foreach (var resultsList in raceSetup.ResultsLists)
            {
                VerifyResultsList(resultsList, resultsListsIds, disciplineIds);
            }
            var startingLaneModels = ConvertAndVerifyStartingLanes(raceSetup.StartingLanes);

            repositorySet.RaceSettings.SetRaceSettings(raceSetup.Race);
            repositorySet.Disciplines.SetDisciplines(raceSetup.Disciplines);
            repositorySet.ResultsLists.ClearResultLists();
            foreach (var resultsList in raceSetup.ResultsLists)
            {
                repositorySet.ResultsLists.SetResultsList(resultsList);
            }
            repositorySet.StartingLanes.SetStartingLanes(startingLaneModels);
        }

        private void VerifyRaceSettings(RaceSettings race)
        {
            if (string.IsNullOrEmpty(race.Name)) throw new ArgumentNullException("Missing race name");
        }

        private void VerifyDiscipline(Discipline discipline, HashSet<string> disciplineIds)
        {
            if (string.IsNullOrEmpty(discipline.DisciplineId)) throw new ArgumentNullException("Missing DisciplineId");
            if (string.IsNullOrEmpty(discipline.Name)) throw new ArgumentNullException("Missing Discipline.Name");
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

        private List<Models.StartingLane> ConvertAndVerifyStartingLanes(List<StartingLane> dtos)
        {
            var models = new Dictionary<string, Models.StartingLane>();
            ConvertAndVerifyStartingLanes(models, dtos);
            return models.Values.ToList();
        }

        private void ConvertAndVerifyStartingLanes(Dictionary<string, Models.StartingLane> models, List<StartingLane> dtos)
        {
            if (dtos == null) return;
            foreach (var dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.StartingLaneId)) throw new ArgumentNullException("Missing StartingLaneId");
                if (string.IsNullOrEmpty(dto.ShortName)) throw new ArgumentNullException("Missing StartingLane.ShortName");
                if (models.ContainsKey(dto.StartingLaneId)) throw new ArgumentOutOfRangeException("Duplicate StartingLaneId " + dto.StartingLaneId);

                var model = new Models.StartingLane();
                model.StartingLaneId = dto.StartingLaneId;
                model.ShortName = dto.ShortName;
                model.ParentLaneId = null;
                models[model.StartingLaneId] = model;
            }
        }
    }
}
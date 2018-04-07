using FreediveComp.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiSetup
    {
        RaceSetupDto GetSetup(string raceId);
        void SetupRace(string raceId, RaceSetupDto raceSetup);
    }

    public class ApiSetup : IApiSetup
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly IRulesRepository rulesProvider;

        public ApiSetup(IRepositorySetProvider repositorySetProvider, IRulesRepository rulesProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
            this.rulesProvider = rulesProvider;
        }

        public RaceSetupDto GetSetup(string raceId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var raceSetup = new RaceSetupDto();
            raceSetup.Race = BuildRaceSettings(repositorySet.RaceSettings.GetRaceSettings());
            raceSetup.StartingLanes = repositorySet.StartingLanes.GetStartingLanes().Select(BuildStartingLane).ToList();
            raceSetup.ResultsLists = repositorySet.ResultsLists.GetResultsLists().Select(BuildResultsList).ToList();
            raceSetup.Disciplines = repositorySet.Disciplines.GetDisciplines().Select(BuildDiscipline).ToList();
            return raceSetup;
        }

        private static RaceSettingsDto BuildRaceSettings(RaceSettings model)
        {
            return new RaceSettingsDto
            {
                Name = model.Name
            };
        }

        private static StartingLaneDto BuildStartingLane(StartingLane model)
        {
            return new StartingLaneDto
            {
                ShortName = model.ShortName,
                StartingLaneId = model.StartingLaneId,
                SubLanes = model.SubLanes.Select(BuildStartingLane).ToList()
            };
        }

        private static ResultsListDto BuildResultsList(ResultsList model)
        {
            return new ResultsListDto
            {
                Title = model.Title,
                ResultsListId = model.ResultsListId,
                Columns = model.Columns.Select(BuildResultsColumn).ToList()
            };
        }

        private static ResultsColumnDto BuildResultsColumn(ResultsColumn model)
        {
            return new ResultsColumnDto
            {
                Title = model.Title,
                IsFinal = model.IsFinal,
                Components = model.Components.Select(BuildResultsComponent).ToList()
            };
        }

        private static ResultsComponentDto BuildResultsComponent(ResultsComponent model)
        {
            return new ResultsComponentDto
            {
                DisciplineId = model.DisciplineId,
                FinalPointsCoeficient = model.FinalPointsCoeficient
            };
        }

        private static DisciplineDto BuildDiscipline(Discipline model)
        {
            return new DisciplineDto
            {
                DisciplineId = model.DisciplineId,
                LongName = model.LongName,
                ShortName = model.ShortName,
                AnnouncementsClosed = model.AnnouncementsClosed,
                Rules = model.Rules
            };
        }

        public void SetupRace(string raceId, RaceSetupDto raceSetupDto)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (raceSetupDto == null) throw new ArgumentNullException("Missing RaceSetup");
            if (raceSetupDto.Race == null) throw new ArgumentNullException("Missing RaceSetup.Race");
            if (raceSetupDto.Disciplines == null) throw new ArgumentNullException("Missing RaceSetup.Disciplines");
            if (raceSetupDto.ResultsLists == null) throw new ArgumentNullException("Missing RaceSetup.ResultsLists");
            if (raceSetupDto.StartingLanes == null) throw new ArgumentNullException("Missing RaceSetup.StartingLanes");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var raceSettings = VerifyRaceSettings(raceSetupDto.Race);
            var disciplineIds = new HashSet<string>();
            var rulesNames = rulesProvider.GetNames();
            var disciplines = new List<Discipline>();
            foreach (var discipline in raceSetupDto.Disciplines)
            {
                disciplines.Add(VerifyDiscipline(discipline, disciplineIds, rulesNames));
            }
            var startingLaneIds = new HashSet<string>();
            var startingLanes = new List<StartingLane>();
            foreach (var startingLane in raceSetupDto.StartingLanes)
            {
                startingLanes.Add(VerifyStartingLane(startingLane, disciplineIds));
            }
            var resultsListsIds = new HashSet<string>();
            var resultsLists = new List<ResultsList>();
            foreach (var resultsList in raceSetupDto.ResultsLists)
            {
                resultsLists.Add(VerifyResultsList(resultsList, resultsListsIds, disciplineIds));
            }

            repositorySet.RaceSettings.SetRaceSettings(raceSettings);
            repositorySet.Disciplines.SetDisciplines(disciplines);
            repositorySet.StartingLanes.SetStartingLanes(startingLanes);
            repositorySet.ResultsLists.ClearResultLists();
            foreach (var resultsList in resultsLists)
            {
                repositorySet.ResultsLists.SetResultsList(resultsList);
            }
        }

        private RaceSettings VerifyRaceSettings(RaceSettingsDto race)
        {
            if (string.IsNullOrEmpty(race.Name)) throw new ArgumentNullException("Missing race name");
            return new RaceSettings
            {
                Name = race.Name
            };
        }

        private Discipline VerifyDiscipline(DisciplineDto discipline, HashSet<string> disciplineIds, HashSet<string> rulesNames)
        {
            if (string.IsNullOrEmpty(discipline.DisciplineId)) throw new ArgumentNullException("Missing DisciplineId");
            if (string.IsNullOrEmpty(discipline.LongName)) throw new ArgumentNullException("Missing Discipline.Name");
            if (string.IsNullOrEmpty(discipline.Rules)) throw new ArgumentNullException("Missing Discipline.Rules");
            if (!rulesNames.Contains(discipline.Rules)) throw new ArgumentOutOfRangeException("Unknown Discipline.Rules " + discipline.Rules);
            if (!disciplineIds.Add(discipline.DisciplineId)) throw new ArgumentOutOfRangeException("Duplicate DisciplineId " + discipline.DisciplineId);

            return new Discipline
            {
                DisciplineId = discipline.DisciplineId,
                LongName = discipline.LongName,
                ShortName = discipline.ShortName,
                AnnouncementsClosed = discipline.AnnouncementsClosed,
                Rules = discipline.Rules
            };
        }

        private ResultsList VerifyResultsList(ResultsListDto resultsListDto, HashSet<string> resultsListsIds, HashSet<string> disciplineIds)
        {
            if (string.IsNullOrEmpty(resultsListDto.ResultsListId)) throw new ArgumentNullException("Missing ResultsList.ResultsListId");
            if (string.IsNullOrEmpty(resultsListDto.Title)) throw new ArgumentNullException("Missing ResultsList.Title");
            if (!resultsListsIds.Add(resultsListDto.ResultsListId)) throw new ArgumentOutOfRangeException("Duplicate ResultsList.ResultsListId " + resultsListDto.ResultsListId);
            if (resultsListDto.Columns == null || resultsListDto.Columns.Count == 0) throw new ArgumentNullException("Missing ResultsList.Columns");

            ResultsList resultsList = new ResultsList
            {
                Title = resultsListDto.Title,
                ResultsListId = resultsListDto.ResultsListId,
                Columns = new List<ResultsColumn>()
            };

            foreach (var columnDto in resultsListDto.Columns)
            {
                if (string.IsNullOrEmpty(columnDto.Title)) throw new ArgumentNullException("Missing ResultsList.Column.Title");
                if (columnDto.Components == null || columnDto.Components.Count == 0) throw new ArgumentNullException("Missing ResultsList.Column.Components");

                var column = new ResultsColumn
                {
                    Title = columnDto.Title,
                    IsFinal = columnDto.IsFinal,
                    Components = new List<ResultsComponent>()
                };

                foreach (var componentDto in columnDto.Components)
                {
                    if (string.IsNullOrEmpty(componentDto.DisciplineId)) throw new ArgumentNullException("Missing ResultsList.Column.Component.DisciplineId");
                    if (!disciplineIds.Contains(componentDto.DisciplineId)) throw new ArgumentOutOfRangeException("Unknown ResultsList.Column.Component.DisciplineId " + componentDto.DisciplineId);

                    column.Components.Add(new ResultsComponent
                    {
                        DisciplineId = componentDto.DisciplineId,
                        FinalPointsCoeficient = componentDto.FinalPointsCoeficient
                    });
                }

                resultsList.Columns.Add(column);
            }

            return resultsList;
        }

        private StartingLane VerifyStartingLane(StartingLaneDto dto, HashSet<string> startingLaneIds)
        {
            if (string.IsNullOrEmpty(dto.StartingLaneId)) throw new ArgumentNullException("Missing StartingLaneId");
            if (string.IsNullOrEmpty(dto.ShortName)) throw new ArgumentNullException("Missing StartingLane.ShortName");
            if (!startingLaneIds.Add(dto.StartingLaneId)) throw new ArgumentOutOfRangeException("Duplicate StartingLaneId " + dto.StartingLaneId);

            StartingLane startingLane = new StartingLane
            {
                ShortName = dto.ShortName,
                StartingLaneId = dto.StartingLaneId,
                SubLanes = new List<StartingLane>()
            };

            if (dto.SubLanes != null)
            {
                foreach (var subLane in dto.SubLanes)
                {
                    startingLane.SubLanes.Add(VerifyStartingLane(subLane, startingLaneIds));
                }
            }

            return startingLane;
        }
    }
}
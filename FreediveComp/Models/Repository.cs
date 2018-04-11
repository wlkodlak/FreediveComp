using System;
using System.Collections.Generic;

namespace MilanWilczak.FreediveComp.Models
{
    public interface IRaceSettingsRepository
    {
        RaceSettings GetRaceSettings();
        void SetRaceSettings(RaceSettings raceSettings);
    }

    public interface IStartingListRepository
    {
        List<StartingListEntry> GetStartingList();
        void SaveStartingList(List<StartingListEntry> startingList);
    }

    public interface IStartingLanesRepository
    {
        List<StartingLane> GetStartingLanes();
        void SetStartingLanes(List<StartingLane> startingLanes);
        StartingLane FindStartingLane(string startingLaneId);
    }

    public interface IDisciplinesRepository
    {
        List<Discipline> GetDisciplines();
        void SetDisciplines(List<Discipline> disciplines);
        Discipline FindDiscipline(string disciplineId);
    }

    public interface IResultsListsRepository
    {
        List<ResultsList> GetResultsLists();
        ResultsList FindResultsList(string resultsListId);
        void SetResultsList(ResultsList resultsList);
        void ClearResultLists();
    }

    public interface IAthletesRepository
    {
        List<Athlete> GetAthletes();
        Athlete FindAthlete(string athleteId);
        void SaveAthlete(Athlete athlete);
        void ClearAthletes();
    }

    public interface IJudgesRepository
    {
        Judge AuthenticateJudge(string authenticationToken);
        JudgeDevice FindJudgeDevice(string deviceId);
        JudgeDevice FindConnectCode(string connectCode);
        Judge FindJudge(string judgeId);
        void SaveJudgeDevice(JudgeDevice judgeDevice);
        void SaveJudge(Judge judge);
        List<JudgeDevice> FindJudgesDevices(string judgeId);
        List<Judge> GetJudges();
    }

    public interface IRacesIndexRepository
    {
        List<RaceIndexEntry> GetAll();
        List<RaceIndexEntry> Search(HashSet<string> search, DateTimeOffset? date);
        void SaveRace(RaceIndexEntry entry);
    }
}
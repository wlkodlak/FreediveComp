﻿using System;
using System.Collections.Generic;

namespace FreediveComp.Models
{
    public class Athlete
    {
        public string AthleteId { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Club { get; set; }
        public string CountryName { get; set; }
        public string ProfilePhotoName { get; set; }
        public Gender Gender { get; set; }
        public string Category { get; set; }
        public string ModeratorNotes { get; set; }
        public List<Announcement> Announcements { get; set; }
        public List<ActualResult> ActualResults { get; set; }
    }

    public enum Gender
    {
        Unspecified,
        Male,
        Female
    }

    public class Announcement
    {
        public string DisciplineId { get; set; }
        public Performance Performance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class Performance
    {
        public TimeSpan? Duration { get; set; }
        public double? Depth { get; set; }
        public double? Distance { get; set; }
        public double? Points { get; set; }
        public double? DurationSeconds
        {
            get { return Duration == null ? (double?)null : Duration.Value.TotalSeconds; }
            set { Duration = value == null ? (TimeSpan?)null : TimeSpan.FromSeconds(value.Value); }
        }
    }

    public class ActualResult
    {
        public string DisciplineId { get; set; }
        public Performance Performance { get; set; }
        public List<Penalization> Penalizations { get; set; }
        public CardResult CardResult { get; set; }
        public string JudgeId { get; set; }
        public string JudgeComment { get; set; }
        public Performance FinalPerformance { get; set; }
    }

    public class Penalization
    {
        public string Reason { get; set; }
        public string ShortReason { get; set; }
        public string PenalizationId { get; set; }
        public bool IsShortPerformance { get; set; }
        public Performance Performance { get; set; }
        public double? RuleInput { get; set; }
    }

    public enum CardResult
    {
        None,
        DidNotStart,
        White,
        Yellow,
        Red
    }

    public class Discipline
    {
        public string DisciplineId { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Rules { get; set; }
    }

    public class RaceSettings
    {
        public string Name { get; set; }
    }

    public class Judge
    {
        public string JudgeId { get; set; }
        public string Name { get; set; }
    }

    public class JudgeDevice
    {
        public string AuthenticationToken { get; set; }
        public string ConnectCode { get; set; }
        public string DeviceId { get; set; }
        public string JudgeId { get; set; }
    }

    public class StartingLane
    {
        public string StartingLaneId { get; set; }
        public string ShortName { get; set; }
        public List<StartingLane> SubLanes { get; set; }
    }

    public class StartingListEntry
    {
        public string AthleteId { get; set; }
        public string DisciplineId { get; set; }
        public string StartingLaneId { get; set; }
        public DateTimeOffset? WarmUpTime { get; set; }
        public DateTimeOffset OfficialTop { get; set; }
    }

    public class ResultsList
    {
        public string ResultsListId { get; set; }
        public string Title { get; set; }
        public List<ResultsColumn> Columns { get; set; }
    }

    public class ResultsColumn
    {
        public string Title { get; set; }
        public bool IsFinal { get; set; }
        public List<ResultsComponent> Components { get; set; }
    }

    public class ResultsComponent
    {
        public string DisciplineId { get; set; }
        public double? FinalPointsCoeficient { get; set; }
    }
}
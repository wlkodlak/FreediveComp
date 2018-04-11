using System.Collections.Generic;

namespace MilanWilczak.FreediveComp.Models
{
    public interface IRepositorySetProvider
    {
        IRepositorySet GetRepositorySet(string raceId);
    }

    public interface IRepositorySet
    {
        IRaceSettingsRepository RaceSettings { get; }
        IStartingListRepository StartingList { get; }
        IStartingLanesRepository StartingLanes { get; }
        IDisciplinesRepository Disciplines { get; }
        IResultsListsRepository ResultsLists { get; }
        IAthletesRepository Athletes { get; }
        IJudgesRepository Judges { get; }
    }

    public class RepositorySet : IRepositorySet
    {
        private readonly IRaceSettingsRepository raceSettings;
        private readonly IStartingListRepository startingList;
        private readonly IStartingLanesRepository startingLanes;
        private readonly IDisciplinesRepository disciplines;
        private readonly IResultsListsRepository resultsLists;
        private readonly IAthletesRepository athletes;
        private readonly IJudgesRepository judges;

        public RepositorySet(
            IRaceSettingsRepository raceSettings,
            IStartingListRepository startingList,
            IStartingLanesRepository startingLanes,
            IDisciplinesRepository disciplines,
            IResultsListsRepository resultsLists,
            IAthletesRepository athletes,
            IJudgesRepository judges)
        {
            this.raceSettings = raceSettings;
            this.startingList = startingList;
            this.startingLanes = startingLanes;
            this.disciplines = disciplines;
            this.resultsLists = resultsLists;
            this.athletes = athletes;
            this.judges = judges;
        }

        public IRaceSettingsRepository RaceSettings
        {
            get { return raceSettings; }
        }

        public IStartingListRepository StartingList
        {
            get { return startingList; }
        }

        public IStartingLanesRepository StartingLanes
        {
            get { return startingLanes; }
        }

        public IDisciplinesRepository Disciplines
        {
            get { return disciplines; }
        }

        public IResultsListsRepository ResultsLists
        {
            get { return resultsLists; }
        }

        public IAthletesRepository Athletes
        {
            get { return athletes; }
        }

        public IJudgesRepository Judges
        {
            get { return judges; }
        }
    }

    public abstract class RepositorySetProvider : IRepositorySetProvider
    {
        private Dictionary<string, IRepositorySet> sets = new Dictionary<string, IRepositorySet>();

        protected abstract IRepositorySet CreateRepositorySet(string raceId);

        public IRepositorySet GetRepositorySet(string raceId)
        {
            IRepositorySet repositorySet;
            lock (sets)
            {
                if (sets.TryGetValue(raceId, out repositorySet)) return repositorySet;
            }
            repositorySet = CreateRepositorySet(raceId);
            lock (sets)
            {
                if (sets.ContainsKey(raceId))
                {
                    return sets[raceId];
                }
                else
                {
                    sets[raceId] = repositorySet;
                    return repositorySet;
                }
            }
        }
    }

    public class RepositorySetMemoryProvider : RepositorySetProvider
    {
        protected override IRepositorySet CreateRepositorySet(string raceId)
        {
            return new RepositorySet(
                new RaceSettingsMemoryRepository(),
                new StartingListMemoryRepository(),
                new StartingLanesMemoryRepository(),
                new DisciplinesMemoryRepository(),
                new ResultsListsMemoryRepository(),
                new AthletesMemoryRepository(),
                new JudgesMemoryRepository()
                );
        }
    }

    public class RepositorySetJsonProvider : RepositorySetProvider
    {
        private IDataFolder rootFolder;

        public RepositorySetJsonProvider(IDataFolder rootFolder)
        {
            this.rootFolder = rootFolder;
        }

        protected override IRepositorySet CreateRepositorySet(string raceId)
        {
            var dataFolder = rootFolder.GetSubfolder(raceId);
            var judges = new JudgesJsonRepository(dataFolder);
            var athletes = new AthletesJsonRepository(dataFolder);
            var start = new StartingListJsonRepository(dataFolder);
            var race = new RaceJsonRepository(dataFolder);
            return new RepositorySet(race, start, race, race, race, athletes, judges);
        }
    }
}
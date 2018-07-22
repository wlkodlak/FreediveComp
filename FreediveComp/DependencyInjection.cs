﻿using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using System.Configuration;
using Unity;
using Unity.Exceptions;
using MilanWilczak.FreediveComp.Api;
using MilanWilczak.FreediveComp.Models;
using Unity.Injection;

namespace MilanWilczak.FreediveComp
{
    public class DependencyInjection
    {
        public PeristenceKind PersistenceKind { get; set; }
        public string PersistencePath { get; set; }

        public IUnityContainer BuildContainer()
        {
            UnityContainer container = new UnityContainer();
            container.RegisterType<IApiAthlete, ApiAthlete>();
            container.RegisterType<IApiAuthentication, ApiAuthentication>();
            container.RegisterType<IApiReports, ApiReports>();
            container.RegisterType<IApiRules, ApiRules>();
            container.RegisterType<IApiSearch, ApiSearch>();
            container.RegisterType<IApiSetup, ApiSetup>();
            container.RegisterType<IApiStartingList, ApiStartingList>();
            container.RegisterType<IStartingLanesFlatBuilder, StartingLanesFlatBuilder>();
            container.RegisterType<IRulesRepository>(new InjectionFactory(RulesRepositoryFactory));

            if (PersistenceKind == PeristenceKind.InMemory)
            {
                container.RegisterInstance<IRepositorySetProvider>(new RepositorySetMemoryProvider());
                container.RegisterInstance<IRacesIndexRepository>(new RacesIndexMemoryRepository());
            }
            else
            {
                if (PersistenceKind == PeristenceKind.RealFolder)
                {
                    container.RegisterInstance<IDataFolder>(new DataFolderReal(PersistencePath));
                }
                else
                {
                    container.RegisterInstance<IDataFolder>(new DataFolderMemory());
                }
                container.RegisterSingleton<IRepositorySetProvider, RepositorySetJsonProvider>();
                container.RegisterSingleton<IRacesIndexRepository, RacesIndexJsonRepository>();
            }

            return container;
        }

        private object RulesRepositoryFactory(IUnityContainer container)
        {
            RulesRepository rulesRepository = new RulesRepository();
            Rules.AddTo(rulesRepository);
            return rulesRepository;
        }
    }

    public class UnityResolver : IDependencyResolver
    {
        private IUnityContainer unity;

        public UnityResolver(IUnityContainer unity)
        {
            this.unity = unity;
        }

        public IDependencyScope BeginScope()
        {
            return new UnityResolver(unity.CreateChildContainer());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                unity.Dispose();
            }
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return unity.Resolve(serviceType);
            }
            catch (ResolutionFailedException)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return unity.ResolveAll(serviceType);
            }
            catch (ResolutionFailedException)
            {
                return null;
            }
        }
    }

    public enum PeristenceKind
    {
        RealFolder,
        VirtualFolder,
        InMemory
    }

    public class AppConfiguration
    {
        public static PeristenceKind GetPersistenceKind()
        {
            string kind = ConfigurationManager.AppSettings["persistence:kind"];
            if (string.IsNullOrEmpty(kind)) return PeristenceKind.RealFolder;
            if (string.Equals(kind, "Folder", StringComparison.InvariantCultureIgnoreCase)) return PeristenceKind.RealFolder;
            if (string.Equals(kind, "RealFolder", StringComparison.InvariantCultureIgnoreCase)) return PeristenceKind.RealFolder;
            if (string.Equals(kind, "VirtualFolder", StringComparison.InvariantCultureIgnoreCase)) return PeristenceKind.VirtualFolder;
            if (string.Equals(kind, "Virtual", StringComparison.InvariantCultureIgnoreCase)) return PeristenceKind.VirtualFolder;
            if (string.Equals(kind, "Memory", StringComparison.InvariantCultureIgnoreCase)) return PeristenceKind.InMemory;
            if (string.Equals(kind, "InMemory", StringComparison.InvariantCultureIgnoreCase)) return PeristenceKind.InMemory;
            throw new ConfigurationErrorsException("Illegal persistence kind " + kind);
        }

        public static string GetPersistencePath()
        {
            string path = ConfigurationManager.AppSettings["persistence:path"];
            if (string.IsNullOrEmpty(path))
            {
                path = IsWebBased() ? "%BASEFOLDER%/App_Data" : "%APPDATA%/MilanWilczak.FreediveComp";
            }
            if (path.Contains("%APPDATA%")) path = path.Replace("%APPDATA%", ResolveAppDataFolder());
            if (path.Contains("%BASEFOLDER%")) path = path.Replace("%BASEFOLDER%", ResolveAppDomainFolder());
            return path;
        }

        private static bool IsWebBased()
        {
            return true;
        }

        private static string ResolveAppDataFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private static string ResolveAppDomainFolder()
        {
            return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        }
    }
}
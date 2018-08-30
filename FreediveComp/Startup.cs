using MilanWilczak.FreediveComp.Controllers;
using Newtonsoft.Json;
using Owin;
using System;
using System.Web.Http;
using System.Web.Http.Cors;
using Unity;

namespace MilanWilczak.FreediveComp
{
    public class Startup
    {
        private static IUnityContainer container;

        public static IUnityContainer Container
        {
            get
            {
                if (container != null) return container;
                lock (typeof(Startup))
                {
                    if (container != null) return container;

                    JsonConvert.DefaultSettings = CreateJsonSettings;

                    var dependencyInjection = new DependencyInjection();
                    dependencyInjection.PersistenceKind = AppConfiguration.GetPersistenceKind();
                    dependencyInjection.PersistencePath = AppConfiguration.GetPersistencePath();
                    container = dependencyInjection.BuildContainer();
                    return container;
                }
            }
        }

        public static JsonSerializerSettings CreateJsonSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public static void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.SuppressDefaultHostAuthentication();
            config.MapHttpAttributeRoutes();
            config.ParameterBindingRules.Insert(0, PrincipalBinder.BindingRule);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Filters.Add(Container.Resolve<TokenAuthenticationFilter>());
            config.Filters.Add(Container.Resolve<IpAuthenticationFilter>());
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.DependencyResolver = new UnityResolver(Container);
            config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
            app.UseWebApi(config);

            app.UseUdpDiscovery();

            app.UseFiles(AppConfiguration.GetUiPath());
        }
    }
}
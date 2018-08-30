using MilanWilczak.FreediveComp.Controllers;
using Owin;
using System.Web.Http;
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

                    var dependencyInjection = new DependencyInjection();
                    dependencyInjection.PersistenceKind = AppConfiguration.GetPersistenceKind();
                    dependencyInjection.PersistencePath = AppConfiguration.GetPersistencePath();
                    container = dependencyInjection.BuildContainer();
                    return container;
                }
            }
        }

        public static void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.SuppressDefaultHostAuthentication();
            config.MapHttpAttributeRoutes();
            config.ParameterBindingRules.Insert(0, PrincipalBinder.BindingRule);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Filters.Add(Container.Resolve<TokenAuthenticationFilter>());
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.DependencyResolver = new UnityResolver(Container);
            app.UseWebApi(config);

            app.UseUdpDiscovery();

            app.UseFiles(AppConfiguration.GetUiPath());
        }
    }
}
using FreediveComp.Controllers;
using Owin;
using System.Web.Http;
using Unity;

namespace FreediveComp
{
    public class Startup
    {
        public static void Configure(IAppBuilder app)
        {
            var container = DependencyInjection.Initialize();

            HttpConfiguration config = new HttpConfiguration();
            config.SuppressDefaultHostAuthentication();
            config.MapHttpAttributeRoutes();
            config.ParameterBindingRules.Insert(0, PrincipalBinder.BindingRule);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Filters.Add(container.Resolve<TokenAuthenticationFilter>());
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.DependencyResolver = new UnityResolver(container);
            app.UseWebApi(config);
        }
    }
}
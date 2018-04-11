using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace MilanWilczak.FreediveComp.Controllers
{
    public class PrincipalBinder : HttpParameterBinding
    {
        public static HttpParameterBinding BindingRule(HttpParameterDescriptor parameter)
        {
            if (typeof(IPrincipal).IsAssignableFrom(parameter.ParameterType))
            {
                return new PrincipalBinder(parameter);
            }
            else
            {
                return null;
            }
        }

        private PrincipalBinder(HttpParameterDescriptor descriptor) 
            : base(descriptor)
        {
        }

        public override Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var principal = actionContext.RequestContext.Principal;
            if (principal != null && Descriptor.ParameterType.IsAssignableFrom(principal.GetType()))
            {
                actionContext.ActionArguments[Descriptor.ParameterName] = principal;
            }
            return Task.FromResult(0);
        }
    }
}
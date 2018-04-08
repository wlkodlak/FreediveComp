using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace FreediveComp.Controllers
{
    public class FromHeaderAttribute : ParameterBindingAttribute
    {
        private readonly string headerName;

        public FromHeaderAttribute()
        {
        }

        public FromHeaderAttribute(string headerName)
        {
            this.headerName = headerName;
        }

        public override HttpParameterBinding GetBinding(HttpParameterDescriptor parameter)
        {
            if (parameter.ParameterType == typeof(string))
            {
                return new FromHeaderBinding(parameter, headerName);
            }
            else
            {
                return parameter.BindAsError("Only strings are supported by FromHeaderAttribute");
            }
        }
    }

    public class FromHeaderBinding : HttpParameterBinding
    {
        private readonly string headerName;

        public FromHeaderBinding(HttpParameterDescriptor parameter, string headerName)
            : base(parameter)
        {
            this.headerName = headerName;
        }

        public override Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            TaskCompletionSource<object> task = new TaskCompletionSource<object>();
            try
            {
                var headers = actionContext.Request.Headers;
                var name = headerName ?? Descriptor.ParameterName;
                var value = headers.GetValues(name).FirstOrDefault();
                actionContext.ActionArguments[Descriptor.ParameterName] = value;
                task.SetResult(null);
            }
            catch (Exception e)
            {
                task.SetException(e);
            }
            return task.Task;
        }
    }
}
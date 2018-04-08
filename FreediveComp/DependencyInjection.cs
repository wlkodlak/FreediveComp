using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Unity;
using Unity.Exceptions;

namespace FreediveComp
{
    public class DependencyInjection
    {
        public static IUnityContainer Initialize()
        {
            UnityContainer container = new UnityContainer();
            



            return container;
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
            unity.Dispose();
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
}
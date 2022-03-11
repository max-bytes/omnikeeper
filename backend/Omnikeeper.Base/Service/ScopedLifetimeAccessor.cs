using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Omnikeeper.Base.Service
{
    // NOTE: this class uses the same mechanism as the HttpContextAccessor implementation to provide a local instance of lifetimeScope per request
    public class ScopedLifetimeAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        private static AsyncLocal<LifetimeScopeHolder> _lifetimeScopeCurrent = new AsyncLocal<LifetimeScopeHolder>();

        public ScopedLifetimeAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void SetLifetimeScope(ILifetimeScope scope)
        {
            var holder = _lifetimeScopeCurrent.Value;
            if (holder != null)
            {
                holder.LifetimeScope = null;
            }

            if (scope != null)
            {
                // Use an object indirection to hold the LifetimeScope in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                _lifetimeScopeCurrent.Value = new LifetimeScopeHolder { LifetimeScope = scope };
            }
        }
        public void ResetLifetimeScope()
        {
            var holder = _lifetimeScopeCurrent.Value;
            if (holder != null)
            {
                holder.LifetimeScope = null;
            }
        }

        public ILifetimeScope? GetLifetimeScope()
        {
            if (_lifetimeScopeCurrent.Value != null)
                return _lifetimeScopeCurrent.Value?.LifetimeScope;
            else if (httpContextAccessor.HttpContext != null)
            {
                var ls = httpContextAccessor.HttpContext.RequestServices.GetRequiredService<ILifetimeScope>();
                return ls;
            }
            else
            {
                return null;
            }
        }

        private class LifetimeScopeHolder
        {
            public ILifetimeScope? LifetimeScope;
        }
    }
}

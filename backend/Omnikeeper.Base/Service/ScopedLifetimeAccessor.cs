using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Omnikeeper.Base.Service
{
    public class ScopedLifetimeAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        private ILifetimeScope? lifetimeScope;

        public ScopedLifetimeAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void SetLifetimeScope(ILifetimeScope scope)
        {
            lifetimeScope = scope;
        }
        public void ResetLifetimeScope()
        {
            lifetimeScope = null;
        }

        public ILifetimeScope? GetLifetimeScope()
        {
            if (lifetimeScope != null)
                return lifetimeScope;
            else if (httpContextAccessor.HttpContext != null)
            {
                var ls = httpContextAccessor.HttpContext.RequestServices.GetRequiredService<ILifetimeScope>();
                return ls;
            } else
            {
                return null;
            }
        }
    }
}

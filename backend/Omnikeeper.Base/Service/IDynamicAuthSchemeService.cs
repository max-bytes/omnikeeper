﻿using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IDynamicAuthSchemeService
    {
        Task<bool> TryAdd(string scheme, System.Type handlerType, AuthenticationSchemeOptions? options = null);
    }
}

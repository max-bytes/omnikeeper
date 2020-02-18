﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Integration.GraphQL
{
    public class SimpleContainerAdapter : IServiceProvider
    {
        private readonly ISimpleContainer _container;

        public SimpleContainerAdapter(ISimpleContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType)
        {
            return _container.Get(serviceType);
        }
    }
}

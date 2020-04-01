using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public class AuthorizationService
    {
        private IUserInDatabaseModel UserModel { get; }
        public AuthorizationService(IUserInDatabaseModel userModel)
        {
            UserModel = userModel;
        }

        //public async Task<bool> CanWriteToLayer(UserInDatabase user, Layer layer)
        //{
        //}
    }
}

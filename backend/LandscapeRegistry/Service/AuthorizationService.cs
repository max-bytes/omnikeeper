using Landscape.Base.Model;

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

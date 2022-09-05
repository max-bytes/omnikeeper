using Omnikeeper.Base.Authz;

namespace Omnikeeper.Base.Entity
{
    public interface IAuthenticatedUser
    {
        UserInDatabase InDatabase { get; }
        string Username { get; }
    }

    public class AuthenticatedInternalUser : IAuthenticatedUser
    {
        public UserInDatabase InDatabase { get; }
        public string Username => InDatabase.Username;

        public AuthenticatedInternalUser(UserInDatabase inDatabase)
        {
            InDatabase = inDatabase;
        }
    }

    public class AuthenticatedHttpUser : IAuthenticatedUser
    {
        public UserInDatabase InDatabase { get; }
        public AuthRole[] AuthRoles { get; }
        public HttpUser HttpUser { get; }

        public string Username => InDatabase.Username;

        public AuthenticatedHttpUser(UserInDatabase inDatabase, AuthRole[] authRoles, HttpUser httpUser)
        {
            InDatabase = inDatabase;
            AuthRoles = authRoles;
            HttpUser = httpUser;
        }
    }
}

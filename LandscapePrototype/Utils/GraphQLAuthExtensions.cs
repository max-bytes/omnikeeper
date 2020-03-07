//using GraphQL.Authorization;
//using GraphQL.Validation;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.DependencyInjection.Extensions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace LandscapePrototype.Utils
//{
//    public static class GraphQLAuthExtensions
//    {
//        public static void AddGraphQLAuth(this IServiceCollection services)
//        {
//            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
//            services.TryAddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
//            services.AddTransient<IValidationRule, AuthorizationValidationRule>();

//            services.TryAddSingleton(s =>
//            {
//                var authSettings = new AuthorizationSettings();

//                //authSettings.AddPolicy("AdminPolicy", _ => _.RequireClaim("role", "Admin"));
//                authSettings.AddPolicy("AuthenticatedUser", _ => _.AddRequirement(new AuthenticatedUserRequirement()));

//                return authSettings;
//            });
//        }
//    }
//}

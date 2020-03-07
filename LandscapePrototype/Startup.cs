using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using GraphQL.Validation;
using Landscape.Base;
using Landscape.Base.Model;
using LandscapePrototype.Entity.Converters;
using LandscapePrototype.Entity.GraphQL;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using NetCore.AutoRegisterDi;
using Npgsql;
using Plugin;

namespace LandscapePrototype
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IServiceProvider>(x => new FuncServiceProvider(x.GetRequiredService)); // graphql needs this

            var testAssembly = Assembly.LoadFrom(@"C:\Users\Maximilian Csuk\Projects\Landscape\TestPlugin\bin\Debug\netstandard2.1\TestPlugin.dll");

            services.RegisterAssemblyPublicNonGenericClasses(testAssembly)
                .Where(a => {
                    return true;// a.GetInterfaces().Contains(typeof(ILandscapePluginRegistry));
                    })
                .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);
            services.AddSingleton<IPluginRegistry, PluginRegistry>();

            services.AddCors(options => options.AddPolicy("AllowAllOrigins", builder =>
               builder.WithOrigins("http://localhost:3000")
               .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader())
            );

            //.AddJsonOptions(options =>
            // {
            //     options.JsonSerializerOptions.Converters.Add(new AttributeValueConverter());
            //     options.JsonSerializerOptions.MaxDepth = 64; // graphql output can be big, allow big jsons
            // })
            services.AddControllers().AddNewtonsoftJson();

            services.AddTransient((sp) =>
            {
                var dbcb = new DBConnectionBuilder();
                return dbcb.Build("landscape_prototype");
            });

            services.AddScoped<ICIModel, CIModel>();
            services.AddScoped<CIModel>();

            services.AddScoped<ILayerModel, LayerModel>();
            services.AddScoped<LayerModel>();
            services.AddScoped<RelationModel>();
            services.AddScoped<IChangesetModel, ChangesetModel>();
            services.AddScoped<ChangesetModel>();
            services.AddScoped<LandscapeUserContext>();

            services.AddScoped<CIType>();
            services.AddScoped<RelationType>();
            services.AddScoped<ISchema, LandscapeSchema>();

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddGraphQL(x =>
            {
                x.ExposeExceptions = CurrentEnvironment.IsDevelopment(); //set true only in development mode. make it switchable.
            })
            .AddGraphTypes(ServiceLifetime.Scoped)
            .AddUserContextBuilder<LandscapeUserContext>((httpContext) => new LandscapeUserContext());

            services.AddSingleton<IDocumentExecuter, MyDocumentExecutor>(); // custom document executor that does serial queries, required by postgres

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = "landscape",
                };
                options.Authority = "http://localhost:8080/auth/realms/landscape";
                options.Audience = "landscape";
                options.RequireHttpsMetadata = false;
                options.Events = new JwtBearerEvents()
                {
                    OnForbidden = c =>
                    {
                        Console.WriteLine(c);
                        return c.Response.WriteAsync("blub");
                    },
                    //OnAuthenticationFailed = c =>
                    //{
                    //    c.NoResult();

                    //    c.Response.StatusCode = 500;
                    //    c.Response.ContentType = "text/plain";
                    //    if (CurrentEnvironment.IsDevelopment())
                    //    {
                    //        return c.Response.WriteAsync(c.Exception.ToString());
                    //    }
                    //    return c.Response.WriteAsync("An error occured processing your authentication.");
                    //}
                };
                options.Validate();
            });

            services.AddAuthorization(options =>
            {
                //options.AddPolicy("AuthenticatedUser", _ => _.AddRequirements(new AuthenticatedUserRequirement()));
                //options.AddPolicy("Accounting", policy =>
                //policy.RequireClaim("member_of", "[accounting]")); //this claim value is an array. Any suggestions how to extract just single role? This still works.
            });

            //services.AddGraphQLAuth();
        }

        private IWebHostEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IEnumerable<IComputeLayerBrain> computeLayers, IPluginRegistry pluginRegistry)
        {
            pluginRegistry.RegisterComputeLayerBrains(computeLayers);

            app.UseCors("AllowAllOrigins");

            //app.UseGraphQL<LandscapeSchema>();

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseGraphQLPlayground(new GraphQLPlaygroundOptions()); //to explorer API navigate https://*DOMAIN*/ui/playground

                IdentityModelEventSource.ShowPII = true;
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // TODO: make run in cron
            computeLayers.First().Run().GetAwaiter().GetResult();
        }

        //public static void UseGraphQLWithAuth(this IApplicationBuilder app)
        //{
        //    var settings = new GraphQLSettings
        //    {
        //        BuildUserContext = ctx =>
        //        {
        //            var userContext = new GraphQLUserContext
        //            {
        //                User = ctx.User
        //            };

        //            return Task.FromResult(userContext);
        //        }
        //    };

        //    var rules = app.ApplicationServices.GetServices<IValidationRule>();
        //    settings.ValidationRules.AddRange(rules);

        //    app.UseMiddleware<GraphQLMiddleware>(settings);
        //}


        //public class GraphQLSettings
        //{
        //    public Func<HttpContext, Task<object>> BuildUserContext { get; set; }
        //    public object Root { get; set; }
        //    public List<IValidationRule> ValidationRules { get; } = new List<IValidationRule>();
        //}
    }
}

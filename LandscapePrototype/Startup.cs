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
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.AspNetCore;
using Hangfire.Console;
using LandscapePrototype.Model.Cached;
using TestPlugin;
using Hangfire.Dashboard;
using Hangfire.Annotations;

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

            // add plugins
            //var testAssembly = Assembly.LoadFrom(@"C:\Users\Maximilian Csuk\Projects\Landscape\TestPlugin\bin\Debug\netstandard2.1\TestPlugin.dll");
            //services.RegisterAssemblyPublicNonGenericClasses(testAssembly)
            //    .Where(a => {
            //        return true;// a.GetInterfaces().Contains(typeof(ILandscapePluginRegistry));
            //        })
            //    .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);
            services.AddScoped<IComputeLayerBrain, CLBMonitoring>();

            services.AddCors(options => options.AddPolicy("AllowAllOrigins", builder =>
               builder.WithOrigins("http://localhost:3000")
               .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader())
            );

            services.AddControllers().AddNewtonsoftJson();

            services.AddScoped((sp) =>
            {
                var dbcb = new DBConnectionBuilder();
                return dbcb.Build(Configuration);
            });

            // TODO: remove AddScoped<Model>(), only use AddScoped<IModel, Model>()
            services.AddScoped<ICIModel, CIModel>();
            services.AddScoped<CIModel>();
            services.AddScoped<IUserModel, UserModel>();
            services.AddScoped<UserModel>();
            services.AddScoped<ILayerModel, LayerModel>();
            services.AddScoped<LayerModel>();
            services.AddScoped<CachedLayerModel>();
            services.AddScoped<IRelationModel, RelationModel>();
            services.AddScoped<RelationModel>();
            services.AddScoped<IChangesetModel, ChangesetModel>();
            services.AddScoped<ChangesetModel>();
            services.AddScoped<ITemplateModel, TemplateModel>();
            services.AddScoped<TemplateModel>();

            services.AddScoped<PredicateModel>();

            services.AddSingleton<TemplatesProvider>(); // can be singleton because it does not depend on any scoped services
            services.AddSingleton<CachedTemplatesProvider>(); // can be singleton because it does not depend on any scoped services

            services.AddScoped<MergedCIType>();
            services.AddScoped<RelationType>();
            services.AddScoped<ISchema, LandscapeSchema>();

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddGraphQL(x =>
            {
                x.ExposeExceptions = CurrentEnvironment.IsDevelopment() || CurrentEnvironment.IsStaging(); //set true only in development mode. make it switchable.
            })
            .AddGraphTypes(ServiceLifetime.Scoped);

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
                options.Authority = Configuration["AuthenticationAuthority"];
                options.Audience = "landscape";
                options.RequireHttpsMetadata = false;
                options.Events = new JwtBearerEvents()
                {
                    //OnForbidden = c =>
                    //{
                    //    Console.WriteLine(c);
                    //    return c.Response.WriteAsync("blub");
                    //},
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

            services.AddScoped<CLBRunner>();
            services.AddHangfire(config =>
            {
                var cs = Configuration.GetConnectionString("HangfireConnection");
                config.UsePostgreSqlStorage(cs);
                //config.UseConsole();
            });

        }

        private IWebHostEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceScopeFactory serviceScopeFactory)
        {
            app.UseCors("AllowAllOrigins");

            app.UseStaticFiles();

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();

                app.UseGraphQLPlayground(new GraphQLPlaygroundOptions()); //to explorer API navigate https://*DOMAIN*/ui/playground

                IdentityModelEventSource.ShowPII = true; // to show more debugging information
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            // Configure hangfire to use the new JobActivator we defined.
            GlobalConfiguration.Configuration.UseConsole().UseActivator(new AspNetCoreJobActivator(serviceScopeFactory));
            app.UseHangfireServer();
            if (env.IsDevelopment())
            {
                app.UseHangfireDashboard(options: new DashboardOptions()
                {
                    Authorization = new IDashboardAuthorizationFilter[] { new HangFireAuthorizationFilter() }
                });
            }

            RecurringJob.AddOrUpdate<CLBRunner>(runner => runner.Run(), Cron.Daily);// "*/15 * * * * *");
        }
    }

    // in a docker-based environment, we need a custom authorization filter for the hangfire dashboard because non-localhost access is blocked by default
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }

    public class CLBRunner
    {
        public CLBRunner(IEnumerable<IComputeLayerBrain> computeLayerBrains)
        {
            ComputeLayerBrains = computeLayerBrains;
        }

        public void Run()
        {
            Console.WriteLine("Running CLBRunner");
            foreach (var clb in ComputeLayerBrains)
            {
                var settings = new CLBSettings("Monitoring"); // TODO
                clb.RunSync(settings);
            }
        }

        private IEnumerable<IComputeLayerBrain> ComputeLayerBrains { get; }
    }
}

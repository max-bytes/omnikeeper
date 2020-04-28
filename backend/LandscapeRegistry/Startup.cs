using DBMigrations;
using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.AspNetCore;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Landscape.Base;
using Landscape.Base.Model;
using LandscapeRegistry.GraphQL;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using LandscapeRegistry.Runners;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MonitoringPlugin;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LandscapeRegistry
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

            services.AddApiVersioning();

            // add plugins
            //var testAssembly = Assembly.LoadFrom(@"C:\Users\Maximilian Csuk\Projects\Landscape\TestPlugin\bin\Debug\netstandard2.1\TestPlugin.dll");
            //services.RegisterAssemblyPublicNonGenericClasses(testAssembly)
            //    .Where(a => {
            //        return true;// a.GetInterfaces().Contains(typeof(ILandscapePluginRegistry));
            //        })
            //    .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);

            // register compute layer brains
            services.AddScoped<IComputeLayerBrain, CLBMonitoring>();

            services.AddCors(options => options.AddPolicy("AllowAllOrigins", builder =>
               builder.WithOrigins(Configuration.GetSection("CORS")["AllowedHosts"].Split(","))
               .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader())
            );

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                // enums to string conversion
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });

            services.AddScoped((sp) =>
            {
                var dbcb = new DBConnectionBuilder();
                return dbcb.Build(Configuration);
            });

            services.AddHttpContextAccessor();

            // TODO: remove AddScoped<Model>(), only use AddScoped<IModel, Model>()
            services.AddScoped<ICISearchModel, CISearchModel>();
            services.AddScoped<ICIModel, CIModel>();
            services.AddScoped<CIModel>();
            services.AddScoped<IAttributeModel, AttributeModel>();
            services.AddScoped<AttributeModel>();
            services.AddScoped<IUserInDatabaseModel, UserInDatabaseModel>();
            services.AddScoped<UserInDatabaseModel>();
            services.AddScoped<ILayerModel, LayerModel>();
            services.AddScoped<LayerModel>();
            services.AddScoped<CachedLayerModel>();
            services.AddScoped<IRelationModel, RelationModel>();
            services.AddScoped<RelationModel>();
            services.AddScoped<IChangesetModel, ChangesetModel>();
            services.AddScoped<ChangesetModel>();
            services.AddScoped<ITemplateModel, TemplateModel>();
            services.AddScoped<TemplateModel>();
            services.AddScoped<IPredicateModel, PredicateModel>();
            services.AddScoped<PredicateModel>();
            services.AddScoped<CachedPredicateModel>();
            services.AddScoped<KeycloakModel>();

            services.AddScoped<ITraitModel, TraitModel>();
            services.AddScoped<TraitModel>();

            services.AddScoped<AuthorizationService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<MarkedForDeletionService>();
            services.AddScoped<IngestDataService>();

            services.AddSingleton<ITemplatesProvider, CachedTemplatesProvider>(); // can be singleton because it does not depend on any scoped services
            services.AddSingleton<TemplatesProvider>(); // can be singleton because it does not depend on any scoped services
            services.AddSingleton<ITraitsProvider, CachedTraitsProvider>(); // can be singleton because it does not depend on any scoped services
            services.AddSingleton<TraitsProvider>(); // can be singleton because it does not depend on any scoped services

            services.AddScoped<MergedCIType>();
            services.AddScoped<RelationType>();
            services.AddScoped<ISchema, RegistrySchema>();

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
                    ValidAudience = Configuration.GetSection("Authentication")["Audience"]
                };
                options.Authority = Configuration.GetSection("Authentication")["Authority"];
                options.Audience = Configuration.GetSection("Authentication")["Audience"];
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

            services.AddHangfire(config =>
            {
                var cs = Configuration.GetConnectionString("HangfireConnection");
                config.UsePostgreSqlStorage(cs);
                //config.UseConsole(); //TODO
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Landscape Registry REST API", Version = "v1" });
                var filePath = Path.Combine(AppContext.BaseDirectory, "LandscapeRegistry.xml");
                c.IncludeXmlComments(filePath);
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Scheme = "basic",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Flows = new OpenApiOAuthFlows
                    {
                        ClientCredentials = new OpenApiOAuthFlow
                        {
                            Scopes = new Dictionary<string, string> { },
                            AuthorizationUrl = new Uri(Configuration.GetSection("Authentication")["Authority"] + "/protocol/openid-connect/auth", UriKind.Absolute),
                            TokenUrl = new Uri(Configuration.GetSection("Authentication")["Authority"] + "/protocol/openid-connect/token", UriKind.Absolute),
                        }
                    }
                });
                c.OperationFilter<AuthenticationRequirementsOperationFilter>();

                // Use method name as operationId
                c.CustomOperationIds(apiDesc => apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null);
            });
            services.AddSwaggerGenNewtonsoftSupport();
        }
        public class AuthenticationRequirementsOperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                if (operation.Security == null)
                    operation.Security = new List<OpenApiSecurityRequirement>();
                var scheme = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } };
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [scheme] = new List<string>()
                });
            }
        }

        private IWebHostEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceScopeFactory serviceScopeFactory, IHostApplicationLifetime hostApplicationLifetime)
        {
            // run database migrations
            // NOTE: is now run in own executable running before app itself starts
            //var cs = Configuration.GetConnectionString("LandscapeDatabaseConnection");
            //var migrationResult = DBMigration.Migrate(cs);
            //if (!migrationResult.Successful)
            //    throw new Exception("Database migration failed!", migrationResult.Error);

            app.UseCors("AllowAllOrigins");

            app.UseStaticFiles();

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();

                IdentityModelEventSource.ShowPII = true; // to show more debugging information
            }

            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());

            if (env.IsDevelopment())
            {
                // non-dev environments are behind an ssl proxy, so no https redirect required
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "backend/{documentName}/swagger.json"; // TEST
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "Landscape Registry REST API V1");
                c.RoutePrefix = "backend"; // TEST
                //c.RoutePrefix = ((Configuration["BaseURL"].Length == 0) ? "" : Configuration["BaseURL"] + "/") + "swagger";
                c.OAuthClientId("landscape-registry-api");
                c.OAuthClientSecret(Configuration.GetSection("SwaggerUI")["OAuthClientSecret"]);
            });

            // Configure hangfire to use the new JobActivator we defined.
            GlobalConfiguration.Configuration
                .UseConsole()
                .UseActivator(new AspNetCoreJobActivator(serviceScopeFactory));
            app.UseHangfireServer();
            if (env.IsDevelopment() || env.IsStaging())
            { // TODO: also use in production, but fix auth first
                // workaround, see: https://github.com/HangfireIO/Hangfire/issues/1110
                app.Use((context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/hangfire"))
                    {
                        context.Request.PathBase = new PathString(context.Request.Headers["X-Forwarded-Prefix"]);
                    }
                    return next();
                });

                app.UseHangfireDashboard(options: new DashboardOptions()
                {
                    AppPath = null,
                    Authorization = new IDashboardAuthorizationFilter[] { new HangFireAuthorizationFilter() }
                });
            }

            RecurringJob.AddOrUpdate<CLBRunner>(runner => runner.Run(), Cron.Minutely);// "*/15 * * * * *");
            RecurringJob.AddOrUpdate<MarkedForDeletionRunner>(s => s.Run(), Cron.Minutely);
        }
    }

    // in a docker-based environment, we need a custom authorization filter for the hangfire dashboard because non-localhost access is blocked by default
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true; // TODO: proper auth
        }
    }
}

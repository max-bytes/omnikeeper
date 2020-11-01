using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.AspNetCore;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using Omnikeeper.Ingest.ActiveDirectoryXML;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Runners;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Npgsql.Logging;
using OKPluginCLBMonitoring;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using FluentValidation.AspNetCore;

namespace Omnikeeper
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
            services.AddScoped<IServiceProvider>(x =>
                new FuncServiceProvider(x.GetRequiredService)
                ); // graphql needs this

            services.AddApiVersioning();

            services.AddMediatR(Assembly.GetExecutingAssembly());

            // add plugins
            //var testAssembly = Assembly.LoadFrom(@"C:\Users\Maximilian Csuk\Projects\Landscape\TestPlugin\bin\Debug\netstandard2.1\TestPlugin.dll");
            //services.RegisterAssemblyPublicNonGenericClasses(testAssembly)
            //    .Where(a => {
            //        return true;// a.GetInterfaces().Contains(typeof(ILandscapePluginRegistry));
            //        })
            //    .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);

            // register compute layer brains
            //services.AddScoped<IComputeLayerBrain, CLBMonitoring>();
            services.AddScoped<IComputeLayerBrain, CLBNaemonMonitoring>();

            // register online inbound adapters and managers
            services.AddSingleton<IExternalIDMapper, ExternalIDMapper>();
            services.AddSingleton<IExternalIDMapPersister, ExternalIDMapPostgresPersister>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIAKeycloak.OnlineInboundAdapter.Builder>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIAKeycloak.OnlineInboundAdapter.BuilderInternal>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIAOmnikeeper.OnlineInboundAdapter.Builder>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIASharepoint.OnlineInboundAdapter.Builder>();
            services.AddScoped<IInboundAdapterManager, InboundAdapterManager>();

            services.AddCors(options => options.AddPolicy("DefaultCORSPolicy", builder =>
               builder.WithOrigins(Configuration.GetSection("CORS")["AllowedHosts"].Split(","))
               .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader())
            );

            services.AddOData();//.EnableApiVersioning();

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                // enums to string conversion
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });

            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.Build(Configuration);
            });

            services.AddHttpContextAccessor();

            services.AddScoped<ICISearchModel, CISearchModel>();
            services.AddScoped<ICIModel, CIModel>();
            services.AddScoped<IAttributeModel, AttributeModel>();
            services.AddScoped<IBaseAttributeModel, BaseAttributeModel>();
            services.Decorate<IBaseAttributeModel, CachingBaseAttributeModel>();
            services.Decorate<IBaseAttributeModel, OIABaseAttributeModel>();
            services.AddScoped<IUserInDatabaseModel, UserInDatabaseModel>();
            services.AddScoped<ILayerModel, LayerModel>();
            services.Decorate<ILayerModel, CachingLayerModel>();
            services.AddScoped<ILayerStatisticsModel, LayerStatisticsModel>();
            services.AddScoped<IRelationModel, RelationModel>();
            services.AddScoped<IBaseRelationModel, BaseRelationModel>();
            services.Decorate<IBaseRelationModel, CachingBaseRelationModel>();
            services.Decorate<IBaseRelationModel, OIABaseRelationModel>();
            services.AddScoped<IChangesetModel, ChangesetModel>();
            services.AddScoped<ITemplateModel, TemplateModel>();
            services.AddScoped<IPredicateModel, PredicateModel>();
            services.Decorate<IPredicateModel, CachingPredicateModel>();
            services.AddScoped<IMemoryCacheModel, MemoryCacheModel>();
            services.AddScoped<IODataAPIContextModel, ODataAPIContextModel>();
            services.Decorate<IODataAPIContextModel, CachingODataAPIContextModel>();

            services.AddScoped<IRecursiveTraitModel, RecursiveTraitModel>();
            services.Decorate<IRecursiveTraitModel, CachingRecursiveTraitModel>();
            services.AddScoped<IEffectiveTraitModel, EffectiveTraitModel>();

            services.AddScoped<IOIAConfigModel, OIAConfigModel>();

            services.AddScoped<IRegistryAuthorizationService, RegistryAuthorizationService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<MarkedForDeletionService>();
            services.AddScoped<IngestDataService>();

            services.AddScoped<ITemplatesProvider, TemplatesProvider>();
            services.Decorate<ITemplatesProvider, CachedTemplatesProvider>();

            services.AddScoped<MergedCIType>();
            services.AddScoped<RelationType>();
            services.AddScoped<ISchema, GraphQLSchema>();

            services.AddSingleton<NpgsqlLoggingProvider>();

            services.AddScoped<ITraitsProvider, TraitsProvider>();

            services.AddScoped<IOnlineAccessProxy, OnlineAccessProxy>();

            services.AddScoped<IngestActiveDirectoryXMLService, IngestActiveDirectoryXMLService>();

            services.AddScoped<CIMappingService, CIMappingService>();
          
            services.AddScoped<IGridViewConfigModel, GridViewConfigModel>();

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
                { // TODO: is this needed? According to https://developer.okta.com/blog/2018/03/23/token-authentication-aspnetcore-complete-guide, this should work automatically
                    ValidateAudience = true,
                    ValidAudience = Configuration.GetSection("Authentication")["Audience"]
                };

                // NOTE: according to https://social.technet.microsoft.com/Forums/en-US/2f889c6f-b500-4ba6-bba0-a2a4fee1604f/cannot-authenticate-odata-feed-using-an-organizational-account
                // windows applications want to receive an authorization_uri in the challenge response with an URI where the user can authenticate
                // unfortunately, this does not work as microsoft apps only seem to trust Azure AD, but not other IDPs
                //options.Challenge = $"Bearer authorization_uri=\"{Configuration.GetSection("Authentication")["Authority"]}/protocol/openid-connect/auth\"";
                options.Authority = Configuration.GetSection("Authentication")["Authority"];
                options.Audience = Configuration.GetSection("Authentication")["Audience"];
                options.RequireHttpsMetadata = false;
                options.Events = new JwtBearerEvents()
                {
                    OnForbidden = c =>
                    {
                        var logger = c.HttpContext.RequestServices.GetRequiredService<ILogger<IRegistryAuthorizationService>>();
                        logger.LogInformation($"Rejected user");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = c =>
                    {
                        var logger = c.HttpContext.RequestServices.GetRequiredService<ILogger<IRegistryAuthorizationService>>();
                        var userService = c.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
                        logger.LogInformation($"Validated token for user {userService.GetUsernameFromClaims(c.Principal.Claims) ?? "Unknown User"}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = c =>
                    {
                        var logger = c.HttpContext.RequestServices.GetRequiredService<ILogger<IRegistryAuthorizationService>>();
                        logger.LogError(c.Exception, $"Failure when trying to authenticate user");
                        return Task.CompletedTask;
                    }
                };
                options.Validate();
            });

            services.AddAuthorization(options =>
            {
                //options.AddPolicy("AuthenticatedUser", _ => _.AddRequirements(new AuthenticatedUserRequirement()));
                //options.AddPolicy("Accounting", policy =>
                //policy.RequireClaim("member_of", "[accounting]"));
            });

            services.AddHangfire(config =>
            {
                var cs = Configuration.GetConnectionString("HangfireConnection");
                config.UseMemoryStorage();
                config.UseFilter(new AutomaticRetryAttribute() { Attempts = 0 });
                config.UseConsole(); //TODO
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

            services.AddMemoryCache();

            // HACK: needed by odata, see: https://github.com/OData/WebApi/issues/2024
            services.AddMvcCore(options =>
            {
                foreach (var outputFormatter in options.OutputFormatters.OfType<OutputFormatter>().Where(x => x.SupportedMediaTypes.Count == 0))
                {
                    outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
                }

                foreach (var inputFormatter in options.InputFormatters.OfType<InputFormatter>().Where(x => x.SupportedMediaTypes.Count == 0))
                {
                    inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
                }

                // NOTE: in order for the OData service to work behind a reverse proxy, we need to modify the base URL
                // adding the BaseURL from the configuration and forcing https
                Uri ModifyBaseAddress(HttpRequest m)
                {
                    var logger = m.HttpContext.RequestServices.GetRequiredService<ILogger<IODataAPIContextModel>>();
                    var std = ODataInputFormatter.GetDefaultBaseAddress(m);
                    string newBaseURLPrefix;
                    string replaceStr;
                    if (CurrentEnvironment.IsDevelopment())
                    {
                        newBaseURLPrefix = $"https://{m.Host.Host}:{m.Host.Port}{Configuration["BaseURL"]}";
                        replaceStr = m.Scheme + "://" + m.Host.Host + ":" + m.Host.Port;
                    }
                    else
                    {
                        newBaseURLPrefix = $"https://{m.Host.Host}{Configuration["BaseURL"]}";
                        replaceStr = m.Scheme + "://" + m.Host.Host;
                    }
                    var oldBaseURL = std.ToString();
                    var newBaseURL = oldBaseURL.Replace(replaceStr, newBaseURLPrefix, StringComparison.InvariantCultureIgnoreCase);

                    logger.LogDebug($"Built new base URL prefix: {newBaseURLPrefix}");
                    logger.LogDebug($"Modifying base URL from {oldBaseURL} to {newBaseURL}");
                    return new Uri(newBaseURL);
                }
                foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>())
                {
                    outputFormatter.BaseAddressFactory = (m) => ModifyBaseAddress(m);
                }

                foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>())
                {
                    inputFormatter.BaseAddressFactory = (m) => ModifyBaseAddress(m);
                }
            }).AddFluentValidation();
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceScopeFactory serviceScopeFactory,
            NpgsqlLoggingProvider npgsqlLoggingProvider, ILogger<Startup> logger)
        {
            var version = VersionService.GetVersion();
            logger.LogInformation($"Running version: {version}");

            NpgsqlLogManager.Provider = npgsqlLoggingProvider;

            app.UseCors("DefaultCORSPolicy");

            // make application properly consider headers (and populate httprequest object) when behind reverse proxy
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

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
                // odata
                var builder = new ODataConventionModelBuilder(app.ApplicationServices);
                builder.EntitySet<Omnikeeper.Controllers.OData.AttributeDTO>("Attributes");
                builder.EntitySet<Omnikeeper.Controllers.OData.RelationDTO>("Relations");
                //endpoints.EnableDependencyInjection();
                endpoints.Select().Expand().Filter().OrderBy().Count();
                var edmModel = builder.GetEdmModel();
                //endpoints.MapODataRoute("odata", "api/v{version:apiVersion}/odata/{context}", edmModel);
                endpoints.MapODataRoute("odata", "api/odata/{context}", edmModel);

            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://{httpReq.Host.Value}{Configuration["BaseURL"]}" } };
                });
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{Configuration["BaseURL"]}/swagger/v1/swagger.json", "Landscape Registry REST API V1");
                c.OAuthClientId("landscape-registry-api");
                c.OAuthClientSecret(Configuration.GetSection("SwaggerUI")["OAuthClientSecret"]);
            });

            // Configure hangfire to use the new JobActivator we defined.
            GlobalConfiguration.Configuration
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


            RecurringJob.AddOrUpdate<CLBRunner>(s => s.Run(null), "*/15 * * * * *");
            RecurringJob.AddOrUpdate<MarkedForDeletionRunner>(s => s.Run(null), Cron.Minutely);
            RecurringJob.AddOrUpdate<ExternalIDManagerRunner>(s => s.Run(null), "*/5 * * * * *");
            RecurringJob.AddOrUpdate<ArchiveOldDataRunner>(s => s.Run(null), "*/5 * * * * *");
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

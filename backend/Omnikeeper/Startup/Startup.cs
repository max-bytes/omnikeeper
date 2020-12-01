using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Npgsql.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    public partial class Startup
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
            services.AddApiVersioning();

            services.AddCors(options => options.AddPolicy("DefaultCORSPolicy", builder =>
               builder.WithOrigins(Configuration.GetSection("CORS")["AllowedHosts"].Split(","))
               .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader())
            );

            services.AddOData();//.EnableApiVersioning();

            services.AddHttpContextAccessor();

            services.AddSignalR();

            var pluginFolder = Path.Combine(Directory.GetCurrentDirectory(), "OKPlugins");
            ServiceRegistration.RegisterLogging(services);
            ServiceRegistration.RegisterDB(services, Configuration);
            ServiceRegistration.RegisterOIABase(services);
            ServiceRegistration.RegisterModels(services, true, true);
            ServiceRegistration.RegisterServices(services);
            ServiceRegistration.RegisterGraphQL(services);
            var assemblies = ServiceRegistration.RegisterOKPlugins(services, pluginFolder);

            var mvcBuilder = services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    // enums to string conversion
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                });
            // load controllers from plugins
            foreach (var assembly in assemblies)
            {
                mvcBuilder.AddApplicationPart(assembly);
            }

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddGraphQL(x => { })
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = CurrentEnvironment.IsDevelopment() || CurrentEnvironment.IsStaging())
                .AddGraphTypes();

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
                        var logger = c.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerChallengeContext>>();
                        logger.LogInformation($"Rejected user");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = c =>
                    {
                        var logger = c.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerChallengeContext>>();
                        var userService = c.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
                        logger.LogInformation($"Validated token for user {userService.GetUsernameFromClaims(c.Principal.Claims) ?? "Unknown User"}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = c =>
                    {
                        var logger = c.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerChallengeContext>>();
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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Landscape omnikeeper REST API", Version = "v1" });
                var filePath = Path.Combine(AppContext.BaseDirectory, "omnikeeper.xml");
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
            });
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

                endpoints.MapHub<SignalRHubLogging>("/api/signalr/logging");
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
                c.SwaggerEndpoint($"{Configuration["BaseURL"]}/swagger/v1/swagger.json", "Landscape omnikeeper REST API V1");
                c.OAuthClientId("landscape-omnikeeper-api");
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
        }
    }
}

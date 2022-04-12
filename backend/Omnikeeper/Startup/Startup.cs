using Autofac;
using FluentValidation.AspNetCore;
using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using MediatR;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Plugins;
using Omnikeeper.GraphQL;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using SpanJson.AspNetCore.Formatter;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    public class Startup
    {
        private IMvcBuilder? mvcBuilder;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private TokenValidationParameters BuildTokenValidationParameters()
        {
            return new TokenValidationParameters
            { // TODO: is this needed? According to https://developer.okta.com/blog/2018/03/23/token-authentication-aspnetcore-complete-guide, this should work automatically
                ValidateAudience = true,
                ValidAudience = Configuration.GetSection("Authentication")["Audience"],
                ValidateIssuer = Configuration.GetSection("Authentication").GetValue<bool>("ValidateIssuer")
            };
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var csOmnikeeper = Configuration.GetConnectionString("OmnikeeperDatabaseConnection");
            services.AddHealthChecks().AddNpgSql(csOmnikeeper);

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.EnableForHttps = true;
            }); // reponse compression

            services.AddApiVersioning();

            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddCors(options => options.AddPolicy("DefaultCORSPolicy", builder =>
               builder.WithOrigins(Configuration.GetSection("CORS")["AllowedHosts"].Split(","))
               .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition"))
            );

            services.AddOData();//.EnableApiVersioning();

            services.AddHttpContextAccessor();

            services.AddSignalR();

            // HACK: member is set here and used later
            mvcBuilder = services.AddControllers();

            // add input and output formatters
            services.AddOptions<MvcOptions>()
                .PostConfigure<IOptions<JsonOptions>, IOptions<MvcNewtonsoftJsonOptions>, ArrayPool<char>, ObjectPoolProvider, ILoggerFactory>((config, jsonOpts, newtonJsonOpts, charPool, objectPoolProvider, loggerFactory) =>
                {

                    config.InputFormatters.Clear();
                    config.InputFormatters.Add(new MySuperJsonInputFormatter());
                    config.InputFormatters.Add(new NewtonsoftJsonInputFormatter(
                        loggerFactory.CreateLogger<NewtonsoftJsonInputFormatter>(), newtonJsonOpts.Value.SerializerSettings, charPool, objectPoolProvider, config, newtonJsonOpts.Value
                    ));
                    config.InputFormatters.Add(new SpanJsonInputFormatter<SpanJsonDefaultResolver<byte>>());

                    config.OutputFormatters.Clear();
                    config.OutputFormatters.Add(new MySuperJsonOutputFormatter());
                    config.OutputFormatters.Add(new NewtonsoftJsonOutputFormatter(newtonJsonOpts.Value.SerializerSettings, charPool, config, null));
                    config.OutputFormatters.Add(new SpanJsonOutputFormatter<SpanJsonDefaultResolver<byte>>());
                });


            global::GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(services, c =>
            {
                c.AddErrorInfoProvider(opt => {
                     opt.ExposeExceptionStackTrace = true;
                     opt.ExposeData = true;
                 })
                .AddGraphTypes()
                .AddSerializer<SpanJSONGraphQLSerializer>();
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = BuildTokenValidationParameters();

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
                        logger.LogInformation($"Validated token for user {HttpUserUtils.GetUsernameFromClaims(c.Principal!.Claims) ?? "Unknown User"}");
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

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Landscape omnikeeper REST API", Version = "v1" });
                c.TagActionsBy(api =>
                {
                    if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                    {
                        var attribute = controllerActionDescriptor.EndpointMetadata.OfType<ApiExplorerSettingsAttribute>().FirstOrDefault();
                        var tag = attribute?.GroupName ?? controllerActionDescriptor.ControllerName;
                        return new[] { tag };
                    }

                    throw new InvalidOperationException("Unable to determine tag for endpoint.");
                });
                c.DocInclusionPredicate((name, api) => true);

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
                            TokenUrl = new Uri(Configuration.GetSection("Authentication")["Authority"] + "/protocol/openid-connect/token", UriKind.Absolute),
                        },
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            Scopes = new Dictionary<string, string> { },
                            AuthorizationUrl = new Uri(Configuration.GetSection("Authentication")["Authority"] + "/protocol/openid-connect/auth", UriKind.Absolute),
                            TokenUrl = new Uri(Configuration.GetSection("Authentication")["Authority"] + "/protocol/openid-connect/token", UriKind.Absolute),
                            RefreshUrl = new Uri(Configuration.GetSection("Authentication")["Authority"] + "/protocol/openid-connect/token", UriKind.Absolute)
                        }
                    }
                });
                c.OperationFilter<AuthenticationRequirementsOperationFilter>();
                // Use action name as operationId
                /* NOTE: the swagger spec requires that the operationID is unique across the whole document. But Swashbuckle
                 * itself does not enforce it. So we tried to enforce this uniqueness by checking for already used operationIDs.
                 * If we encounter any, we throw an exception in the OperationFilter... but, the operationFilter runs more than once,
                 * so it's impossible to distinguish between a duplicate operationId and a completely new set of operationIds.
                 * And even if the check would work, this is not ideal, because it can lead to omnikeeper not being able to start when 
                 * f.e. two plugins define the endpoints with the same operationId/action name.
                 * But I believe it's better to be explicit here because if we allow duplicates in the operationIds,
                 * frontend-code generators (such as swagger-js) trip over the duplicate operationIDs and behave weirdly or break.
                 * 
                 * A cleaner solution would involve splitting each plugin into their own swagger document, but that's a large
                 * investment that we can't afford right now.
                 */
                //c.OperationFilter<DuplicateOperationIDOperationFilter>();
                c.CustomOperationIds(apiDesc =>
                {
                    string? oid = null;
                    if (apiDesc.ActionDescriptor is ControllerActionDescriptor ad)
                        oid = ad.ActionName;
                    else
                        throw new Exception("Invalid API description encountered");

                    return oid;
                });
            });
            services.AddSwaggerGenNewtonsoftSupport();

            // whether or not to show personally identifiable information in exceptions
            // see https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/wiki/PII
            IdentityModelEventSource.ShowPII = Configuration.GetValue<bool>("ShowPII");

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
                    replaceStr = $"{m.Scheme}://{m.Host.Host}:{m.Host.Port}";
                    newBaseURLPrefix = $"{m.Scheme}://{m.Host.Host}:{m.Host.Port}{Configuration["BaseURL"]}";
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

        public void ConfigureContainer(ContainerBuilder builder)
        {
            ServiceRegistration.RegisterLogging(builder);

            var csOmnikeeper = Configuration.GetConnectionString("OmnikeeperDatabaseConnection"); // TODO: add Enlist=false to connection string
            ServiceRegistration.RegisterDB(builder, csOmnikeeper, false);

            ServiceRegistration.RegisterModels(builder, enablePerRequestModelCaching: true, true, true, true);

            ServiceRegistration.RegisterGraphQL(builder);
            ServiceRegistration.RegisterOIABase(builder);
            ServiceRegistration.RegisterServices(builder);
            var csQuartz = Configuration.GetConnectionString("QuartzDatabaseConnection");
            ServiceRegistration.RegisterQuartz(builder, csQuartz);

            // plugins
            var pluginFolder = Path.Combine(Directory.GetCurrentDirectory(), "OKPlugins");
            var assemblies = ServiceRegistration.RegisterOKPlugins(builder, pluginFolder);
            // load controllers from plugins
            foreach (var assembly in assemblies)
            {
                mvcBuilder!.AddApplicationPart(assembly);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime,
            ILogger<Startup> logger, IEnumerable<IPluginRegistration> plugins, IServiceProvider serviceProvider)
        {
            var version = VersionService.GetVersion();
            logger.LogInformation($"Running version: {version}");

            // must be really early, because it affects later parts
            app.UsePathBase(Configuration.GetValue<string>("BaseURL"));

            app.UseHealthChecks("/health", HealthCheckSettings.Options);

            app.UseResponseCompression(); // response compression

            app.UseCors("DefaultCORSPolicy");


            // make application properly consider headers (and populate httprequest object) when behind reverse proxy
            var forwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            // NOTE, HACK: we should not do this because this has the potential of opening up IP spoofing attacks
            // but, it's otherwise really hard to correctly know the IP-ranges of possible proxies, so we disable whitelisting (for now)
            // see https://github.com/dotnet/AspNetCore.Docs/issues/2384 for a discussion
            forwardedHeaderOptions.KnownNetworks.Clear();
            forwardedHeaderOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardedHeaderOptions);

            //// debug to log all requests and their headers
            //app.Use(async (context, next) =>
            //{
            //    // Request method, scheme, and path
            //    logger.LogInformation("Request Method: {Method}", context.Request.Method);
            //    logger.LogInformation("Request Scheme: {Scheme}", context.Request.Scheme);
            //    logger.LogInformation("Request Path: {Path}", context.Request.Path);

            //    // Headers
            //    foreach (var header in context.Request.Headers)
            //    {
            //        logger.LogInformation("Header: {Key}: {Value}", header.Key, header.Value);
            //    }

            //    // Connection: RemoteIp
            //    logger.LogInformation("Request RemoteIp: {RemoteIpAddress}",
            //        context.Connection.RemoteIpAddress);

            //    logger.LogInformation("Known proxies: {KnownProxies}", (object)(new ForwardedHeadersOptions().KnownProxies));
            //    logger.LogInformation("Known proxies: {KnownNetworks}", (object)(new ForwardedHeadersOptions().KnownNetworks));

            //    await next();
            //});

            app.UseStaticFiles();

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();

                IdentityModelEventSource.ShowPII = true; // to show more debugging information
            }

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseGraphQLPlayground(new PlaygroundOptions() { GraphQLEndPoint = "/graphql-debug" });
            }

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
                var cv = endpoints.MapControllers();
                // add AllowAnonymous Attribute when debugAllowAll is true
                if (Configuration.GetSection("Authentication").GetValue("debugAllowAll", false))
                    cv.WithMetadata(new AllowAnonymousAttribute());

                // odata
                // DEFUNCT
                //var builder = new ODataConventionModelBuilder(app.ApplicationServices);
                //builder.EntitySet<Omnikeeper.Controllers.OData.AttributeDTO>("Attributes");
                //builder.EntitySet<Omnikeeper.Controllers.OData.RelationDTO>("Relations");
                //endpoints.EnableDependencyInjection();
                //endpoints.Select().Expand().Filter().OrderBy().Count();
                //var edmModel = builder.GetEdmModel();
                //endpoints.MapODataRoute("odata", "api/odata/{context}", edmModel);

                endpoints.MapHub<SignalRHubLogging>("/api/signalr/logging");
            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{Configuration["BaseURL"]}" } };
                });
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{Configuration["BaseURL"]}/swagger/v1/swagger.json", "Landscape omnikeeper REST API V1");
                c.OAuthClientId("landscape-omnikeeper"); // TODO: make configurable?
            });

            // plugins setup
            foreach (var plugin in plugins)
            {
                if (plugin.DBMigration != null)
                {
                    var cs = Configuration.GetConnectionString("OmnikeeperDatabaseConnection");
                    var result = plugin.DBMigration.Migrate(cs);
                    if (!result.Successful)
                    {
                        logger.LogError(result.Error, $"Error performing plugin DB migration");
                    }
                    else
                    {
                        logger.LogInformation("Performed plugin DB migration");
                    }
                }
            }
        }
    }

    internal class DuplicateOperationIDOperationFilter : IOperationFilter
    {
        private readonly ISet<string> usedOperationIds = new HashSet<string>();
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            string? oid;
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor ad)
                oid = ad.ActionName;
            else
                throw new Exception("Invalid API description encountered");

            if (usedOperationIds.Contains(oid)) // operation ID is already in use, swagger actually does not support this, so we bail
                throw new Exception($"Duplicate operation ID \"{oid}\" encountered");

            usedOperationIds.Add(oid);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using Landscape.Base;
using Landscape.Base.Model;
using LandscapePrototype.Entity.Converters;
using LandscapePrototype.Entity.GraphQL;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new AttributeValueConverter());
            });

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
            services.AddScoped<LandscapeSchema>();
            
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
        }

        private IWebHostEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, IEnumerable<IComputeLayerBrain> computeLayers, IPluginRegistry pluginRegistry)
        {
            pluginRegistry.RegisterComputeLayerBrains(computeLayers);


            app.UseCors("AllowAllOrigins");

            app.UseGraphQL<LandscapeSchema>();

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseGraphQLPlayground(new GraphQLPlaygroundOptions()); //to explorer API navigate https://*DOMAIN*/ui/playground
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //computeLayers.First().Run().GetAwaiter().GetResult();
        }
    }
}

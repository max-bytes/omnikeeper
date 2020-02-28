using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
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
using Npgsql;

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

            services.AddScoped<CIModel>();
            services.AddScoped<LayerModel>();
            services.AddScoped<RelationModel>();
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
        }
    }
}

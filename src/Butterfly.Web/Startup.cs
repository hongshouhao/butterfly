using AutoMapper;
using Butterfly.Elasticsearch;
using Butterfly.EntityFrameworkCore;
using Butterfly.Consumer.Lite;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Butterfly.Common;
using Butterfly.HttpCollector;

namespace Butterfly.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mvcBuilder = services.AddMvc(option =>
            {
                option.OutputFormatters.Add(new MessagePackOutputFormatter(ContractlessStandardResolver.Instance));
                option.InputFormatters.Add(new MessagePackInputFormatter(ContractlessStandardResolver.Instance));
            });

            mvcBuilder.AddApplicationPart(typeof(HttpCollectorOptions).Assembly);

            services.AddResponseCompression();
            services.Configure<HttpCollectorOptions>(Configuration);
            services.AddCors();
            services.AddAutoMapper(option => option.AddProfile<MappingProfile>());
            services.AddSwaggerGen(option => { option.SwaggerDoc("v1", new Info { Title = "butterfly http api", Version = "v1" }); });
            services.AddLiteConsumer(Configuration)
                .AddEntityFrameworkCore(Configuration);

            services.AddElasticsearch(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "butterfly http api v1");
            });

            app.UseResponseCompression();
            app.UseCors(cors => cors.AllowAnyOrigin());
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
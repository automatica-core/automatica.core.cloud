using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.LicenseManager;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Automatica.Core.Cloud
{
    public class Startup
    {
        private const string AutomaticaCorePrivat = "this-is-a-very-secret-code";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<CoreContext>();


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
              .AddJwtBearer(config =>
              {
                  config.RequireHttpsMetadata = false;
                  config.SaveToken = true;
                  config.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuerSigningKey = true,
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AutomaticaCorePrivat)),
                      ValidateIssuer = false,
                      ValidateAudience = false
                  };
              });


            services.AddMvcCore(config =>
            {
                config.Filters.Add(new AuthorizeFilter());

            }).AddAuthorization(options =>
            {

            }).AddApplicationPart(typeof(BaseController).GetTypeInfo().Assembly).AddControllersAsServices().AddJsonOptions(options => {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            }).AddJsonFormatters().AddXmlSerializerFormatters();

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
                x.ValueCountLimit = int.MaxValue;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Automatica.Core.Cloud", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new Swashbuckle.AspNetCore.Swagger.ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\", provide value: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                var security = new Dictionary<string, IEnumerable<string>>
                  {
                      {"Bearer", new string[] { }},
                  };
                c.AddSecurityRequirement(security);
            });

            services.AddMvc();
            services.AddApiVersioning(o =>
                {
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                });

            services.AddSingleton(new JwtInfoProvider(AutomaticaCorePrivat));
            services.AddScoped<ILicenseManager, LicenseManager.LicenseManager>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            var builder = new ConfigurationBuilder()
              .SetBasePath(env.ContentRootPath)
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
              .AddEnvironmentVariables();


            Configuration = builder.Build();
            

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }

            if (env.IsDevelopment())
            {
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
                // specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Automatica.Core.Cloud");
                    c.IndexStream = () => Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Cloud.Swagger.swagger.index.html");
                });
            


            
                }


            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404 &&
                    !Path.HasExtension(context.Request.Path.Value) &&
                    !context.Request.Path.Value.StartsWith("/api/"))
                {
                    context.Request.Path = "/index.html";
                    await next();
                }
            });

            app.Map("/webapi", appBuilder =>
            {
                appBuilder.UseAuthentication();

               // appBuilder.UseMiddleware<WebApiErrorMiddleware>();
                appBuilder.UseMvc();
            });



            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(wwwrootPath)
            });

        }
    }
}

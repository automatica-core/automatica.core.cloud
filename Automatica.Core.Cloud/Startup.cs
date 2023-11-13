using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.LicenseManager;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
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
using Automatica.Core.Cloud.RemoteControl;
using Automatica.Core.Cloud.TTS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Automatica.Core.Model.Models.User;
using Microsoft.OpenApi.Models;

namespace Automatica.Core.Cloud
{
    public class Startup
    {
        private const string AutomaticaCorePrivat = "this-is-a-very-secret-code-with-a-lot-of-chars";

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
            services.Configure<MvcOptions>(options =>
            {
            });

            services.AddMvcCore(config =>
                {
                    config.Filters.Add(new AuthorizeFilter());

                }).AddAuthorization(options =>
                {
                    options.AddPolicy(Role.AdminRole, policy => policy.RequireRole(Role.AdminRole));
                    options.AddPolicy(Role.ViewerRole,
                        policy => policy.RequireRole(Role.ViewerRole, Role.AdminRole, Role.VisuRole));
                    options.AddPolicy(Role.VisuRole, policy => policy.RequireRole(Role.VisuRole));

                })
                .AddApplicationPart(typeof(BaseController).GetTypeInfo().Assembly)
                .AddControllersAsServices()
                .AddApplicationPart(typeof(UserController).GetTypeInfo().Assembly)
                .AddControllersAsServices()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });


            services.AddControllers();

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
                x.ValueCountLimit = int.MaxValue;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Automatica.Core.Cloud", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\", provide value: \"Bearer {token}\"",
                    Name = "Authorization",
                    
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

            });
            services.AddApiVersioning(o =>
                {
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                });

            services.AddSingleton(new JwtInfoProvider(AutomaticaCorePrivat));
            services.AddScoped<ILicenseManager, LicenseManager.LicenseManager>();

            services.AddDnsZoneProvider(a =>
            {
                a.DnsZoneName = "automaticaremote.com";
                a.ResourceGroup = "automatica";
                a.CNameTarget = "dev.automaticaremote.com";
                a.CNamePrefix = "dev";
            });

            services.AddTextToSpeechProvider();

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

          
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Automatica.Core.Cloud");
                c.IndexStream = () =>
                    Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("Automatica.Core.Cloud.Swagger.swagger.index.html");
            });

        
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("webapi", "");
            });

            //app.Map("/webapi", appBuilder =>
            //{
            //    appBuilder.UseAuthentication();
                
            //   // appBuilder.UseMiddleware<WebApiErrorMiddleware>();
            //    appBuilder.UseMvc();
            //});



            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(wwwrootPath)
            });

        }
    }
}

using System;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Helper;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.LicenseManager;
using Automatica.Core.Cloud.RemoteControl;
using Automatica.Core.Cloud.TTS;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Controllers;
using Automatica.Core.Cloud.WebApi.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Secrets: Key Vault (managed identity) in Azure, user-secrets locally. Nothing secret lives in appsettings.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var services = builder.Services;

services.AddDbContext<CoreContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AutomaticaCoreCloudSql");
    if (builder.Environment.IsEnvironment("Testing") && string.IsNullOrEmpty(connectionString))
    {
        return; // the test host registers its own provider
    }
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});

services.AddControllers()
    .AddApplicationPart(typeof(BaseController).Assembly)
    .AddNewtonsoftJson(options =>
    {
        // payload format the cores expect
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });

services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new ApiVersion(1, 0);
}).AddMvc();

services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 1L << 30);

services.AddCloudSecurity(builder.Configuration, builder.Environment);
services.AddDnsZoneProvider(builder.Configuration.GetSection("RemoteControl"));
services.AddTextToSpeechProvider();
services.AddSingleton<IMailSender, AcsMailSender>();
services.AddSingleton<IPackageStore, AzurePackageStore>();
services.AddScoped<PackageService>();
services.AddScoped<ILicenseManager, LicenseManager>();
services.AddHealthChecks();
services.AddProblemDetails();

if (builder.Environment.IsDevelopment())
{
    services.AddOpenApi();
}

services.Configure<ForwardedHeadersOptions>(o =>
{
    // App Service terminates TLS in front of the app; trust exactly one proxy hop
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

services.AddHsts(o =>
{
    o.MaxAge = TimeSpan.FromDays(365);
    o.IncludeSubDomains = false;
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Unhandled");
    // ArgumentException is the old "business error" path the cores know; keep it a 400 without details leaking
    var isBusinessError = feature?.Error is ArgumentException;
    if (!isBusinessError)
    {
        logger.LogError(feature?.Error, "Unhandled exception for {Path}", context.Request.Path);
    }
    context.Response.StatusCode = isBusinessError ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    var message = isBusinessError ? feature.Error.Message : "internal error";
    await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = message }));
}));

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Frame-Options"] = "DENY";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz").AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// SPA deep links; never for api paths, so unknown api routes stay 404
app.MapFallback("{*path:nonfile}", async context =>
{
    if (context.Request.Path.StartsWithSegments("/webapi"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    var file = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
    if (!file.Exists)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    await context.Response.SendFileAsync(file);
}).AllowAnonymous();

await DatabaseInitializer.InitializeAsync(app.Services);

await app.RunAsync();

public partial class Program;

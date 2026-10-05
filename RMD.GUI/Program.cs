using ApexCharts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RMD.Business.Services;
using RMD.Data.Context;
using RMD.GUI.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddApexCharts();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDataTransferService, DataTransferService>();
builder.Services.AddSingleton<BackupStore>();

//DbContext (connection string from appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("RmdDatabase");
// Factory: services open a short-lived context per operation (a scoped context would live for the whole Blazor circuit)
builder.Services.AddDbContextFactory<RMDContext>(options =>
	options.UseSqlServer(connectionString));

//Swagger Config
builder.Services.AddControllers()
	// Song <-> SongArtist <-> Artist navigations point both ways
	.AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "RMD API",
		Version = "v1",
		Description = "API for managing artists and songs"
	});
});

//Auth and cookie config
builder.Services.AddDbContext<AuthDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("RmdDatabase")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
	{
		options.Lockout.MaxFailedAccessAttempts = 5;
		options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
		options.Lockout.AllowedForNewUsers = true;

		options.Password.RequiredLength = 8;
		options.User.RequireUniqueEmail = true;
	})
	.AddEntityFrameworkStores<AuthDbContext>()
	.AddDefaultTokenProviders();
builder.Services.AddAuthRateLimiting();
builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/login";
	options.AccessDeniedPath = "/login";
	options.LogoutPath = "/auth/logout";

	options.Cookie.Name = ".AspNetCore.Identity.Application";
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
	options.Cookie.SameSite = SameSiteMode.Lax;
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

	options.ExpireTimeSpan = TimeSpan.FromDays(7);
	options.SlidingExpiration = true;

	// API callers get status codes instead of a redirect to the login page
	options.Events.OnRedirectToLogin = context =>
	{
		if (context.Request.Path.StartsWithSegments("/api"))
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
		else
			context.Response.Redirect(context.RedirectUri);
		return Task.CompletedTask;
	};
	options.Events.OnRedirectToAccessDenied = context =>
	{
		if (context.Request.Path.StartsWithSegments("/api"))
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
		else
			context.Response.Redirect(context.RedirectUri);
		return Task.CompletedTask;
	};
});

// Cookies are re-checked against the security stamp every 5 minutes (default 30)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
	options.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Hosting: Data Protection keys and health checks (see docs/azure-deployment.md)
builder.Services.AddRmdDataProtection(builder.Configuration);
builder.Services.AddRmdHealthChecks();

var app = builder.Build();

app.WarnAboutLocalConnectionString();
await app.MigrateDatabasesIfConfiguredAsync();

// Seed RMD's sole user to the database
await IdentitySeeder.SeedUserAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI( c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "RMD API v1");
		c.RoutePrefix = "swagger";
	});
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseRmdSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    // Large, rarely changing assets: let the browser keep them for a week (saves bandwidth on the free tier)
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path;
        if (path.StartsWithSegments("/video") || path.StartsWithSegments("/images") ||
            path.StartsWithSegments("/bootstrap-icons/font/fonts"))
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=604800";
        }
    }
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UsePrerenderRedirects();
app.UseRateLimiter();

app.MapHealthChecks("/healthz");
app.MapBlazorHub();
app.MapControllers();
// Unknown /api routes return 404 instead of falling through to the Blazor host page
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToPage("/_Host");

app.Run();

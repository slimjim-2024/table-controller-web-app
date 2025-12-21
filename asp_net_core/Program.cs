using asp_net_core;
using asp_net_core.Data;
using asp_net_core.Models;
using asp_net_core.Seeders;
using asp_net_core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Optional: configure Kestrel to use a PFX if configured.
var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
var certPassword = builder.Configuration["Kestrel:Certificates:Default:Password"];
if (!string.IsNullOrEmpty(certPath))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = new X509Certificate2(certPath, certPassword);
        });
    });
}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=my_sql;Port=3306;Database=AspNetDatabase;Uid=root;Pwd=markloh;";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRazorComponents();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{ 
    options.SignIn.RequireConfirmedAccount = false; 
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<MQTT_Service>();
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<PersistenceService>();


var app = builder.Build();

// Initialize MQTT connection
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var mqttService = app.Services.GetRequiredService<MQTT_Service>();
    var mosquittoConfig = builder.Configuration.GetSection("MosquittoConfig");
    
    var brokerAddress = mosquittoConfig["BrokerAddress"] ?? "mosquitto";
    var brokerPort = int.TryParse(mosquittoConfig["BrokerPort"], out int port) ? port : 1883;
    var username = mosquittoConfig["Username"];
    var password = mosquittoConfig["Password"];

    logger.LogInformation("Attempting to connect to MQTT broker at {Broker}:{Port}", brokerAddress, brokerPort);
    
    await mqttService.ConnectAsync(brokerAddress, brokerPort, username, password);
    
    logger.LogInformation("MQTT service initialized successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize MQTT service. The application will continue but MQTT functionality will not be available.");
    // Don't throw - allow the app to start even if MQTT fails
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var roleLogger = services.GetRequiredService<ILogger<Program>>();
    var configService = services.GetRequiredService<ConfigService>();
    var persistanceService = services.GetRequiredService<PersistenceService>();
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        logger.LogInformation("Application is shutting down...");

        // Save state before shutdown

        // This runs synchronously during shutdown
        persistanceService.SaveStateAsync(configService).GetAwaiter().GetResult();

        logger.LogInformation("State saved successfully");
    });
    try
    {
        var tempAdmin = await persistanceService.LoadStateAsync<ConfigService>();
        configService.CanGetAdmin = tempAdmin is null || tempAdmin.CanGetAdmin;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        dbContext.Database.Migrate();
        await ApplicationRoles.InitializeRoles(services);
    }
    catch (Exception ex)
    {
        roleLogger.LogError(ex, "An error occurred while seeding roles.");
        throw;
    }
}

// Register shutdown handler

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");
app.MapRazorPages();
app.MapBlazorHub();
app.MapRazorComponents<App>();

app.Run();
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PANDOLLAR.Data;
using PANDOLLAR.Hubs;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Register PandollarDbContext for your application data
builder.Services.AddDbContext<PandollarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PandollarConnection")));

// Register ApplicationDbContext for Identity data
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PandollarConnection")));

// Configure and Register IdentityFramework
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configure password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4; // Changed to align with Medisat settings
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Configure user settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Lockout settings
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = true; // Added to align with Medisat
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Configure token providers
    options.Tokens.EmailConfirmationTokenProvider = "Default";
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure the token lifespan for all token providers
builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
    opt.TokenLifespan = TimeSpan.FromMinutes(5));  // Ensures 5-minute expiry for tokens

// Add Session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register EmailSender and SmsSender services
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Register RoleRedirectService
builder.Services.AddTransient<RoleRedirectService>();

// Register NotificationService
builder.Services.AddTransient<NotificationService>();

// Register HttpClient as a service
builder.Services.AddHttpClient();

// Register ErrorCodeService
builder.Services.AddSingleton<IErrorCodeService, ErrorCodeService>();

// Configure SignalR
builder.Services.AddSignalR();

// Add services to the container
builder.Services
    .AddRazorPages()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

// Enable areas support
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Map the API controller route for the LogoutAPI
app.MapControllerRoute(
    name: "LogoutAPI",
    pattern: "api/{controller}/{action}");

// Map routes for the "NutritionCompany" area
app.MapAreaControllerRoute(
    name: "Company",
    areaName: "Company",
    pattern: "Company/{controller=Home}/{action=Index}/{userId}/{companyId:guid?}");

// Specifically routes to the "NutritionSystem" controller within the "NutritionCompany" area
app.MapAreaControllerRoute(
    name: "companySystemRoute",
    areaName: "Company",
    pattern: "Company/{controller=CompanySystem}/{action=Index}/{userId}/{companyId:guid}");

// Map the default controller route (for non-area routes)
app.MapDefaultControllerRoute();

// Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

// Map Razor Pages
app.MapRazorPages();

app.Run();

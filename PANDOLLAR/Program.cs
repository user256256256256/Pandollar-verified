using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// Register MedisatErpDbContext for your application data
builder.Services.AddDbContext<PandollarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PandollarConnection")));


// Add DbContext to the service container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);


// Configure Identity Framework
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configure password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Configure user settings
    options.User.RequireUniqueEmail = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Lockout settings
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Enabling areas route suppor
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Map the default controller route (for non-area routes)
app.MapDefaultControllerRoute();

app.Run();

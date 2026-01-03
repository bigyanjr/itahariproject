using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UsersApp.Data;
using UsersApp.Models;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Add services to the container
// --------------------
builder.Services.AddControllersWithViews();

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure DbContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)); // Microsoft.EntityFrameworkCore.Sqlite 9.0.0 or later

// Configure Identity
builder.Services.AddIdentity<Users, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// --------------------
// Build the app
// --------------------
var app = builder.Build();

// --------------------
// Configure the HTTP request pipeline
// --------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the app
app.Run();

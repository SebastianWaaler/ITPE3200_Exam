using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Data.Repositories.Interfaces;
using QuizApp.Data.Repositories.Implementations;
using QuizApp.Models; // ApplicationUser

var builder = WebApplication.CreateBuilder(args);

// LOGGING

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// DATABASE
builder.Services.AddDbContext<QuizContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("QuizContext")));
// IDENTITY (with roles + ApplicationUser)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; 
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<QuizContext>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// REPOSITORIES
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IOptionRepository, OptionRepository>();

var app = builder.Build();

// SEED ROLES + DEFAULT ADMIN USER
// SEED ROLES + DEFAULT ADMIN USER
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roleNames = { "Admin", "Player" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Create a default admin user
    string adminEmail = "admin@example.com";
    string adminPassword = "Admin123!"; // change before handing in 😉

    // 👉 use email as username too
    var adminUser = await userManager.FindByNameAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,   // 👈 matches what login will use
            Email = adminEmail
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            // Optional: log errors to console so you can see if password rules failed, etc.
            foreach (var error in createResult.Errors)
            {
                Console.WriteLine($"Admin user creation error: {error.Code} - {error.Description}");
            }
        }
    }
}


// PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Quiz}/{action=Index}/{id?}");

// Identity Razor Pages (/Identity/Account/Login, etc.)
app.MapRazorPages();

app.Run();

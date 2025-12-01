using Microsoft.EntityFrameworkCore;
using QuizApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext with dependency injection
builder.Services.AddDbContext<QuizContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("QuizContext"))); 

// Register MVC controllers + Razor views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Adds security header for production
}

app.UseHttpsRedirection(); 
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Default route pattern: /{controller=Quiz}/{action=Index}/{id?}
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Quiz}/{action=Index}/{id?}");

app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using RemcSys.Models;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<RemcDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RemcDBContext") ?? throw new InvalidOperationException("Connection string 'RemcDBContext' not found.")));
var connectionString = builder.Configuration.GetConnectionString("RemcSysDBContextConnection") ?? throw new InvalidOperationException("Connection string 'RemcSysDBContextConnection' not found.");

builder.Services.AddDbContext<RemcSysDBContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<SystemUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<RemcSysDBContext>();

builder.Services.AddScoped<ActionLoggerService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Faculty", "Evaluator", "Chief" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<RemcDBContext>();

    var existingSettings = dbContext.Settings.FirstOrDefault(s => s.Id == "MainOption");
    if(existingSettings == null)
    {
        var settings = new Settings
        {
            Id = "MainOption",
            isMaintenance = false,
            isUFRApplication = false,
            isEFRApplication = false,
            isUFRLApplication = false,
            evaluatorNum = 5,
            daysEvaluation = 7
        };

        dbContext.Settings.Add(settings);
        dbContext.SaveChanges();
    }
}

app.Run();

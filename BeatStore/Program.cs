using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeatStore.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔥 БД
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 Identity + Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// 🔥 SESSION (ДЛЯ КОРЗИНЫ)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔥 MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 🔥 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

// 🔥 SESSION ДОЛЖЕН БЫТЬ ДО AUTH
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 🔥 ROUTES
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Beats}/{action=Index}/{id?}");

app.MapRazorPages();


// 🔥 СОЗДАНИЕ ADMIN (ОДИН РАЗ)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    // создаем роль Admin
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // 👇 ТВОЙ EMAIL
    var email = "test@test.com";

    var user = await userManager.FindByEmailAsync(email);

    if (user != null && !await userManager.IsInRoleAsync(user, "Admin"))
    {
        await userManager.AddToRoleAsync(user, "Admin");
    }
}

app.Run();
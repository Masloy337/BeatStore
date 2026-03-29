using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeatStore.Data;
using Resend;
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
// Подключаем сам Resend, забирая токен из appsettings.json
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.AddTransient<IResend, ResendClient>();
// Регистрируем наш сервис писем
builder.Services.AddTransient<BeatStore.Services.IEmailService, BeatStore.Services.EmailService>();
var app = builder.Build();

// 🔥 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
if (!app.Environment.IsDevelopment())
{
    // Если произошла фатальная ошибка в коде (500)
    app.UseExceptionHandler("/Errors/500");
    app.UseHsts();
}

// 🔥 Перехватываем 404 и другие ошибки статус-кодов
app.UseStatusCodePagesWithReExecute("/Errors/{0}");
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
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();

    // 1. Убеждаемся, что роль "Admin" физически существует в базе
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
    }

    // 2. ВПИШИ СЮДА ВАШИ РЕАЛЬНЫЕ ПОЧТЫ (под которыми вы зарегистрируетесь на сайте)
    var immortalAdmins = new[] {
        "kipishgesh@gmail.com",
        "luzylego@gmail.com"
    };

    foreach (var email in immortalAdmins)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            // Если вы есть в базе, но еще не админы — система выдает вам права
            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }
}

app.Run(); 
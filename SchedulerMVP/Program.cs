using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using SchedulerMVP.Data.Seed;
using SchedulerMVP.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Explicitly tell Kestrel to listen on Fly.io port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add ApplicationDbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite("Data Source=app.db");
}, ServiceLifetime.Transient);

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Persist login for 7 days
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
});

// Add AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite("Data Source=app.db");
}, ServiceLifetime.Transient);

builder.Services.AddHttpContextAccessor();

// Add services
builder.Services.AddScoped<IConflictService, ConflictService>();
builder.Services.AddScoped<IScheduleTemplateService, ScheduleTemplateService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<ICalendarBookingService, CalendarBookingService>();
builder.Services.AddScoped<BookingDialogService>();
builder.Services.AddScoped<UIState>();
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<IGroupTypeService, GroupTypeService>();
builder.Services.AddScoped<RoleManager<IdentityRole>>();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();

    // Fly.io hanterar redan HTTPS – undvik redirect-loop
    app.Use((ctx, next) =>
    {
        if (ctx.Request.Headers.TryGetValue("Fly-Forwarded-Proto", out var proto) && proto == "https")
            ctx.Request.Scheme = "https";
        return next();
    });
}

app.UseStaticFiles();
app.UseRouting();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// --- Auth endpoints ---
app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?error=missing");

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
    if (result.Succeeded)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);
        }
        return Results.Redirect("/");
    }

    return Results.Redirect("/login?error=invalid");
});

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});
app.MapGet("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

// --- DB migration & seed ---
using (var scope = app.Services.CreateScope())
{
    var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await identityContext.Database.MigrateAsync();

    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var provider = context.Database.ProviderName ?? string.Empty;
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();
    }
    catch { }

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

app.Run();

using ElamIgugu.Web.Data;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ElamIgugu.Web.Models;
using ElamIgugu.Web.Options;
using ElamIgugu.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (Azure SQL in production, LocalDB while developing) ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---- Identity: accounts, password hashing, roles, lockout ----
// AddIdentity (not AddDefaultIdentity) is used so we control the pages ourselves.
// AddDefaultTokenProviders also registers the authenticator-app (TOTP) provider,
// which is what two-factor authentication will use later.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Lock the account for 5 minutes after 5 wrong passwords
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

// The NPO's contact details shown in the Donate pop-up live in appsettings.json
builder.Services.Configure<NpoOptions>(builder.Configuration.GetSection("Npo"));

// ---- JWT: lets the Android app sign in through /api/... (the website keeps using cookies) ----
// Jwt:Key must be 32+ characters. Set it with user-secrets locally and in App Service settings on Azure.
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<long random text>\"");
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

// Slow down password guessing on the API sign-in/sign-up endpoints
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

// Every POST form must carry an anti-forgery token (protects against CSRF)
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

// ---- Create/upgrade the database, then add roles, the admin account and sample webinars ----
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();   // who are you?
app.UseAuthorization();    // are you allowed in?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

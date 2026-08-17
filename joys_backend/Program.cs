using Hlouwa.Models;
using Hlouwa.Controllers;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// DbContext
// --------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// --------------------
// Identity
// --------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequiredLength = 6;

    opt.User.RequireUniqueEmail = true;

    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    opt.Lockout.AllowedForNewUsers = true;
})
.AddErrorDescriber<Hlouwa.FrenchIdentityErrorDescriber>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ✅ IMPORTANT: stop redirects to /Account/Login for /api/* (return 401/403 instead of 302)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

// --------------------
// JWT
// --------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;

// ✅ IMPORTANT: force JWT as default scheme (auth + challenge)
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// --------------------
// Payment Providers (Dummy now)
// --------------------
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPaymentProviderClient, DummyPaymentProviderClient>();

// --------------------
// CORS (Angular)
// --------------------
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAngular", p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
            p.WithOrigins(origins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        }
    });
});

// --------------------
// Controllers + Swagger
// --------------------

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hlouwa API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Put: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --------------------
// Reverse proxy headers
// --------------------
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// --------------------
// Global exception handler -> ProblemDetails (stable)
// --------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");

        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;
        if (ex != null) logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = 500,
            Title = "Erreur serveur",
            Detail = env.IsDevelopment() ? ex?.ToString() : "Erreur interne. Consultez les logs serveur.",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    });
});

// --------------------
// Swagger (DEV conseillé)
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --------------------
// Pipeline
// --------------------
app.UseHttpsRedirection();

// Angular build dans wwwroot
app.UseDefaultFiles(); // doit être AVANT UseStaticFiles
app.UseStaticFiles();

app.UseRouting();

// CORS + Security
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

// ✅ API controllers
app.MapControllers();

// ✅ SPA fallback -> ton controller SpaFallbackController.Index
app.MapFallbackToController("{*path:nonfile}", "Index", "SpaFallback");

// --------------------
// Seed roles + admin + client
// --------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

    // (Optionnel) appliquer migrations automatiquement
    // ⚠️ Si tu préfères gérer via CLI, commente cette partie.
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migrate failed");
        // On ne crash pas l'app: stabilité. Mais vérifie les logs.
    }

    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Roles
    foreach (var roleName in new[] { "Admin", "Client" })
    {
        if (!await roleMgr.RoleExistsAsync(roleName))
        {
            var rr = await roleMgr.CreateAsync(new IdentityRole(roleName));
            if (!rr.Succeeded)
                logger.LogError("Create role {Role} failed: {Errors}", roleName, string.Join(", ", rr.Errors.Select(e => e.Description)));
        }
    }

    // ---- Seed Admin
    var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@hlouwa.tn";
    var adminPass = builder.Configuration["Seed:AdminPassword"] ?? "Admin123!";

    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Admin",
            EmailConfirmed = true
        };

        var create = await userMgr.CreateAsync(admin, adminPass);
        if (!create.Succeeded)
        {
            logger.LogError("Create admin failed: {Errors}", string.Join(", ", create.Errors.Select(e => e.Description)));
        }
    }

    if (admin != null && !await userMgr.IsInRoleAsync(admin, "Admin"))
    {
        var addRole = await userMgr.AddToRoleAsync(admin, "Admin");
        if (!addRole.Succeeded)
            logger.LogError("Add Admin role failed: {Errors}", string.Join(", ", addRole.Errors.Select(e => e.Description)));
    }

    // ---- Seed Client
    var clientEmail = builder.Configuration["Seed:ClientEmail"] ?? "client@hlouwa.tn";
    var clientPass = builder.Configuration["Seed:ClientPassword"] ?? "Client123!";

    var clientFullName = builder.Configuration["Seed:ClientFullName"] ?? "Client Demo";
    var clientPhone = builder.Configuration["Seed:ClientPhone"] ?? "20000000";

    var client = await userMgr.FindByEmailAsync(clientEmail);
    if (client == null)
    {
        client = new ApplicationUser
        {
            UserName = clientEmail,
            Email = clientEmail,
            FullName = clientFullName,
            PhoneNumber = clientPhone,
            EmailConfirmed = true
        };

        var create = await userMgr.CreateAsync(client, clientPass);
        if (!create.Succeeded)
        {
            logger.LogError("Create client failed: {Errors}", string.Join(", ", create.Errors.Select(e => e.Description)));
        }
    }

    if (client != null && !await userMgr.IsInRoleAsync(client, "Client"))
    {
        var addRole = await userMgr.AddToRoleAsync(client, "Client");
        if (!addRole.Succeeded)
            logger.LogError("Add Client role failed: {Errors}", string.Join(", ", addRole.Errors.Select(e => e.Description)));
    }
}

app.Run();

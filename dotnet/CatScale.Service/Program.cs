using System.Text.Json.Serialization;
using CatScale.Service.Authentication;
using CatScale.Service.DbModel;
using CatScale.Service.Repositories;
using CatScale.Service.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSystemd();

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);
});

builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opt.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<CatScaleContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("CatScalePG"));
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    
    options.User.RequireUniqueEmail = false;

    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<CatScaleContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

 builder.Services
     .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
     //.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters()
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidAudience = builder.Configuration["Jwt:Audience"],
//             ValidIssuer = builder.Configuration["Jwt:Issuer"],
//             IssuerSigningKey = new SymmetricSecurityKey(
//                 Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
//             )
//         };
//     })
     .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IClassificationService, ClassificationService>();
builder.Services.AddScoped<IInfluxService, InfluxService>();
builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>(); // One Instance per Http-Request


var app = builder.Build();

var configEnableMigration = app.Configuration.GetValue<bool>("Database:EnableMigration");
var configPopulateUsers = app.Configuration.GetValue<bool>("Database:PopulateUsers");
var configPopulateData = app.Configuration.GetValue<bool>("Database:PopulateData");

Console.WriteLine($"Database config {configEnableMigration} {configPopulateUsers} {configPopulateData}");

if (configEnableMigration)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<CatScaleContext>();
        //context.Database.EnsureDeleted();
        //context.Database.EnsureCreated();
        context.Database.Migrate();
    }
}

if (configPopulateData)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<CatScaleContext>();
        CatScaleDbInitializer.Initialize(context);
    }
}

if (configPopulateUsers)
{
    Task.Run(async () =>
    {
        Console.WriteLine($"Start of user init task");
        
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            await UserDbInitializer.Initialize(userManager, roleManager);
        }
        
        Console.WriteLine($"End of user init task");
    });
}

app.UseSerilogRequestLogging(); // Note: position controls which requests are logged. Move down to reduce.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

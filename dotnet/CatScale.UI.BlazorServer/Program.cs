using System.Globalization;
using CatScale.UI.BlazorServer.Services;
using CatScale.UI.BlazorServer.Services.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// builder.Services.AddOptions();
// builder.Services.AddAuthorizationCore();  // xxx
// builder.Services.AddAuthentication();     // xxx

builder.Services.AddScoped<IdentityAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<IdentityAuthenticationStateProvider>());

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICatScaleService, CatScaleService>();

var serviceAddr = builder.Configuration.GetValue<string>("CatScaleServiceAddr");
if (String.IsNullOrWhiteSpace(serviceAddr)) throw new ArgumentException("invalid configuration, missing CatScaleServiceAddr");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(serviceAddr) });

var supportedCultures = new List<CultureInfo>()
{
    new CultureInfo("de-DE"),
    //new CultureInfo("en"),
};

builder.Services.Configure<RequestLocalizationOptions>(opt =>
{
    opt.DefaultRequestCulture = new RequestCulture("de-DE");
    opt.SupportedCultures = supportedCultures;
    opt.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();
//app.UseRequestLocalization("de-DE");

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

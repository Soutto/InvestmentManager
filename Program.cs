using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using InvestmentManager.Components;
using InvestmentManager.Components.Account;
using InvestmentManager.Data;
using InvestmentManager.Data.Repositories.Interfaces;
using PortofolioManager.Infrastructure.Repositories;
using InvestmentManager.Services;
using InvestmentManager.Services.Interfaces;
using MudBlazor.Translations;
using MudBlazor;
using InvestmentManager.Utils;
using MudExtensions.Services;
using InvestmentManager.Shared.Models;
using DadosDeMercadoClient.Interfaces;
using DadosDeMercadoClient.Clients;
using StackExchange.Redis;
using ApexCharts;
using InvestmentManager.Shared.Validators;
using DadosDeMercadoClient.Interfaces.Brapi;
using DadosDeMercadoClient.Clients.Brapi;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddMudTranslations();
builder.Services.AddTransient<MudLocalizer, CustomMudBlazorLocalizer>();
builder.Services.AddMudExtensions();
builder.Services.AddApexCharts();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)/*.EnableSensitiveDataLogging()*/);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddHttpClient<IAssetClient, AssetClient>();
builder.Services.AddHttpClient<INewsClient, NewsClient>();
builder.Services.AddHttpClient<IBrapiAssetClient, BrapiAssetClient>();
builder.Services.AddControllers();

#region Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
#endregion

#region Services

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IAssetMonthlyPriceService, AssetMonthlyPriceService>();
builder.Services.AddScoped<INewsService, NewsService>();

#endregion

#region Validators
builder.Services.AddScoped<TransactionDtoValidator>();
#endregion

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetSection("Redis")["ConnectionString"];
    return ConnectionMultiplexer.Connect(configuration!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapControllers();

app.MapGet("/redis-test", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    await db.StringSetAsync("key", "value");
    var value = await db.StringGetAsync("key");
    return Results.Ok($"Redis key value: {value}");
});

app.Run();

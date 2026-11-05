using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Data;
using ShareMaster.Server.Hosting;
using ShareMaster.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- 設定 ----
var authSection = builder.Configuration.GetSection(AuthSettings.SectionName);
var authSettings = authSection.Get<AuthSettings>() ?? new AuthSettings();
builder.Services.Configure<AuthSettings>(authSection);
var dbSection = builder.Configuration.GetSection(DatabaseSettings.SectionName);
var dbSettings = dbSection.Get<DatabaseSettings>() ?? new DatabaseSettings();
builder.Services.Configure<DatabaseSettings>(dbSection);
builder.Services.Configure<CanonicalUrlSettings>(builder.Configuration.GetSection(CanonicalUrlSettings.SectionName));

// ---- データベース（現在：SQLite / 移行先：SQL Server・Azure SQL Database）----
var connectionString = builder.Configuration.GetConnectionString(DatabaseSettings.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException($"接続文字列 '{DatabaseSettings.ConnectionStringName}' が設定されていません。");
builder.Services.AddShareMasterDatabase(dbSettings, connectionString);

// ---- 認証（本番：Microsoft Entra ID / 開発：擬似ログイン）----
if (authSettings.IsDevelopmentMode)
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("本番環境では Auth:Mode=Development は使用できません。");
    builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, null);
}
else
{
    builder.Services.AddAuthentication().AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}
builder.Services.AddAuthorization();

// ---- アプリケーションサービス ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<HistoryWriter>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CalendarService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// データ移行コマンド（SQLite → SQL Server 等）。Web サーバーは起動しない
if (args.FirstOrDefault() == DataTransferCommand.CommandName)
    return await DataTransferCommand.RunAsync(app);

await app.InitializeDatabaseAsync();

app.UseExceptionHandler();

// HTTP → HTTPS、ホスト名のみ → ドメイン付与（CanonicalUrl 設定）
app.UseMiddleware<CanonicalUrlMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

// 本番は発行時に wwwroot へ配置した React のビルド成果物を配信する
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html");

await app.RunAsync();
return 0;

// 統合テストから参照するため
public partial class Program;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
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

// ---- ログ（IIS ではコンソールを確認できないため、ファイルにも出力する）----
var fileLogSection = builder.Configuration.GetSection(FileLoggingSettings.SectionName);
builder.Services.Configure<FileLoggingSettings>(fileLogSection);
if (fileLogSection.Get<FileLoggingSettings>()?.Enabled ?? true)
    builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

// ---- データベース（現在：SQLite / 移行先：SQL Server・Azure SQL Database）----
var connectionString = builder.Configuration.GetConnectionString(DatabaseSettings.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException($"接続文字列 '{DatabaseSettings.ConnectionStringName}' が設定されていません。");
builder.Services.AddShareMasterDatabase(dbSettings, connectionString);

// ---- 認証（本番：Windows 認証 または Microsoft Entra ID / 開発：擬似ログイン）----
if (authSettings.IsDevelopmentMode)
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("本番環境では Auth:Mode=Development は使用できません。");
    builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, null);
    builder.Services.AddScoped<IUserDirectory, DevUserDirectory>();
}
else if (authSettings.IsWindowsMode)
{
    // IIS 配下では IIS の Windows 認証に委譲する（IIS 側で Windows 認証の有効化が必要）。Kestrel 単体でも動作する
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
    // 利用者の検索と表示名の取得は Active Directory から行う
    builder.Services.AddScoped<IUserDirectory, ActiveDirectoryUserDirectory>();
    builder.Services.AddScoped<IClaimsTransformation, DirectoryClaimsTransformation>();
}
else if (authSettings.IsEntraIdMode)
{
    // 利用者の検索は Microsoft Graph API から行う（アプリの許可 User.Read.All と AzureAd:ClientCredentials が必要）
    builder.Services.AddAuthentication().AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();
    builder.Services.AddHttpClient<IUserDirectory, GraphUserDirectory>();
}
else
{
    throw new InvalidOperationException($"Auth:Mode '{authSettings.Mode}' は不正です（Windows / EntraId / Development のいずれか）。");
}
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.Configure<UserDirectorySettings>(builder.Configuration.GetSection(UserDirectorySettings.SectionName));

// ---- アプリケーションサービス ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<HistoryWriter>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<DataMaintenanceService>();

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

app.Logger.LogInformation("起動します（環境：{Environment}、認証：{AuthMode}、DB：{Provider}、フォルダ：{ContentRoot}）",
    app.Environment.EnvironmentName, authSettings.NormalizedMode, dbSettings.Provider, app.Environment.ContentRootPath);

try
{
    await app.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "データベースの初期化に失敗しました。");
    throw;
}

// エラー応答（4xx/5xx）をログに残す。例外ハンドラーより外側に置き、最終的なステータスを記録する
app.UseMiddleware<RequestErrorLoggingMiddleware>();
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

// 本番は発行時に wwwroot へ配置した React のビルド成果物を配信する（index.html は MapSpaFallback で返す）
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapSpaFallback();

await app.RunAsync();
return 0;

// 統合テストから参照するため
public partial class Program;

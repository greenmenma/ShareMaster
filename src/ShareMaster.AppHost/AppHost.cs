// ローカル開発・デバッグ専用のオーケストレーター。
// SQL Server はコンテナ（エミュレーター）で起動し、本番（Azure SQL Database 等）とは接続文字列のみ差し替える。
var builder = DistributedApplication.CreateBuilder(args);

// パスワードは Aspire が自動生成し、AppHost の user-secrets に保存される（リポジトリには含めない）
var sql = builder.AddSqlServer("sql")
    .WithDataVolume("sharemaster-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);
var db = sql.AddDatabase("ShareMasterDb");

var server = builder.AddProject<Projects.ShareMaster_Server>("server")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Database__MigrateOnStartup", "true")
    .WithEnvironment("Database__SeedSampleData", "true")
    .WithHttpHealthCheck("/health");

builder.AddViteApp("client", "../ShareMaster.Client")
    .WithReference(server)
    .WaitFor(server)
    .WithExternalHttpEndpoints();

builder.Build().Run();

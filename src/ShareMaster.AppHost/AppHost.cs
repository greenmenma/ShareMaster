// ローカル開発・デバッグ専用のオーケストレーター（コンテナは使用しない）。
// DB は Server の設定（appsettings.Development.json）どおり SQLite ファイルを使う。
var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.ShareMaster_Server>("server")
    .WithHttpHealthCheck("/health");

builder.AddViteApp("client", "../ShareMaster.Client")
    // VS Code の Chrome デバッグ構成から接続できるようポートを固定する
    .WithEndpoint("http", e => e.Port = 5173)
    .WithReference(server)
    .WaitFor(server)
    .WithExternalHttpEndpoints();

builder.Build().Run();

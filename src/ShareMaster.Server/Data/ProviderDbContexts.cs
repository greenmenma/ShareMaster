using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShareMaster.Server.Data;

// DB 製品ごとに DbContext を派生させ、マイグレーションを別フォルダで管理する。
//   SQLite     : Data/Migrations/Sqlite
//   SQL Server : Data/Migrations/SqlServer
// モデルを変更したら両方にマイグレーションを追加すること（docs/03_開発環境.md 参照）。

/// <summary>SQLite 用（現在の運用 DB）</summary>
public sealed class SqliteShareMasterDbContext(DbContextOptions<SqliteShareMasterDbContext> options) : ShareMasterDbContext(options);

/// <summary>SQL Server / Azure SQL Database 用（移行先）</summary>
public sealed class SqlServerShareMasterDbContext(DbContextOptions<SqlServerShareMasterDbContext> options) : ShareMasterDbContext(options);

/// <summary>dotnet ef 用（SQLite）。接続先は不要</summary>
public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteShareMasterDbContext>
{
    public SqliteShareMasterDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqliteShareMasterDbContext>().UseSqlite("Data Source=design-time.db").Options);
}

/// <summary>dotnet ef 用（SQL Server）。接続先は不要</summary>
public sealed class SqlServerDesignTimeFactory : IDesignTimeDbContextFactory<SqlServerShareMasterDbContext>
{
    public SqlServerShareMasterDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqlServerShareMasterDbContext>().UseSqlServer("Server=design-time").Options);
}

public static class DatabaseServiceExtensions
{
    /// <summary>設定（Database:Provider）に応じて DbContext を登録する。サービスは ShareMasterDbContext で解決する</summary>
    public static IServiceCollection AddShareMasterDatabase(this IServiceCollection services, DatabaseSettings settings, string connectionString)
    {
        if (settings.IsSqlServer)
        {
            services.AddDbContext<ShareMasterDbContext, SqlServerShareMasterDbContext>(o =>
                o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        }
        else
        {
            EnsureSqliteDirectory(connectionString);
            services.AddDbContext<ShareMasterDbContext, SqliteShareMasterDbContext>(o => o.UseSqlite(connectionString));
        }
        return services;
    }

    /// <summary>SQLite のファイルを置くフォルダが無ければ作成する</summary>
    public static void EnsureSqliteDirectory(string connectionString)
    {
        var path = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrEmpty(path) || path == ":memory:") return;
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}

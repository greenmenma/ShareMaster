namespace ShareMaster.Server.Data;

/// <summary>データベース設定（appsettings の "Database" セクション）</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "ShareMasterDb";

    /// <summary>"Sqlite"（既定・現在の運用）または "SqlServer"（SQL Server / Azure SQL Database）</summary>
    public string Provider { get; set; } = "Sqlite";

    /// <summary>起動時にマイグレーションを適用するか</summary>
    public bool MigrateOnStartup { get; set; } = true;

    /// <summary>起動時にサンプルデータを投入するか（カレンダーが 0 件の場合のみ）</summary>
    public bool SeedSampleData { get; set; }

    public bool IsSqlServer => string.Equals(Provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
}

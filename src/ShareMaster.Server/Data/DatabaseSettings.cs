namespace ShareMaster.Server.Data;

/// <summary>データベース設定（appsettings の "Database" セクション）</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "ShareMasterDb";

    /// <summary>"SqlServer"（既定・本番）または "Sqlite"（コンテナを使わない簡易起動用）</summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>起動時にマイグレーションを適用するか</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>起動時にサンプルデータを投入するか（カレンダーが 0 件の場合のみ）</summary>
    public bool SeedSampleData { get; set; }

    public bool IsSqlite => string.Equals(Provider, "Sqlite", StringComparison.OrdinalIgnoreCase);
}

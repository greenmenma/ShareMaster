namespace ShareMaster.Server.Hosting;

/// <summary>正規 URL へのリダイレクト設定（appsettings の "CanonicalUrl" セクション）</summary>
public sealed class CanonicalUrlSettings
{
    public const string SectionName = "CanonicalUrl";

    /// <summary>HTTP でのアクセスを HTTPS へリダイレクトするか</summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>リダイレクト先の HTTPS ポート。未指定なら 443（URL にポートを付けない）</summary>
    public int? HttpsPort { get; set; }

    /// <summary>
    /// ホスト名だけ（ドットなし）でアクセスされたときに付与するドメイン。
    /// "*.hoge.co.jp" または "hoge.co.jp" の形式で指定する。空なら付与しない。
    /// </summary>
    public string Domain { get; set; } = "";
}

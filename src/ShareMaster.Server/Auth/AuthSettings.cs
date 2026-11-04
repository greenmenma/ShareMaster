namespace ShareMaster.Server.Auth;

/// <summary>認証設定（appsettings の "Auth" セクション）</summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";
    public const string ModeDevelopment = "Development";
    public const string ModeEntraId = "EntraId";

    /// <summary>"EntraId"（本番）または "Development"（ローカル開発用の擬似ログイン）</summary>
    public string Mode { get; set; } = ModeEntraId;

    /// <summary>初期システム管理者（UserKey＝UPN/メールアドレス）。システム管理者が 0 件の場合に登録される</summary>
    public string[] BootstrapAdmins { get; set; } = [];

    /// <summary>SPA（React）がサインインに使う設定</summary>
    public SpaAuthSettings Spa { get; set; } = new();

    /// <summary>開発用ユーザー（Mode=Development のときのみ使用）</summary>
    public List<DevUser> DevUsers { get; set; } = [];

    public bool IsDevelopmentMode => string.Equals(Mode, ModeDevelopment, StringComparison.OrdinalIgnoreCase);
}

public sealed class SpaAuthSettings
{
    public string? ClientId { get; set; }
    public string? Authority { get; set; }
    public string[] Scopes { get; set; } = [];
}

public sealed class DevUser
{
    public string UserKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

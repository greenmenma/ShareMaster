namespace ShareMaster.Server.Auth;

/// <summary>認証設定（appsettings の "Auth" セクション）</summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";
    public const string ModeDevelopment = "Development";
    public const string ModeEntraId = "EntraId";
    public const string ModeWindows = "Windows";

    /// <summary>
    /// "Windows"（本番・既定：IIS の Windows 認証）、"EntraId"（本番：Microsoft Entra ID）、
    /// "Development"（ローカル開発用の擬似ログイン）のいずれか
    /// </summary>
    public string Mode { get; set; } = ModeWindows;

    /// <summary>初期システム管理者（UserKey＝UPN/メールアドレス）。システム管理者が 0 件の場合に登録される</summary>
    public string[] BootstrapAdmins { get; set; } = [];

    /// <summary>SPA（React）がサインインに使う設定</summary>
    public SpaAuthSettings Spa { get; set; } = new();

    /// <summary>開発用ユーザー（Mode=Development のときのみ使用）</summary>
    public List<DevUser> DevUsers { get; set; } = [];

    public bool IsDevelopmentMode => string.Equals(Mode, ModeDevelopment, StringComparison.OrdinalIgnoreCase);
    public bool IsWindowsMode => string.Equals(Mode, ModeWindows, StringComparison.OrdinalIgnoreCase);
    public bool IsEntraIdMode => string.Equals(Mode, ModeEntraId, StringComparison.OrdinalIgnoreCase);

    /// <summary>画面へ返す正規化したモード名</summary>
    public string NormalizedMode => IsDevelopmentMode ? ModeDevelopment : IsWindowsMode ? ModeWindows : ModeEntraId;
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

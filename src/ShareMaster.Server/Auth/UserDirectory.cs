using System.DirectoryServices;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using ShareMaster.Server.Data;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Auth;

/// <summary>ディレクトリ（AD / Entra ID）の利用者</summary>
public sealed record DirectoryUser(string UserKey, string DisplayName, string? Email, string? Department);

/// <summary>
/// 利用者の検索・取得。認証方式に応じて実装を切り替える。
/// Windows 認証：Active Directory（LDAP）、Entra ID：Microsoft Graph API、開発：開発用ユーザー。
/// </summary>
public interface IUserDirectory
{
    /// <summary>氏名・ユーザー名・メールアドレスの部分一致で検索する</summary>
    Task<IReadOnlyList<DirectoryUser>> SearchAsync(string search, int max, CancellationToken ct = default);

    /// <summary>UserKey で 1 件取得する（見つからなければ null）</summary>
    Task<DirectoryUser?> FindAsync(string userKey, CancellationToken ct = default);
}

/// <summary>利用者ディレクトリの設定（appsettings の "UserDirectory" セクション）</summary>
public sealed class UserDirectorySettings
{
    public const string SectionName = "UserDirectory";

    /// <summary>
    /// AD の検索起点（例：LDAP://DC=corp,DC=example,DC=co,DC=jp）。
    /// 未指定ならサーバーが参加しているドメイン全体を検索する
    /// </summary>
    public string? LdapPath { get; set; }

    /// <summary>
    /// UserKey（DOMAIN\ユーザー名）に使うドメイン名（NetBIOS 名。例：CORP）。
    /// 未指定ならサインイン中の利用者のドメインを使う
    /// </summary>
    public string? NetBiosDomain { get; set; }
}

// ---------------------------------------------------------------- Active Directory

/// <summary>Active Directory から利用者を検索する（Windows 認証用。Windows サーバーでのみ動作）</summary>
public sealed class ActiveDirectoryUserDirectory(
    IOptions<UserDirectorySettings> options,
    IHttpContextAccessor accessor,
    ILogger<ActiveDirectoryUserDirectory> logger) : IUserDirectory
{
    private static readonly string[] Properties = ["sAMAccountName", "displayName", "mail", "department"];

    public Task<IReadOnlyList<DirectoryUser>> SearchAsync(string search, int max, CancellationToken ct = default)
    {
        var term = Escape(search.Trim());
        if (term.Length == 0) return Task.FromResult<IReadOnlyList<DirectoryUser>>([]);
        var filter = $"(|(displayName=*{term}*)(sAMAccountName=*{term}*)(mail=*{term}*)(sn=*{term}*)(givenName=*{term}*))";
        return Task.Run(() => Query(filter, max), ct);
    }

    public async Task<DirectoryUser?> FindAsync(string userKey, CancellationToken ct = default)
    {
        var account = userKey.Contains('\\') ? userKey[(userKey.IndexOf('\\') + 1)..] : userKey;
        if (string.IsNullOrWhiteSpace(account)) return null;
        var list = await Task.Run(() => Query($"(sAMAccountName={Escape(account)})", 1), ct);
        return list.FirstOrDefault();
    }

    private IReadOnlyList<DirectoryUser> Query(string condition, int max)
    {
        if (!OperatingSystem.IsWindows())
            throw ServiceException.BadRequest("Active Directory の検索は Windows サーバーでのみ利用できます。");
        try
        {
            return QueryWindows(condition, max);
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Active Directory の検索に失敗しました（条件：{Filter}）", condition);
            throw ServiceException.BadRequest("Active Directory から利用者を取得できませんでした。管理者に連絡してください。");
        }
    }

    [SupportedOSPlatform("windows")]
    private List<DirectoryUser> QueryWindows(string condition, int max)
    {
        var s = options.Value;
        var domain = ResolveDomain(s);
        // 有効なユーザーアカウントのみ（無効化されたアカウントを除外）
        var filter = $"(&(objectCategory=person)(objectClass=user)(!(userAccountControl:1.2.840.113556.1.4.803:=2)){condition})";

        using var root = string.IsNullOrWhiteSpace(s.LdapPath) ? new DirectoryEntry() : new DirectoryEntry(s.LdapPath);
        using var searcher = new DirectorySearcher(root, filter, Properties) { SizeLimit = max, PageSize = 0, ClientTimeout = TimeSpan.FromSeconds(15) };
        searcher.Sort = new SortOption("displayName", SortDirection.Ascending);
        using var results = searcher.FindAll();

        var list = new List<DirectoryUser>();
        foreach (SearchResult r in results)
        {
            var sam = Get(r, "sAMAccountName");
            if (string.IsNullOrEmpty(sam)) continue;
            var key = (string.IsNullOrEmpty(domain) ? sam : $"{domain}\\{sam}").ToLowerInvariant();
            list.Add(new DirectoryUser(key, Get(r, "displayName") ?? sam, Get(r, "mail"), Get(r, "department")));
        }
        return list;
    }

    [SupportedOSPlatform("windows")]
    private static string? Get(SearchResult r, string name) =>
        r.Properties[name] is { Count: > 0 } values ? values[0]?.ToString() : null;

    private string? ResolveDomain(UserDirectorySettings s)
    {
        if (!string.IsNullOrWhiteSpace(s.NetBiosDomain)) return s.NetBiosDomain.Trim();
        // サインイン中の利用者（DOMAIN\user）と同じドメインとみなす
        var name = accessor.HttpContext?.User.Identity?.Name;
        if (name?.IndexOf('\\') is int i and > 0) return name[..i];
        return null;
    }

    /// <summary>LDAP フィルターの特殊文字をエスケープする（RFC 4515）</summary>
    public static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '\\' => @"\5c",
                '*' => @"\2a",
                '(' => @"\28",
                ')' => @"\29",
                '\0' => @"\00",
                _ => c.ToString(),
            });
        }
        return sb.ToString();
    }
}

// ---------------------------------------------------------------- Microsoft Graph

/// <summary>
/// Microsoft Graph API から利用者を検索する（Entra ID 用）。
/// アプリ登録にアプリケーションの許可 User.Read.All（管理者の同意）と、
/// クライアントシークレットまたは証明書（AzureAd:ClientCredentials）が必要。
/// </summary>
public sealed class GraphUserDirectory(HttpClient http, IAuthorizationHeaderProvider tokens, ILogger<GraphUserDirectory> logger) : IUserDirectory
{
    private const string GraphScope = "https://graph.microsoft.com/.default";
    private const string Select = "$select=displayName,userPrincipalName,mail,department";

    public async Task<IReadOnlyList<DirectoryUser>> SearchAsync(string search, int max, CancellationToken ct = default)
    {
        // $search の値に含められない文字を除く
        var term = search.Trim().Replace("\"", "").Replace("\\", "");
        if (term.Length == 0) return [];
        var query = Uri.EscapeDataString($"\"displayName:{term}\" OR \"userPrincipalName:{term}\" OR \"mail:{term}\"");
        var users = await GetAsync($"users?$search={query}&$orderby=displayName&$top={max}&{Select}", ct);
        return users;
    }

    public async Task<DirectoryUser?> FindAsync(string userKey, CancellationToken ct = default)
    {
        var upn = userKey.Replace("'", "''");
        var users = await GetAsync($"users?$filter={Uri.EscapeDataString($"userPrincipalName eq '{upn}'")}&{Select}", ct);
        return users.FirstOrDefault();
    }

    private async Task<IReadOnlyList<DirectoryUser>> GetAsync(string relativeUrl, CancellationToken ct)
    {
        try
        {
            var header = await tokens.CreateAuthorizationHeaderForAppAsync(GraphScope, cancellationToken: ct);
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/{relativeUrl}");
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(header);
            // $search を使うために必要
            req.Headers.Add("ConsistencyLevel", "eventual");
            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogError("Graph API の呼び出しに失敗しました（{Status}）：{Body}", (int)res.StatusCode, await res.Content.ReadAsStringAsync(ct));
                throw ServiceException.BadRequest("Entra ID から利用者を取得できませんでした。管理者に連絡してください。");
            }
            var body = await res.Content.ReadFromJsonAsync<GraphUserList>(ct);
            return body?.Value
                .Where(x => !string.IsNullOrEmpty(x.UserPrincipalName))
                .Select(x => new DirectoryUser(x.UserPrincipalName!.ToLowerInvariant(), x.DisplayName ?? x.UserPrincipalName!, x.Mail, x.Department))
                .ToList() ?? [];
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Graph API の呼び出しに失敗しました");
            throw ServiceException.BadRequest("Entra ID から利用者を取得できませんでした。管理者に連絡してください。");
        }
    }

    private sealed class GraphUserList
    {
        [JsonPropertyName("value")] public List<GraphUser> Value { get; set; } = [];
    }

    private sealed class GraphUser
    {
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("userPrincipalName")] public string? UserPrincipalName { get; set; }
        [JsonPropertyName("mail")] public string? Mail { get; set; }
        [JsonPropertyName("department")] public string? Department { get; set; }
    }
}

// ---------------------------------------------------------------- 開発用

/// <summary>開発用：appsettings の開発用ユーザーと、ログイン済みの利用者から検索する</summary>
public sealed class DevUserDirectory(IOptions<AuthSettings> settings, ShareMasterDbContext db) : IUserDirectory
{
    public async Task<IReadOnlyList<DirectoryUser>> SearchAsync(string search, int max, CancellationToken ct = default)
    {
        var all = await AllAsync(ct);
        var term = search.Trim();
        return all.Where(x => term.Length == 0
                              || x.UserKey.Contains(term, StringComparison.OrdinalIgnoreCase)
                              || x.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayName).Take(max).ToList();
    }

    public async Task<DirectoryUser?> FindAsync(string userKey, CancellationToken ct = default) =>
        (await AllAsync(ct)).FirstOrDefault(x => string.Equals(x.UserKey, userKey, StringComparison.OrdinalIgnoreCase));

    private async Task<List<DirectoryUser>> AllAsync(CancellationToken ct)
    {
        var list = settings.Value.DevUsers.Select(x => new DirectoryUser(x.UserKey.ToLowerInvariant(), x.DisplayName, x.UserKey, "開発用")).ToList();
        var known = await db.Users.AsNoTracking().Select(x => new DirectoryUser(x.UserKey, x.DisplayName, x.Email, null)).ToListAsync(ct);
        list.AddRange(known.Where(k => list.All(x => x.UserKey != k.UserKey)));
        return list;
    }
}

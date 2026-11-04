using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;

namespace ShareMaster.Server.Services;

/// <summary>変更履歴の書き込み（呼び出し元の SaveChanges でまとめて保存される）</summary>
public sealed class HistoryWriter(ShareMasterDbContext db, ICurrentUser user, TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public void Add(string entityType, string entityKey, string action, string summary, object? before, object? after, int? calendarId = null)
    {
        db.ChangeHistories.Add(new ChangeHistory
        {
            CalendarId = calendarId,
            EntityType = entityType,
            EntityKey = entityKey,
            Action = action,
            Summary = summary.Length > 1000 ? summary[..1000] : summary,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            ChangedBy = user.UserKey,
            ChangedByName = user.DisplayName,
            ChangedAt = clock.GetUtcNow(),
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;
using ShareMaster.Server.Domain;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Controllers;

[Route("api/public-holidays")]
public sealed class PublicHolidaysController(ShareMasterDbContext db, HistoryWriter history) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PublicHolidayDto>> Get([FromQuery] int year, CancellationToken ct)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        return await db.PublicHolidays.AsNoTracking().Where(x => x.Date >= from && x.Date <= to).OrderBy(x => x.Date)
            .Select(x => new PublicHolidayDto(x.Id, x.Date, x.Name)).ToListAsync(ct);
    }

    /// <summary>法律に基づき指定年の祝日を自動生成する（既存日付はスキップ）</summary>
    [HttpPost("generate"), RequireSystemAdmin]
    public async Task<GenerateResultDto> Generate([FromQuery] int year, CancellationToken ct)
    {
        if (year is < 2000 or > 2099) throw ServiceException.BadRequest("対象は 2000〜2099 年です。");
        var (added, skipped) = await AddGeneratedAsync(db, year, ct);
        if (added > 0) history.Add(nameof(PublicHoliday), year.ToString(), "Create", $"{year} 年の祝日を自動生成（{added} 件）", null, null);
        await db.SaveChangesAsync(ct);
        return new GenerateResultDto(added, skipped);
    }

    [HttpPost, RequireSystemAdmin]
    public async Task<PublicHolidayDto> Create(PublicHolidaySaveRequest req, CancellationToken ct)
    {
        if (await db.PublicHolidays.AnyAsync(x => x.Date == req.Date, ct)) throw ServiceException.Conflict("同じ日付の祝日が既に登録されています。");
        var e = new PublicHoliday { Date = req.Date, Name = req.Name.Trim() };
        db.PublicHolidays.Add(e);
        history.Add(nameof(PublicHoliday), req.Date.ToString("yyyy-MM-dd"), "Create", $"祝日 {req.Date:yyyy/MM/dd}「{e.Name}」を追加", null, req);
        await db.SaveChangesAsync(ct);
        return new PublicHolidayDto(e.Id, e.Date, e.Name);
    }

    [HttpPut("{id:int}"), RequireSystemAdmin]
    public async Task<PublicHolidayDto> Update(int id, PublicHolidaySaveRequest req, CancellationToken ct)
    {
        var e = await db.PublicHolidays.FindAsync([id], ct) ?? throw ServiceException.NotFound();
        if (await db.PublicHolidays.AnyAsync(x => x.Date == req.Date && x.Id != id, ct)) throw ServiceException.Conflict("同じ日付の祝日が既に登録されています。");
        var before = new PublicHolidayDto(e.Id, e.Date, e.Name);
        e.Date = req.Date;
        e.Name = req.Name.Trim();
        history.Add(nameof(PublicHoliday), req.Date.ToString("yyyy-MM-dd"), "Update", $"祝日 {req.Date:yyyy/MM/dd}「{e.Name}」を変更", before, req);
        await db.SaveChangesAsync(ct);
        return new PublicHolidayDto(e.Id, e.Date, e.Name);
    }

    [HttpDelete("{id:int}"), RequireSystemAdmin]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await db.PublicHolidays.FindAsync([id], ct) ?? throw ServiceException.NotFound();
        db.PublicHolidays.Remove(e);
        history.Add(nameof(PublicHoliday), e.Date.ToString("yyyy-MM-dd"), "Delete", $"祝日 {e.Date:yyyy/MM/dd}「{e.Name}」を削除", new PublicHolidayDto(e.Id, e.Date, e.Name), null);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>指定年の祝日を追加する（保存は呼び出し元）</summary>
    public static async Task<(int Added, int Skipped)> AddGeneratedAsync(ShareMasterDbContext db, int year, CancellationToken ct)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        var existing = (await db.PublicHolidays.Where(x => x.Date >= from && x.Date <= to).Select(x => x.Date).ToListAsync(ct)).ToHashSet();
        int added = 0, skipped = 0;
        foreach (var (date, name) in JapaneseHolidayCalculator.GetHolidays(year))
        {
            if (existing.Contains(date)) { skipped++; continue; }
            db.PublicHolidays.Add(new PublicHoliday { Date = date, Name = name });
            added++;
        }
        return (added, skipped);
    }
}

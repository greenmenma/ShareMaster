using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Controllers;

[Route("api/day-types")]
public sealed class DayTypesController(ShareMasterDbContext db, HistoryWriter history) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<DayTypeDto>> GetAll(CancellationToken ct) =>
        await db.DayTypes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new DayTypeDto(x.Id, x.Code, x.Name, x.Color, x.IsHoliday, x.SortOrder, x.IsSystem)).ToListAsync(ct);

    [HttpPost, RequireSystemAdmin]
    public async Task<DayTypeDto> Create(DayTypeSaveRequest req, CancellationToken ct)
    {
        if (await db.DayTypes.AnyAsync(x => x.Code == req.Code, ct)) throw ServiceException.Conflict("コードが重複しています。");
        var e = new DayType();
        Apply(e, req);
        db.DayTypes.Add(e);
        await db.SaveChangesAsync(ct);
        history.Add(nameof(DayType), e.Id.ToString(), "Create", $"日種別「{e.Name}」を作成", null, req);
        await db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    [HttpPut("{id:int}"), RequireSystemAdmin]
    public async Task<DayTypeDto> Update(int id, DayTypeSaveRequest req, CancellationToken ct)
    {
        var e = await db.DayTypes.FindAsync([id], ct) ?? throw ServiceException.NotFound();
        if (await db.DayTypes.AnyAsync(x => x.Code == req.Code && x.Id != id, ct)) throw ServiceException.Conflict("コードが重複しています。");
        if (e.IsSystem && e.Code != req.Code) throw ServiceException.BadRequest("システム予約の日種別はコードを変更できません。");
        var before = ToDto(e);
        Apply(e, req);
        history.Add(nameof(DayType), id.ToString(), "Update", $"日種別「{e.Name}」を変更", before, ToDto(e));
        await db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    [HttpDelete("{id:int}"), RequireSystemAdmin]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await db.DayTypes.FindAsync([id], ct) ?? throw ServiceException.NotFound();
        if (e.IsSystem) throw ServiceException.BadRequest("システム予約の日種別は削除できません。");
        if (await db.CalendarDays.AnyAsync(x => x.DayTypeId == id, ct)) throw ServiceException.Conflict("使用中の日種別は削除できません。");
        db.DayTypes.Remove(e);
        history.Add(nameof(DayType), id.ToString(), "Delete", $"日種別「{e.Name}」を削除", ToDto(e), null);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(DayType e, DayTypeSaveRequest req)
    {
        e.Code = req.Code.Trim();
        e.Name = req.Name.Trim();
        e.Color = req.Color.ToUpperInvariant();
        e.IsHoliday = req.IsHoliday;
        e.SortOrder = req.SortOrder;
    }

    private static DayTypeDto ToDto(DayType x) => new(x.Id, x.Code, x.Name, x.Color, x.IsHoliday, x.SortOrder, x.IsSystem);
}

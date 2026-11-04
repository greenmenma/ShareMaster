using Microsoft.AspNetCore.Mvc;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Controllers;

public sealed class CalendarsController(CalendarService service) : ApiControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CalendarDto>> GetAll(CancellationToken ct) => service.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    public Task<CalendarDto> Get(int id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<CalendarDto>> Create(CalendarSaveRequest req, CancellationToken ct)
    {
        var created = await service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public Task<CalendarDto> Update(int id, CalendarSaveRequest req, CancellationToken ct) => service.UpdateAsync(id, req, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>複数カレンダーの確定済み日情報（一覧グリッド用）。ids 省略時は全カレンダー</summary>
    [HttpGet("days")]
    public Task<IReadOnlyList<CalendarDaysDto>> GetDays([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int[]? ids, CancellationToken ct)
        => service.GetDaysAsync(ids, from, to, ct);

    /// <summary>単一カレンダーの確定済み日情報</summary>
    [HttpGet("{id:int}/days")]
    public async Task<CalendarDaysDto> GetCalendarDays(int id, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => (await service.GetDaysAsync([id], from, to, ct)).SingleOrDefault() ?? throw ServiceException.NotFound("カレンダーが見つかりません。");

    [HttpGet("{id:int}/days/{date}")]
    public Task<DayDetailDto> GetDay(int id, DateOnly date, CancellationToken ct) => service.GetDayDetailAsync(id, date, ct);

    /// <summary>日別設定の更新（単日・期間一括）</summary>
    [HttpPut("{id:int}/days")]
    public Task<DayUpdateResultDto> UpdateDays(int id, DayUpdateRequest req, CancellationToken ct) => service.UpdateDaysAsync(id, req, ct);

    [HttpGet("year-range")]
    public Task<YearRangeDto> GetYearRange(CancellationToken ct) => service.GetYearRangeAsync(ct);
}

using Microsoft.AspNetCore.Mvc;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Controllers;

/// <summary>データ管理（システム管理者のみ）</summary>
[Route("api/data-maintenance")]
[RequireSystemAdmin]
public sealed class DataMaintenanceController(DataMaintenanceService service) : ApiControllerBase
{
    /// <summary>システム管理者・利用者以外の全データを削除し、日種別・祝日を初期値に戻す</summary>
    [HttpPost("clear")]
    public Task<DataMaintenanceResultDto> Clear(CancellationToken ct) => service.ClearAllAsync(ct);

    /// <summary>サンプルデータを挿入する（カレンダーが 0 件のときのみ）</summary>
    [HttpPost("sample")]
    public Task<DataMaintenanceResultDto> InsertSample(CancellationToken ct) => service.InsertSampleAsync(ct);
}

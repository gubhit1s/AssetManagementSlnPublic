using Microsoft.AspNetCore.Mvc;
using AssetManagementApi.Models;
using AssetManagementApi.Models.Reports;
using OfficeOpenXml;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Extensions;
using AssetManagementApi.Models.AdUserGeneration;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/download")]
public class ReportController : ControllerBase {

    private readonly ILogger<ReportController> _logger;
    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;

    public ReportController(ILogger<ReportController> logger, AssetContext assetContext, IAdUser adUser)
    {
        _logger = logger;
        _assetContext = assetContext;
        _adUser = adUser;
    }

    [HttpGet("devices")]
    public async Task<ActionResult> GenerateIdentifiedDeviceReport()
    {
        List<DeviceReport> devices = await _assetContext.Devices.AsNoTracking().Select(d => new DeviceReport()
        {
            Id = d.Id,
            ServiceTag = d.ServiceTag,
            DeviceName = d.DeviceName,
            AcquiredDate = d.AcquiredDate.NullableDateToString(),
            DeviceModel = d.DeviceModel,
            PONumber = d.PONumber,
            DeviceStatus = d.DeviceStatus!.StatusName,
            DeviceType = d.DeviceType!.Name,
            CurrentUser = _assetContext.UserDevices.FirstOrDefault(ud => ud.DeviceId == d.Id && ud.UserOrderId == 0)!.UserName,
            LastUser1 = _assetContext.UserDevices.FirstOrDefault(ud => ud.DeviceId == d.Id && ud.UserOrderId == 1)!.UserName,
            LastUser2 = _assetContext.UserDevices.FirstOrDefault(ud => ud.DeviceId == d.Id && ud.UserOrderId == 2)!.UserName,
            //    LastUser1 = _userOrder.GetUserOfDeviceByOrder(d, 1),
            //    LastUser2 = _userOrder.GetUserOfDeviceByOrder(d, 2),
        }).ToListAsync();

        devices.ForEach(d =>
        {
            d.CurrentUserEmail = d.CurrentUser != null ? _adUser.GetEmail(d.CurrentUser) : null;
            d.LastUser1Email = d.LastUser1 != null ? _adUser.GetEmail(d.LastUser1) : null;
            d.LastUser2Email = d.LastUser2 != null ? _adUser.GetEmail(d.LastUser2) : null;
        });

        return GetExcelFile<DeviceReport>(devices);
    }

    [HttpGet("devicesUd")]
    public async Task<ActionResult> GenerateUnidentifiedDeviceReportAsync()
    {
        List<DeviceUdReport> devicesUd = await _assetContext.DevicesUnidentified.AsNoTracking().Select(du => new DeviceUdReport()
        {
            DeviceType = du.DeviceType!.Name,
            DeviceStatus = du.DeviceStatusType!.StatusName,
            Amount = du.Amount
        }).ToListAsync();

        return GetExcelFile<DeviceUdReport>(devicesUd);
    }
    private ActionResult GetExcelFile<T>(IEnumerable<T> data)
    {
        ExcelPackage ep = new ExcelPackage();
        ExcelWorksheet wsSheet1 = ep.Workbook.Worksheets.Add("Sheet1");
        wsSheet1.Cells["A1"].LoadFromCollection(data, true);
        wsSheet1.Cells[1, 1, 1, wsSheet1.Dimension.End.Column].Style.Font.Bold = true;
        wsSheet1.Protection.IsProtected = false;
        wsSheet1.Protection.AllowSelectLockedCells = false;
        wsSheet1.Cells[wsSheet1.Dimension.Address].AutoFitColumns();

        byte[] fileBytes = ep.GetAsByteArray();
        return new FileContentResult(fileBytes, "application/octet-stream")
        {
            FileDownloadName = "report.xlsx"
        };
    }
}
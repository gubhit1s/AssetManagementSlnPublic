using Microsoft.AspNetCore.Mvc;
using AssetManagementApi.Models;
using AssetManagementApi.Models.DTO;
using OfficeOpenXml;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Extensions;
using AssetManagementApi.Models.AdUserGeneration;

using System.IO;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{

    private readonly ILogger<ImportController> _logger;
    private readonly AssetContext _assetContext;
    private readonly IAdUser _adUser;

    public ImportController(ILogger<ImportController> logger, AssetContext assetContext, IAdUser adUser)
    {
        _logger = logger;
        _assetContext = assetContext;
        _adUser = adUser;
    }

    [HttpPost("devices"), DisableRequestSizeLimit]
    public async Task<ActionResult> UploadAsync(IFormFile file)
    {
        string folderName = "UploadedFiles";
        string pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        if (file.Length < 0)
        {
            return BadRequest("File is empty!");
        }
        if (file.Length > 512000)
        {
            return BadRequest("We only accept file less than 512 kb!");
        }

        string fileName = file.FileName;
        string fileExtension = Path.GetExtension(fileName);
        string[] acceptedExtensions = { ".xlsx" };
        if (!acceptedExtensions.Contains(fileExtension))
        {
            return BadRequest("The file extension is not accepted, must be an Excel (.xlsx) file!");
        }

        string fullPath = Path.Combine(pathToSave, fileName);
        string dbPath = Path.Combine(folderName, fileName);
        using (FileStream stream = new(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
            ExcelPackage package = new ExcelPackage(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            int rows = GetLastUsedRow(worksheet);
            int devicesAdded = 0;
            
            StringBuilder requiredServiceTagErrors = new StringBuilder();
            StringBuilder deviceTypeErrors = new StringBuilder();
            StringBuilder dateTimeErrors = new StringBuilder();
            StringBuilder dupeServiceTagErrors = new StringBuilder();

            int endCol = 6;
            bool error = false;
            List<string> serviceTags = new List<string>();
            for (int dRow = 2; dRow <= rows; dRow++)
            {
                ExcelRange curRow = worksheet.Cells[dRow, 1, dRow, endCol];
                
                var serviceTag = curRow[dRow, 1].GetValue<string>();
                if (string.IsNullOrWhiteSpace(serviceTag))
                {
                    error = true;
                    requiredServiceTagErrors.Append($" Cell A{dRow},");
                }
                if (_assetContext.Devices.Any(d => d.ServiceTag == serviceTag) || serviceTags.Contains(serviceTag))
                {
                    error = true;
                    dupeServiceTagErrors.Append($" Cell A{dRow},");
                }

                var deviceTypeName = curRow[dRow, 2].GetValue<string>();
                DeviceType? deviceType = null;
                if (string.IsNullOrWhiteSpace(deviceTypeName))
                {
                    error = true;
                    deviceTypeErrors.Append($" Cell B{dRow},");
                }
                else
                {
                    deviceType = await _assetContext.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name.ToLower() == deviceTypeName.ToLower());
                    if (deviceType is null)
                    {
                        error = true;
                        deviceTypeErrors.Append($" Cell B{dRow},");
                    }
                }

                var deviceName = curRow[dRow, 3].GetValue<string>();
                var acquiredDateStr = curRow[dRow, 4].GetValue<string>();
                bool dateParse = DateTime.TryParse(acquiredDateStr, out DateTime acquiredDate);
                if (!dateParse)
                {
                    error = true;
                    dateTimeErrors.Append($" Cell D{dRow},");
                }

                var deviceModel = curRow[dRow, 5].GetValue<string>();
                var poNumber = curRow[dRow, 6].GetValue<string>();

                //If an error occured, we stop adding and continue checking for other errors.
                if (error) continue;

                Device device = new Device()
                {
                    ServiceTag = serviceTag,
                    DeviceTypeId = deviceType!.Id,
                    DeviceName = deviceName,
                    AcquiredDate = acquiredDate,
                    DeviceModel = deviceModel,
                    PONumber = poNumber,
                    DeviceStatusId = 1
                };
                await _assetContext.Devices.AddAsync(device);

                Transfer transfer = new()
                {
                    Device = device,
                    TransferFromDestinationId = TransferDestinations.Vendor,
                    TransferToDestinationId = TransferDestinations.Stock,
                    TransferDate = DateTime.Now,
                    TransferTypeId = 1
                };

                await _assetContext.Transfers.AddAsync(transfer);

                
                serviceTags.Add(serviceTag);
                
            }

            string serviceTagErrorMsg = FormatErrorMessage(requiredServiceTagErrors, "Service Tag is required.");
            string deviceTypeErrorMsg = FormatErrorMessage(deviceTypeErrors, "Device Type is invalid. See the accepted values at Settings/Device Types.");
            string dateErrorMsg = FormatErrorMessage(dateTimeErrors, "Date is in incorrect format.");
            string dupeServiceTagMsg = FormatErrorMessage(dupeServiceTagErrors, "Service Tag already exists.");

            string errorMsg = string.Concat(serviceTagErrorMsg, deviceTypeErrorMsg, dateErrorMsg, dupeServiceTagMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return BadRequest($"Error: Cannot upload the selected file. Please see below for the error details. \n{errorMsg}");
            }

            try
            {
                if (!error) await _assetContext.SaveChangesAsync();
                return Accepted($"File successfully uploaded, total records added are {devicesAdded}");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    [HttpGet("devices")]
    public async Task<IActionResult> DownloadSampleFile()
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "TestImport.xlsx");
        byte[] excelBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return new FileContentResult(excelBytes, "application/octet-stream")
        {
            FileDownloadName = "SampleImport.xlsx"
        };
    }

    public static int GetLastUsedRow(ExcelWorksheet sheet)
    {
        if (sheet.Dimension == null)
        {
            return 0;
        }
        int row = sheet.Dimension.End.Row;

        while (row >= 1)
        {
            var range = sheet.Cells[row, 1, row, sheet.Dimension.End.Column];
            if (range.Any(c => !string.IsNullOrWhiteSpace(c.Text)))
            {
                break;
            }
            row--;
        }
        return row;
    }

    public static string FormatErrorMessage(StringBuilder errorMessage, string message)
    {
        return errorMessage.Length == 0 ? string.Empty :
               $"- {message} \nAt: {errorMessage.ToString().Trim(',')} \n";
    }

    

    [HttpGet("assignation"), DisableRequestSizeLimit]
    public async Task<ActionResult> DownloadAssignationSampleAsync()
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "TestAssignationImport.xlsx");
        byte[] excelBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return new FileContentResult(excelBytes, "application/octet-stream")
        {
            FileDownloadName = "SampleImport.xlsx"
        };
    }

    [HttpPost("assignation"), DisableRequestSizeLimit]
    public async Task<ActionResult> UploadUserDeviceAsync(IFormFile file)
    {
        string folderName = "UploadedFiles";
        string pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        if (file.Length < 0)
        {
            return BadRequest("File is empty!");
        }
        if (file.Length > 512000)
        {
            return BadRequest("We only accept file less than 512 kb!");
        }

        string fileName = file.FileName;
        string fileExtension = Path.GetExtension(fileName);
        string[] acceptedExtensions = { ".xlsx" };
        if (!acceptedExtensions.Contains(fileExtension))
        {
            return BadRequest("The file extension is not accepted, must be an Excel (.xlsx) file!");
        }

        string fullPath = Path.Combine(pathToSave, fileName);
        string dbPath = Path.Combine(folderName, fileName);
        List<(string serviceTag, string? userName)> userDevices = new List<(string, string?)>();

        using (FileStream stream = new(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
            ExcelPackage package = new ExcelPackage(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            int rows = GetLastUsedRow(worksheet);
            int userDevicesAdded = 0;
            
            StringBuilder serviceTagNotFoundErrors = new StringBuilder();
            StringBuilder userEmailNotFoundErrors = new StringBuilder();
            StringBuilder deviceNotInStockErrors = new StringBuilder();
            StringBuilder duplicateUserDeviceErrors = new StringBuilder();

            int endCol = 2;
            bool error = false;
            for (int dRow = 2; dRow <= rows; dRow++)
            {
                ExcelRange curRow = worksheet.Cells[dRow, 1, dRow, endCol];
                
                var serviceTag = curRow[dRow, 1].GetValue<string>();
                if (string.IsNullOrWhiteSpace(serviceTag))
                {
                    error = true;
                    serviceTagNotFoundErrors.Append($" Cell A{dRow},");
                }
                Device? device = await _assetContext.Devices.FirstOrDefaultAsync(d => d.ServiceTag == serviceTag);
                if (device == null)
                {
                    error = true;
                    serviceTagNotFoundErrors.Append($" Cell A{dRow},");
                }
                if (device != null && device.DeviceStatusId != 1)
                {
                    error = true;
                    deviceNotInStockErrors.Append($" Cell A{dRow}");
                }

                var userEmail = curRow[dRow, 2].GetValue<string>();
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    error = true;
                    userEmailNotFoundErrors.Append($" Cell B{dRow},");
                }
                User? user = _adUser.Users.FirstOrDefault(u => u.Email.ToLower() == userEmail.ToLower());
                if (user is null)
                {
                    error = true;
                    userEmailNotFoundErrors.Append($" Cell B{dRow},");
                }

                if (user is not null && userDevices.Contains((serviceTag, user.Email))) 
                {
                    error = true;
                    duplicateUserDeviceErrors.Append($" Row {dRow},");
                }

                //If an error occured, we stop adding and continue checking for other errors.
                if (error) continue;
                UserDevice ud = new UserDevice()
                {
                    DeviceId = device!.Id,
                    UserName = user!.UserName,
                    UserOrderId = 0
                };
                
                device.DeviceStatusId = 2; //Set device status to in use 
                await _assetContext.UserDevices.AddAsync(ud);
                _assetContext.Entry(device).State = EntityState.Modified;
                userDevicesAdded++;

                userDevices.Add((serviceTag, user!.Email));
            }

            string serviceTagErrorMsg = FormatErrorMessage(serviceTagNotFoundErrors, "Cannot find an existing Service Tag.");
            string userEmailErrorMsg = FormatErrorMessage(userEmailNotFoundErrors, "Cannot find the given user email.");
            string deviceNotInStockErrorMsg = FormatErrorMessage(deviceNotInStockErrors, "Device not currently in stock");
            string duplicateUserDeviceErrorMsg = FormatErrorMessage(duplicateUserDeviceErrors, "Duplicate Fields");

            string errorMsg = string.Concat(serviceTagErrorMsg, userEmailErrorMsg, deviceNotInStockErrorMsg, duplicateUserDeviceErrorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return BadRequest($"Error: Cannot upload the selected file. Please see below for the error details. \n{errorMsg}");
            }

            try
            {
                if (!error) await _assetContext.SaveChangesAsync();
                return Accepted($"File successfully uploaded, total records added are {userDevicesAdded++}");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    [HttpGet("assignation/unidentified"), DisableRequestSizeLimit]
    public async Task<ActionResult> DownloadAssignationUdSampleAsync()
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "TestAssignationUdImport.xlsx");
        byte[] excelBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return new FileContentResult(excelBytes, "application/octet-stream")
        {
            FileDownloadName = "SampleImport.xlsx"
        };
    }

    [HttpPost("assignation/unidentified"), DisableRequestSizeLimit]
    public async Task<ActionResult> UploadUserDeviceUdAsync(IFormFile file)
    {
        string folderName = "UploadedFiles";
        string pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        if (file.Length < 0)
        {
            return BadRequest("File is empty!");
        }
        if (file.Length > 512000)
        {
            return BadRequest("We only accept file less than 512 kb!");
        }

        string fileName = file.FileName;
        string fileExtension = Path.GetExtension(fileName);
        string[] acceptedExtensions = { ".xlsx" };
        if (!acceptedExtensions.Contains(fileExtension))
        {
            return BadRequest("The file extension is not accepted, must be an Excel (.xlsx) file!");
        }

        string fullPath = Path.Combine(pathToSave, fileName);
        string dbPath = Path.Combine(folderName, fileName);
        using (FileStream stream = new(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
            ExcelPackage package = new ExcelPackage(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            int rows = GetLastUsedRow(worksheet);
            int userDevicesAdded = 0;
            
            StringBuilder deviceTypeNotFoundErrors = new StringBuilder();
            StringBuilder userEmailNotFoundErrors = new StringBuilder();
            StringBuilder deviceOutOfStockErrors = new StringBuilder();
            StringBuilder duplicateUserDeviceErrors = new StringBuilder();

            int endCol = 2;
            bool error = false;
            
            for (int dRow = 2; dRow <= rows; dRow++)
            {
                ExcelRange curRow = worksheet.Cells[dRow, 1, dRow, endCol];
                
                var deviceTypeName = curRow[dRow, 1].GetValue<string>();
                if (string.IsNullOrWhiteSpace(deviceTypeName))
                {
                    error = true;
                    deviceTypeNotFoundErrors.Append($" Cell A{dRow},");
                }

                DeviceType? deviceType = await _assetContext.DeviceTypes.FirstOrDefaultAsync(d => d.Name == deviceTypeName);
                if (deviceType == null)
                {
                    error = true;
                    deviceTypeNotFoundErrors.Append($" Cell A{dRow},");
                } 

                var userEmail = curRow[dRow, 2].GetValue<string>();
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    error = true;
                    userEmailNotFoundErrors.Append($" Cell B{dRow},");
                }
                User? user = _adUser.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user is null)
                {
                    error = true;
                    userEmailNotFoundErrors.Append($" Cell B{dRow},");
                }

                //If an error occured, we stop adding and continue checking for other errors.
                if (error) continue;
                UserDeviceUd? existingInstance = await _assetContext.UserDevicesUd.FirstOrDefaultAsync(d =>
                    d.DeviceTypeId == deviceType!.Id && d.UserName == user!.UserName);
                if (existingInstance is not null)
                {
                    error = true;
                    duplicateUserDeviceErrors.Append($" Row {dRow},");
                    continue;
                }

                UserDeviceUd ud = new UserDeviceUd()
                {
                    DeviceTypeId = deviceType!.Id,
                    UserName = user!.UserName,
                };
                
                DeviceUnidentified inStockInstance = await GetDeviceUnidentifiedAsync(deviceType.Id, 1);
                DeviceUnidentified inUseInstance = await GetDeviceUnidentifiedAsync(deviceType.Id, 2);
                if (inStockInstance.Amount <= 0)
                {
                    error = true;
                    deviceOutOfStockErrors.Append($" Row {dRow}, ");
                    continue;
                }
                inStockInstance.Amount -= 1;
                inUseInstance.Amount += 1;
                await _assetContext.UserDevicesUd.AddAsync(ud);
                _assetContext.DevicesUnidentified.Update(inStockInstance);
                _assetContext.DevicesUnidentified.Update(inUseInstance);

            }

            string serviceTagErrorMsg = FormatErrorMessage(deviceTypeNotFoundErrors, "Device Type not found.");
            string userEmailErrorMsg = FormatErrorMessage(userEmailNotFoundErrors, "Cannot find the given user email.");
            string deviceOutOfStockErrorMsg = FormatErrorMessage(deviceOutOfStockErrors, "Amount input exceeds the current number in stock");
            string duplicateUserDeviceErrorMsg = FormatErrorMessage(duplicateUserDeviceErrors, "Cannot assign multiple devices of same type to one user");

            string errorMsg = string.Concat(serviceTagErrorMsg, userEmailErrorMsg, deviceOutOfStockErrorMsg, duplicateUserDeviceErrorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return BadRequest($"Error: Cannot upload the selected file. Please see below for the error details. \n{errorMsg}");
            }

            try
            {
                if (!error) await _assetContext.SaveChangesAsync();
                return Accepted($"File successfully uploaded, total records added are {userDevicesAdded++}");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    private async Task<DeviceUnidentified> GetDeviceUnidentifiedAsync(int deviceTypeId, int statusId)
    {
        var deviceUnidentified = await _assetContext.DevicesUnidentified.FirstOrDefaultAsync(
            du => du.DeviceTypeId == deviceTypeId && du.DeviceStatusId == statusId);

        if (deviceUnidentified == null)
        {
            throw new ArgumentException("Cannot find the device unidentified instance");
        }
        return deviceUnidentified;
    }


}

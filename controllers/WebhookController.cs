using BitrixSheets.models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Mvc;

namespace BitrixSheets.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class WebhookController : ControllerBase
    {
        [HttpPost("lead")]
        public async Task<IActionResult> ReceiveLead([FromBody] BitrixWebhookDto dto)
        {
            string jsonPath = "C:/Users/nikit/source/repos/BitrixSheets/auto-table-writer-7c3af8657884.json";
            string spreadsheetId = "1pfK-4sCaPvxVnzOLAO8pdw-r7vmKZm3mw1q-5KzmyhQ";
            string sheetName = "ЛПР";

            var credential = GoogleCredential
                .FromFile(jsonPath)
                .CreateScoped(SheetsService.Scope.Spreadsheets);

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Bitrix24 Integration"
            });

            string result = dto.Status.Contains("WON")
                ? "Успешный"
                : "Не успешный";
            var row = new List<object>
            {
                dto.DateClosed.ToString("dd.MM.yyyy"),
                $"https://yourcompany.bitrix24.ru/crm/lead/details/{dto.Id}/",
                dto.Comment,
                dto.Base,
                result
            };

            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { row }
            };

            var appendRequest = service.Spreadsheets.Values.Append(
                valueRange,
                spreadsheetId,
                $"{sheetName}!A:E"
            );

            appendRequest.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            await appendRequest.ExecuteAsync();

            return Ok();
        }
    }
}
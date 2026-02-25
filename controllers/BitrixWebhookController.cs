using BitrixSheets.models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace BitrixSheets.controllers
{
    [ApiController]
    [Route("api/bitrix")]
    public class BitrixWebhookController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BitrixWebhookController> _logger;

        public BitrixWebhookController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BitrixWebhookController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveBitrixEvent([FromBody] BitrixEventDto eventDto)
        {
            try
            {
                // 1. Проверяем, что это событие обновления лида
                if (eventDto.Event != "ONCRMLEADUPDATE")
                {
                    _logger.LogWarning($"Получено неподдерживаемое событие: {eventDto.Event}");
                    return Ok(); // Возвращаем Ok, чтобы Битрикс не считал это ошибкой
                }

                // 2. Получаем ID лида из события
                int leadId = eventDto.Data.FIELDS.ID;
                _logger.LogInformation($"Получено событие обновления лида ID: {leadId}");

                // 3. Получаем полные данные лида из Битрикс24
                var leadData = await GetLeadFromBitrix(leadId);
                if (leadData == null)
                {
                    _logger.LogError($"Не удалось получить данные лида {leadId} из Битрикс24");
                    return StatusCode(502, "Failed to get lead data from Bitrix24");
                }

                // 4. Преобразуем в ваш формат
                var yourFormatDto = MapToYourFormat(leadData);

                // 5. Отправляем на ваш локальный эндпоинт /webhook/lead
                bool forwarded = await ForwardToLocalWebhook(yourFormatDto);
                if (!forwarded)
                {
                    _logger.LogError($"Не удалось отправить данные на локальный вебхук для лида {leadId}");
                    return StatusCode(500, "Failed to forward data to local webhook");
                }

                _logger.LogInformation($"Данные лида {leadId} успешно обработаны и отправлены");
                return Ok(new { status = "success", leadId = leadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке вебхука от Битрикс24");
                return StatusCode(500, "Internal server error");
            }
        }

          private async Task<BitrixLeadResult> GetLeadFromBitrix(int leadId)
          {
              var client = _httpClientFactory.CreateClient();

              // URL вашего вебхука Битрикс24 (замените на свой!)
              string bitrixWebhookUrl = _configuration["Bitrix24:WebhookUrl"]
                  ?? "https://vkp.avtocod.ru/rest/1523/6y0050882wzzg7eu/";

              var requestBody = new { ID = leadId };
              var content = new StringContent(
                  JsonSerializer.Serialize(requestBody),
                  Encoding.UTF8,
                  "application/json");

              var response = await client.PostAsync(bitrixWebhookUrl, content);

              if (!response.IsSuccessStatusCode)
              {
                  var error = await response.Content.ReadAsStringAsync();
                  _logger.LogError($"Ошибка от Битрикс24: {error}");
                  return null;
              }

              var jsonResponse = await response.Content.ReadAsStringAsync();
              var leadResponse = JsonSerializer.Deserialize<BitrixLeadResponse>(jsonResponse);

              return leadResponse?.result;
          }

       /* private async Task<BitrixLeadResult> GetLeadFromBitrix(int leadId)
        {
            // ⚠️ ВРЕМЕННО: возвращаем тестовые данные без реального запроса
            _logger.LogInformation($"ТЕСТОВЫЙ РЕЖИМ: эмулируем получение лида {leadId}");

            // Имитация задержки, как будто был реальный запрос
            await Task.Delay(500);

            // Возвращаем тестовые данные, похожие на то, что приходит из Битрикса
            return new BitrixLeadResult
            {
                ID = leadId.ToString(),
                TITLE = $"Тестовый лид {leadId}",
                NAME = "Иван",
                LAST_NAME = "Петров",
                STATUS_ID = "WON",  // Попробуй поменять на "IN_PROCESS" чтобы увидеть разницу
                COMMENTS = "Это тестовые данные, созданные " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                DATE_CLOSED = DateTime.Now.ToString("yyyy-MM-dd")
            };
        }*/
        private BitrixWebhookDto MapToYourFormat(BitrixLeadResult lead)
        {
            // Преобразуем статус: если STATUS_ID = "WON" или "CONVERTED" - считаем успешным
            string status = lead.STATUS_ID;
            bool isWon = status == "WON" || status == "CONVERTED";

            // Парсим дату закрытия, если она есть
            DateTime? dateClosed = null;
            if (!string.IsNullOrEmpty(lead.DATE_CLOSED))
            {
                DateTime parsedDate;
                if (DateTime.TryParse(lead.DATE_CLOSED, out parsedDate))
                {
                    dateClosed = parsedDate;
                }
            }

            return new BitrixWebhookDto
            {
                Id = int.Parse(lead.ID),
                Status = status,
                // Берем комментарии или формируем из имени
                Comment = lead.COMMENTS ?? $"Лид: {lead.TITLE}",
                // База - название лида или имя контакта
                Base = !string.IsNullOrEmpty(lead.TITLE)
                    ? lead.TITLE
                    : $"{lead.NAME} {lead.LAST_NAME}".Trim(),
                DateClosed = dateClosed ?? DateTime.Now // Если нет даты закрытия, ставим текущую
            };
        }

        private async Task<bool> ForwardToLocalWebhook(BitrixWebhookDto data)
        {
            // Разрешаем все сертификаты (ТОЛЬКО ДЛЯ LOCALHOST В РАЗРАБОТКЕ!)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);

            string localUrl = "https://localhost:7008/webhook/lead"; // Важно: https!

            var content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json");

            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

            _logger.LogInformation($"Отправляю на локальный вебхук: {localUrl}");

            try
            {
                var response = await client.PostAsync(localUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Ответ: {(int)response.StatusCode} - {responseBody}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке на локальный вебхук");
                return false;
            }
        }
    }
}

# 📊 BitrixSheets

> Webhook-сервис на ASP.NET Core, который автоматически переносит данные о закрытых лидах из Битрикс24 в Google Sheets для аналитики.

---

## 📋 О проекте

**BitrixSheets** решает типичную задачу отдела продаж: аналитикам нужны данные о закрытых лидах в удобном виде, но выгружать их вручную из CRM — долго и неудобно.

Схема работы:
1. Менеджер закрывает лид в Битрикс24
2. Битрикс24 отправляет webhook на сервис
3. Сервис фильтрует события — реагирует только на закрытие лида
4. Забирает данные о лиде через Bitrix24 REST API
5. Записывает строку в Google Sheets

Аналитики получают таблицу, которая обновляется автоматически в реальном времени.

---

## ⚙️ Стек технологий

| Категория | Технология |
|---|---|
| Язык / Платформа | C#, ASP.NET Core |
| Входящие события | Bitrix24 Webhook |
| CRM интеграция | Bitrix24 REST API |
| Таблицы | Google Sheets API v4 |
| Авторизация Google | Service Account (JSON key) |

---

## 🏗️ Архитектура

```
BitrixSheets/
├── controllers/        # Webhook endpoint — принимает события от Битрикс24
├── models/             # Модели лида и данных для Google Sheets
├── Properties/         # Настройки проекта
├── Program.cs          # Точка входа, регистрация сервисов
└── appsettings.json    # Конфигурация (не хранить ключи в репо!)
```

### Поток данных

```
Битрикс24
    │
    │  POST webhook (событие изменения лида)
    ▼
WebhookController
    │
    │  Фильтрация: лид закрыт?
    ▼
BitrixService ──► Bitrix24 REST API (получить данные лида)
    │
    ▼
GoogleSheetsService ──► Google Sheets API (записать строку)
```

---

## 🚀 Запуск

### Требования

- .NET 8+ SDK
- Аккаунт Битрикс24 с правами на создание вебхуков
- Google Cloud проект с включённым Google Sheets API
- Сервисный аккаунт Google с доступом к таблице

### 1. Клонировать репозиторий

```bash
git clone https://github.com/nkikita/BitrixSheets.git
cd BitrixSheets
```

### 2. Настроить конфигурацию

Скопировать шаблон и заполнить значения:

```bash
cp appsettings.Example.json appsettings.json
```

```json
{
  "Bitrix": {
    "WebhookSecret": "YOUR_BITRIX_WEBHOOK_SECRET"
  },
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID",
    "ServiceAccountKeyPath": "service-account.json"
  }
}
```

### 3. Добавить ключ сервисного аккаунта Google

Положить файл `service-account.json` в корень проекта (в `.gitignore` он уже добавлен).

### 4. Настроить вебхук в Битрикс24

В Битрикс24: **Приложения → Вебхуки → Исходящий вебхук**
- Событие: `ONCRMLEAD*` (изменение лида)
- URL обработчика: `https://your-domain/api/webhook`

### 5. Запустить

```bash
dotnet run
```

---

## 📌 TODO / Планы развития

- [ ] Добавить фильтрацию по типу закрытия (успешно / неуспешно)
- [ ] Настроить Docker + docker-compose
- [ ] Добавить логирование через ILogger
- [ ] Обработка ошибок и повторные попытки при недоступности Google API
- [ ] Unit-тесты на логику фильтрации событий

---

## 👨‍💻 Автор

**Никита Королёв** — C#/.NET разработчик

- GitHub: [@nkikita](https://github.com/nkikita)
- Telegram: [@John10Nikolas](https://t.me/John10Nikolas)

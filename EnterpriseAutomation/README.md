# Enterprise Automation

Учебный проект: информационная система для автоматизации работы предприятия. Система ведёт учёт клиентов, заявок и заказов, поддерживает авторизацию, роли, фильтры и отчёты.

## Стек
- Редактор: Visual Studio Code
- Backend: ASP.NET Core MVC (.NET 10)
- Frontend: Razor Views + Bootstrap + CSS
- База данных: Microsoft SQL Server LocalDB / SQL Server Express
- ORM: Entity Framework Core

## Структура проекта
- `Program.cs` - настройка приложения, MVC, cookie-авторизации, подключения к БД и автоматического создания таблиц.
- `appsettings.json` - строка подключения к MSSQL и настройки логирования.
- `Models/` - классы предметной области: клиенты, пользователи, заявки, заказы, справочники статусов.
- `Data/AppDbContext.cs` - EF Core контекст, связи таблиц и тестовые данные.
- `Controllers/` - MVC-контроллеры для страниц: вход, клиенты, заявки, заказы, отчёты.
- `Controllers/Api/` - JSON API для клиентов, заявок, заказов и отчётов.
- `ViewModels/` - модели форм входа, фильтров и отчётов.
- `Views/` - Razor-страницы интерфейса.
- `wwwroot/css/site.css` - стили интерфейса.
- `database/schema-and-seed.sql` - SQL-скрипт создания БД, таблиц, ключей, справочников и тестовых данных.
- `docs/defense-materials.md` - краткий текст для защиты проекта.

## ERD/EDM
Таблицы:
- `Users` - пользователи системы. Поля: ФИО, логин, хэш пароля, роль.
- `Clients` - клиенты предприятия. Поля: ФИО/название, телефон, email, адрес, примечание.
- `RequestStatuses` - справочник статусов заявок: новая, в работе, закрыта.
- `ServiceRequests` - заявки клиентов. Связана с клиентом, статусом и менеджером.
- `OrderPaymentStatuses` - справочник статусов оплаты.
- `OrderExecutionStatuses` - справочник статусов исполнения.
- `Orders` - заказы, созданные на основе заявок.

Связи:
- `Clients 1:N ServiceRequests` - один клиент может иметь много заявок.
- `Users 1:N ServiceRequests` - один менеджер отвечает за много заявок.
- `RequestStatuses 1:N ServiceRequests` - статус используется во многих заявках.
- `ServiceRequests 1:1 Orders` - один заказ создаётся на основе одной заявки.
- `OrderPaymentStatuses 1:N Orders` и `OrderExecutionStatuses 1:N Orders` - статусы нормализованы в справочники.

Такая структура правильна, потому что основные сущности не дублируют данные друг друга, статусы вынесены в справочники, а внешние ключи защищают целостность данных.

## Запуск
1. Установить .NET SDK 10, Visual Studio Code и SQL Server Express.
2. Проверить строку подключения в `appsettings.json`:
   `Server=(localdb)\\MSSQLLocalDB;Database=EnterpriseAutomationDb;Trusted_Connection=True;TrustServerCertificate=True;`
3. В терминале открыть папку проекта:
   ```powershell
   cd EnterpriseAutomation
   dotnet restore
   dotnet run
   ```
4. Открыть адрес из терминала, обычно `https://localhost:7xxx` или `http://localhost:5xxx`.

При первом запуске приложение создаёт базу и заполняет её тестовыми данными. Альтернативно можно выполнить SQL-скрипт `database/schema-and-seed.sql` в SQL Server Management Studio.

## Логины
- Администратор: `admin` / `admin123`
- Менеджер: `manager` / `manager123`
- Менеджер 2: `manager2` / `manager123`

## Реализованные функции
- Авторизация и выход из системы.
- Роли `Administrator` и `Manager`.
- CRUD клиентов.
- CRUD заявок.
- CRUD заказов.
- Поиск, фильтрация и сортировка в списках.
- Отчёты: заказы за период, заявки по статусам, топ клиентов по сумме заказов.
- JSON API:
  - `GET /api/clients`
  - `GET /api/requests`
  - `GET /api/orders`
  - `GET /api/reports/top-clients`

## Примечание для защиты
Проект реалистичный, но специально не перегружен. Он показывает типовую архитектуру: модели, база данных, контроллеры, представления, роли, справочники и отчёты.

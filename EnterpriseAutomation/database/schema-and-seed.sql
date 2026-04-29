CREATE DATABASE EnterpriseAutomationDb;
GO
USE EnterpriseAutomationDb;
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(80) NOT NULL,
    Login NVARCHAR(40) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(120) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Administrator','Manager'))
);

CREATE TABLE Clients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(250) NULL,
    Note NVARCHAR(500) NULL
);

CREATE TABLE RequestStatuses (
    Id INT PRIMARY KEY,
    Name NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE OrderPaymentStatuses (
    Id INT PRIMARY KEY,
    Name NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE OrderExecutionStatuses (
    Id INT PRIMARY KEY,
    Name NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE ServiceRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ClientId INT NOT NULL,
    CreatedAt DATE NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    RequestStatusId INT NOT NULL,
    ManagerId INT NOT NULL,
    CONSTRAINT FK_ServiceRequests_Clients FOREIGN KEY (ClientId) REFERENCES Clients(Id),
    CONSTRAINT FK_ServiceRequests_RequestStatuses FOREIGN KEY (RequestStatusId) REFERENCES RequestStatuses(Id),
    CONSTRAINT FK_ServiceRequests_Users FOREIGN KEY (ManagerId) REFERENCES Users(Id)
);

CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ServiceRequestId INT NOT NULL UNIQUE,
    Services NVARCHAR(1000) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL CHECK (Amount >= 0),
    DueDate DATE NOT NULL,
    PaymentStatusId INT NOT NULL,
    ExecutionStatusId INT NOT NULL,
    CONSTRAINT FK_Orders_ServiceRequests FOREIGN KEY (ServiceRequestId) REFERENCES ServiceRequests(Id),
    CONSTRAINT FK_Orders_PaymentStatuses FOREIGN KEY (PaymentStatusId) REFERENCES OrderPaymentStatuses(Id),
    CONSTRAINT FK_Orders_ExecutionStatuses FOREIGN KEY (ExecutionStatusId) REFERENCES OrderExecutionStatuses(Id)
);

INSERT INTO RequestStatuses (Id, Name) VALUES (1, N'Новая'), (2, N'В работе'), (3, N'Закрыта');
INSERT INTO OrderPaymentStatuses (Id, Name) VALUES (1, N'Не оплачен'), (2, N'Частично оплачен'), (3, N'Оплачен');
INSERT INTO OrderExecutionStatuses (Id, Name) VALUES (1, N'Планируется'), (2, N'Выполняется'), (3, N'Завершён');

-- SHA256: admin123, manager123
INSERT INTO Users (FullName, Login, PasswordHash, Role) VALUES
(N'Администратор системы', N'admin', N'240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'Administrator'),
(N'Иван Петров', N'manager', N'866485796CFA8D7C0CF7111640205B83076433547577511D81F8030AE99ECEA5', N'Manager'),
(N'Анна Соколова', N'manager2', N'866485796CFA8D7C0CF7111640205B83076433547577511D81F8030AE99ECEA5', N'Manager');

INSERT INTO Clients (Name, Phone, Email, Address, Note) VALUES
(N'ООО Альфа', N'+7 900 100-10-10', N'info@alfa.ru', N'Москва, ул. Ленина, 1', N'Постоянный клиент'),
(N'ИП Смирнов П.А.', N'+7 900 100-10-11', N'smirnov@mail.ru', N'Тула, пр. Мира, 15', N'Интересуется сервисным обслуживанием'),
(N'ЗАО Вектор', N'+7 900 100-10-12', N'office@vector.ru', N'Казань, ул. Баумана, 8', N'Оплата по счёту'),
(N'ООО СервисПлюс', N'+7 900 100-10-13', N'hello@serviceplus.ru', N'Самара, ул. Садовая, 3', N'Нужны акты'),
(N'АО Север', N'+7 900 100-10-14', N'contact@sever.ru', N'Санкт-Петербург, Невский пр., 20', N'Крупный заказчик'),
(N'ООО Диалог', N'+7 900 100-10-15', N'dialog@mail.ru', N'Пермь, ул. Советская, 44', N'Связь через email'),
(N'ИП Кузнецова М.В.', N'+7 900 100-10-16', N'kuznetsova@mail.ru', N'Воронеж, ул. Кирова, 9', N'Разовый проект'),
(N'ООО Горизонт', N'+7 900 100-10-17', N'sales@gorizont.ru', N'Екатеринбург, ул. Малышева, 12', N'Нужна консультация'),
(N'АО Прогресс', N'+7 900 100-10-18', N'it@progress.ru', N'Новосибирск, Красный пр., 33', N'Технический заказчик'),
(N'ООО Старт', N'+7 900 100-10-19', N'start@start.ru', N'Ростов-на-Дону, ул. Пушкинская, 5', N'Новый клиент');

INSERT INTO ServiceRequests (ClientId, CreatedAt, Description, RequestStatusId, ManagerId) VALUES
(1, '2026-04-01', N'Настройка CRM и обучение сотрудников', 2, 2),
(2, '2026-04-02', N'Разработка сайта-визитки', 1, 2),
(3, '2026-04-03', N'Автоматизация складского учёта', 3, 3),
(4, '2026-04-04', N'Техническая поддержка рабочих мест', 2, 3),
(5, '2026-04-05', N'Внедрение системы заявок', 3, 2),
(6, '2026-04-06', N'Настройка резервного копирования', 1, 3),
(7, '2026-04-07', N'Консультация по учёту заказов', 1, 2),
(8, '2026-04-08', N'Доработка внутреннего портала', 2, 3),
(9, '2026-04-09', N'Интеграция с бухгалтерией', 3, 2),
(10, '2026-04-10', N'Создание отчётов для руководства', 2, 3);

INSERT INTO Orders (ServiceRequestId, Services, Amount, DueDate, PaymentStatusId, ExecutionStatusId) VALUES
(1, N'Настройка CRM; обучение 10 сотрудников', 85000, '2026-04-20', 2, 2),
(2, N'Проектирование и разработка сайта', 60000, '2026-04-25', 1, 1),
(3, N'Модуль складского учёта', 150000, '2026-04-22', 3, 3),
(4, N'Абонентская поддержка на месяц', 45000, '2026-05-01', 2, 2),
(5, N'Внедрение helpdesk-системы', 120000, '2026-04-28', 3, 3),
(6, N'Настройка backup-сервера', 40000, '2026-04-30', 1, 1),
(7, N'Аналитика процессов и консультация', 25000, '2026-04-18', 3, 3),
(8, N'Доработка портала и тестирование', 95000, '2026-05-05', 2, 2),
(9, N'Интеграция с 1С и обмен данными', 180000, '2026-05-08', 3, 3),
(10, N'Разработка управленческих отчётов', 70000, '2026-05-03', 1, 2);

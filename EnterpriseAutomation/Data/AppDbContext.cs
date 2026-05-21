using System.Security.Claims;
using System.Text.Json;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EnterpriseAutomation.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : this(options, new HttpContextAccessor())
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<RequestStatus> RequestStatuses { get; set; }
        public DbSet<OrderPaymentStatus> OrderPaymentStatuses { get; set; }
        public DbSet<OrderExecutionStatus> OrderExecutionStatuses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AutomationRunLog> AutomationRunLogs { get; set; }

        public override int SaveChanges()
        {
            AddAuditEntries();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddAuditEntries();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AddAuditEntries();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Business rules that must remain true even if data is changed outside MVC forms.
            modelBuilder.Entity<User>().HasIndex(x => x.Login).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(x => x.ServiceRequestId).IsUnique();
            modelBuilder.Entity<AuditLog>().HasIndex(x => x.CreatedAt);
            modelBuilder.Entity<AutomationRunLog>().HasIndex(x => x.StartedAt);

            // Reference data is seeded with stable IDs because forms and demo records depend on them.
            modelBuilder.Entity<RequestStatus>().HasData(
                new RequestStatus { Id = 1, Name = "Новая" },
                new RequestStatus { Id = 2, Name = "В работе" },
                new RequestStatus { Id = 3, Name = "Закрыта" });

            modelBuilder.Entity<OrderPaymentStatus>().HasData(
                new OrderPaymentStatus { Id = 1, Name = "Не оплачен" },
                new OrderPaymentStatus { Id = 2, Name = "Частично оплачен" },
                new OrderPaymentStatus { Id = 3, Name = "Оплачен" });

            modelBuilder.Entity<OrderExecutionStatus>().HasData(
                new OrderExecutionStatus { Id = 1, Name = "Планируется" },
                new OrderExecutionStatus { Id = 2, Name = "Выполняется" },
                new OrderExecutionStatus { Id = 3, Name = "Завершён" });

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "Администратор системы", Login = "admin", PasswordHash = PasswordHasher.Hash("admin123"), Role = UserRoles.Admin },
                new User { Id = 2, FullName = "Иван Петров", Login = "manager", PasswordHash = PasswordHasher.Hash("manager123"), Role = UserRoles.Manager },
                new User { Id = 3, FullName = "Анна Соколова", Login = "manager2", PasswordHash = PasswordHasher.Hash("manager123"), Role = UserRoles.Manager });

            modelBuilder.Entity<Client>().HasData(
                new Client { Id = 1, Name = "ООО Альфа", Phone = "+7 900 100-10-10", Email = "info@alfa.ru", Address = "Москва, ул. Ленина, 1", Note = "Постоянный клиент" },
                new Client { Id = 2, Name = "ИП Смирнов П.А.", Phone = "+7 900 100-10-11", Email = "smirnov@mail.ru", Address = "Тула, пр. Мира, 15", Note = "Интересуется сервисным обслуживанием" },
                new Client { Id = 3, Name = "ЗАО Вектор", Phone = "+7 900 100-10-12", Email = "office@vector.ru", Address = "Казань, ул. Баумана, 8", Note = "Оплата по счёту" },
                new Client { Id = 4, Name = "ООО СервисПлюс", Phone = "+7 900 100-10-13", Email = "hello@serviceplus.ru", Address = "Самара, ул. Садовая, 3", Note = "Нужны акты выполненных работ" },
                new Client { Id = 5, Name = "АО Север", Phone = "+7 900 100-10-14", Email = "contact@sever.ru", Address = "Санкт-Петербург, Невский пр., 20", Note = "Крупный заказчик" },
                new Client { Id = 6, Name = "ООО Диалог", Phone = "+7 900 100-10-15", Email = "dialog@mail.ru", Address = "Пермь, ул. Советская, 44", Note = "Связь через email" },
                new Client { Id = 7, Name = "ИП Кузнецова М.В.", Phone = "+7 900 100-10-16", Email = "kuznetsova@mail.ru", Address = "Воронеж, ул. Кирова, 9", Note = "Разовый проект" },
                new Client { Id = 8, Name = "ООО Горизонт", Phone = "+7 900 100-10-17", Email = "sales@gorizont.ru", Address = "Екатеринбург, ул. Малышева, 12", Note = "Нужна консультация" },
                new Client { Id = 9, Name = "АО Прогресс", Phone = "+7 900 100-10-18", Email = "it@progress.ru", Address = "Новосибирск, Красный пр., 33", Note = "Технический заказчик" },
                new Client { Id = 10, Name = "ООО Старт", Phone = "+7 900 100-10-19", Email = "start@start.ru", Address = "Ростов-на-Дону, ул. Пушкинская, 5", Note = "Новый клиент" });

            modelBuilder.Entity<ServiceRequest>().HasData(
                new ServiceRequest { Id = 1, ClientId = 1, CreatedAt = new DateTime(2026, 4, 1), Description = "Настройка CRM и обучение сотрудников", RequestStatusId = 2, ManagerId = 2 },
                new ServiceRequest { Id = 2, ClientId = 2, CreatedAt = new DateTime(2026, 4, 2), Description = "Разработка сайта-визитки", RequestStatusId = 1, ManagerId = 2 },
                new ServiceRequest { Id = 3, ClientId = 3, CreatedAt = new DateTime(2026, 4, 3), Description = "Автоматизация складского учёта", RequestStatusId = 3, ManagerId = 3 },
                new ServiceRequest { Id = 4, ClientId = 4, CreatedAt = new DateTime(2026, 4, 4), Description = "Техническая поддержка рабочих мест", RequestStatusId = 2, ManagerId = 3 },
                new ServiceRequest { Id = 5, ClientId = 5, CreatedAt = new DateTime(2026, 4, 5), Description = "Внедрение системы заявок", RequestStatusId = 3, ManagerId = 2 },
                new ServiceRequest { Id = 6, ClientId = 6, CreatedAt = new DateTime(2026, 4, 6), Description = "Настройка резервного копирования", RequestStatusId = 1, ManagerId = 3 },
                new ServiceRequest { Id = 7, ClientId = 7, CreatedAt = new DateTime(2026, 4, 7), Description = "Консультация по учёту заказов", RequestStatusId = 1, ManagerId = 2 },
                new ServiceRequest { Id = 8, ClientId = 8, CreatedAt = new DateTime(2026, 4, 8), Description = "Доработка внутреннего портала", RequestStatusId = 2, ManagerId = 3 },
                new ServiceRequest { Id = 9, ClientId = 9, CreatedAt = new DateTime(2026, 4, 9), Description = "Интеграция с бухгалтерией", RequestStatusId = 3, ManagerId = 2 },
                new ServiceRequest { Id = 10, ClientId = 10, CreatedAt = new DateTime(2026, 4, 10), Description = "Создание отчётов для руководства", RequestStatusId = 2, ManagerId = 3 });

            modelBuilder.Entity<Order>().HasData(
                new Order { Id = 1, ServiceRequestId = 1, Services = "Настройка CRM; обучение 10 сотрудников", Amount = 85000, DueDate = new DateTime(2026, 4, 20), PaymentStatusId = 2, ExecutionStatusId = 2 },
                new Order { Id = 2, ServiceRequestId = 2, Services = "Проектирование и разработка сайта", Amount = 60000, DueDate = new DateTime(2026, 4, 25), PaymentStatusId = 1, ExecutionStatusId = 1 },
                new Order { Id = 3, ServiceRequestId = 3, Services = "Модуль складского учёта", Amount = 150000, DueDate = new DateTime(2026, 4, 22), PaymentStatusId = 3, ExecutionStatusId = 3 },
                new Order { Id = 4, ServiceRequestId = 4, Services = "Абонентская поддержка на месяц", Amount = 45000, DueDate = new DateTime(2026, 5, 1), PaymentStatusId = 2, ExecutionStatusId = 2 },
                new Order { Id = 5, ServiceRequestId = 5, Services = "Внедрение helpdesk-системы", Amount = 120000, DueDate = new DateTime(2026, 4, 28), PaymentStatusId = 3, ExecutionStatusId = 3 },
                new Order { Id = 6, ServiceRequestId = 6, Services = "Настройка backup-сервера", Amount = 40000, DueDate = new DateTime(2026, 4, 30), PaymentStatusId = 1, ExecutionStatusId = 1 },
                new Order { Id = 7, ServiceRequestId = 7, Services = "Аналитика процессов и консультация", Amount = 25000, DueDate = new DateTime(2026, 4, 18), PaymentStatusId = 3, ExecutionStatusId = 3 },
                new Order { Id = 8, ServiceRequestId = 8, Services = "Доработка портала и тестирование", Amount = 95000, DueDate = new DateTime(2026, 5, 5), PaymentStatusId = 2, ExecutionStatusId = 2 },
                new Order { Id = 9, ServiceRequestId = 9, Services = "Интеграция с 1С и обмен данными", Amount = 180000, DueDate = new DateTime(2026, 5, 8), PaymentStatusId = 3, ExecutionStatusId = 3 },
                new Order { Id = 10, ServiceRequestId = 10, Services = "Разработка управленческих отчётов", Amount = 70000, DueDate = new DateTime(2026, 5, 3), PaymentStatusId = 1, ExecutionStatusId = 2 });
        }

        private void AddAuditEntries()
        {
            ChangeTracker.DetectChanges();

            var entries = ChangeTracker.Entries()
                .Where(entry =>
                    entry.Entity is not AuditLog &&
                    entry.Entity is not AutomationRunLog &&
                    entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(BuildAuditLog)
                .ToList();

            if (entries.Count > 0)
            {
                AuditLogs.AddRange(entries);
            }
        }

        private AuditLog BuildAuditLog(EntityEntry entry)
        {
            var context = _httpContextAccessor.HttpContext;
            var actor = context?.User?.Identity?.Name ?? "system";
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Changed"
            };

            return new AuditLog
            {
                CreatedAt = DateTime.UtcNow,
                Actor = actor,
                ActorRole = context?.User?.FindFirstValue(ClaimTypes.Role),
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityKey = GetPrimaryKey(entry),
                BeforeJson = entry.State is EntityState.Added ? null : SerializeValues(entry, original: true),
                AfterJson = entry.State is EntityState.Deleted ? null : SerializeValues(entry, original: false),
                IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = context?.TraceIdentifier
            };
        }

        private static string? GetPrimaryKey(EntityEntry entry)
        {
            var key = entry.Properties.FirstOrDefault(property => property.Metadata.IsPrimaryKey());
            return key == null ? null : Convert.ToString(key.CurrentValue ?? key.OriginalValue);
        }

        private static string SerializeValues(EntityEntry entry, bool original)
        {
            var values = entry.Properties.ToDictionary(
                property => property.Metadata.Name,
                property => SanitizeValue(property.Metadata.Name, original ? property.OriginalValue : property.CurrentValue));

            return JsonSerializer.Serialize(values);
        }

        private static object? SanitizeValue(string propertyName, object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (propertyName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase))
            {
                return "[redacted]";
            }

            return value;
        }
    }
}

using EnterpriseAutomation.Models;
using EnterpriseAutomation.ViewModels;

namespace EnterpriseAutomation.Services.Dashboard;

public static class DashboardAttentionBuilder
{
    public static IReadOnlyList<DashboardAttentionItem> Build(
        IReadOnlyList<ServiceRequest> requests,
        IReadOnlyList<Order> orders,
        DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? DateTime.Today).Date;
        var candidates = new List<AttentionCandidate>();

        candidates.AddRange(orders
            .Where(order => order.ExecutionStatusId != 3 && order.DueDate.Date < today)
            .Select(order => new AttentionCandidate(
                $"order:{order.Id}",
                1,
                order.DueDate.Date,
                BuildOrderItem(
                    order,
                    $"Просрочен заказ #{order.Id}",
                    "danger",
                    $"{(today - order.DueDate.Date).Days} дн. просрочки",
                    "Открыть заказ"))));

        candidates.AddRange(orders
            .Where(order => order.ExecutionStatusId != 3 && order.DueDate.Date >= today && order.DueDate.Date <= today.AddDays(3))
            .Select(order => new AttentionCandidate(
                $"order:{order.Id}",
                2,
                order.DueDate.Date,
                BuildOrderItem(
                    order,
                    $"Скоро срок заказа #{order.Id}",
                    "warning",
                    BuildDueBadge(order.DueDate, today),
                    "Открыть заказ"))));

        candidates.AddRange(orders
            .Where(order => order.PaymentStatusId != 3)
            .Select(order => new AttentionCandidate(
                $"order:{order.Id}",
                3,
                order.DueDate.Date,
                new DashboardAttentionItem(
                    $"Проверить оплату заказа #{order.Id}",
                    $"{GetClientName(order)}: {order.Amount:N0} ₽, {order.PaymentStatus?.Name ?? "статус оплаты не указан"}",
                    "info",
                    "к оплате",
                    "Открыть заказ",
                    $"/Orders/Edit/{order.Id}"))));

        candidates.AddRange(requests
            .Where(request => request.RequestStatusId != 3 && request.CreatedAt.Date <= today.AddDays(-7))
            .Select(request => new AttentionCandidate(
                $"request:{request.Id}",
                4,
                request.CreatedAt.Date,
                new DashboardAttentionItem(
                    $"Заявка #{request.Id} без закрытия",
                    $"{request.Client?.Name ?? "Клиент не указан"}: {Truncate(request.Description, 84)}",
                    request.RequestStatusId == 1 ? "warning" : "info",
                    $"{(today - request.CreatedAt.Date).Days} дн. в работе",
                    "Открыть заявку",
                    $"/Requests/Edit/{request.Id}"))));

        return candidates
            .OrderBy(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.SortDate)
            .GroupBy(candidate => candidate.Key)
            .Select(group => group.First().Item)
            .Take(6)
            .ToList();
    }

    private static DashboardAttentionItem BuildOrderItem(
        Order order,
        string title,
        string tone,
        string badge,
        string actionLabel)
    {
        return new DashboardAttentionItem(
            title,
            $"{GetClientName(order)}: {Truncate(order.Services, 84)}",
            tone,
            badge,
            actionLabel,
            $"/Orders/Edit/{order.Id}");
    }

    private static string GetClientName(Order order)
    {
        return order.ServiceRequest?.Client?.Name ?? "Клиент не указан";
    }

    private static string BuildDueBadge(DateTime dueDate, DateTime today)
    {
        var days = (dueDate.Date - today.Date).Days;

        return days switch
        {
            0 => "сегодня",
            1 => "завтра",
            _ => $"{days} дн."
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 1)].TrimEnd() + "…";
    }

    private sealed record AttentionCandidate(
        string Key,
        int Priority,
        DateTime SortDate,
        DashboardAttentionItem Item);
}

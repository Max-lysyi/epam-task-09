namespace EpamPreOrderApi.Models;


public class Dish
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class OrderItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DishId { get; set; } = string.Empty;
    public Dish Dish { get; set; } = null!;
    public int Quantity { get; set; }
}

public class GuestSession
{
    public string Id { get; set; } = string.Empty;
    public string GroupReservationId { get; set; } = string.Empty;
    public string? ReservationId { get; set; }
    public string Type { get; set; } = "Guest"; // "Guest" або "Owner"
    public string Name { get; set; } = string.Empty;
    public bool IsSubmitted { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public List<OrderItem> Items { get; set; } = new();
}

public class GroupReservation
{
    public string Id { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? OrderId { get; set; }
    public Order? Order { get; set; }
    public List<GuestSession> Sessions { get; set; } = new();
}

// --- DTOs (Формати відповідей і запитів за вимогами) ---
public class GroupSessionsResponse
{
    public List<ParticipantDto> Participants { get; set; } = new();
    public decimal Total { get; set; }
}

public class ParticipantDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
    public bool IsSubmitted { get; set; }
}

public class ItemDto
{
    public DishDto Dish { get; set; } = null!;
    public int Quantity { get; set; }
}

public class DishDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SubmitPreOrderRequest
{
    public string ReservationId { get; set; } = string.Empty;
}

public class ConsolidatedDishDto
{
    public string DishId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CreateTestGroupRequest
{
    public string? GroupReservationId { get; set; }
    public string? ReservationId { get; set; }
    public string? Status { get; set; }
    public string? OrderId { get; set; }
    public bool CreateOrder { get; set; } = true;
}

public class AddTestSessionRequest
{
    public string? Id { get; set; }
    public string? ReservationId { get; set; }
    public string Type { get; set; } = "Guest";
    public string Name { get; set; } = string.Empty;
    public bool IsSubmitted { get; set; }
    public List<TestOrderItemRequest> Items { get; set; } = new();
}

public class TestOrderItemRequest
{
    public string DishId { get; set; } = string.Empty;
    public string? DishName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

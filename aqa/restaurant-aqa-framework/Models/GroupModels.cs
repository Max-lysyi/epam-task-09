using System.Text.Json.Serialization;

namespace RestaurantAqaFramework.Models;

public class GroupSessionsResponse
{
    [JsonPropertyName("participants")]
    public List<ParticipantDto> Participants { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

public class ParticipantDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<ItemDto> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("isSubmitted")]
    public bool IsSubmitted { get; set; }
}

public class ItemDto
{
    [JsonPropertyName("dish")]
    public DishDto Dish { get; set; } = new();

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class DishDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

public class SubmitPreOrderRequest
{
    [JsonPropertyName("reservationId")]
    public string ReservationId { get; set; } = string.Empty;
}

public class ConsolidatedDishDto
{
    [JsonPropertyName("dishId")]
    public string DishId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class CreateTestGroupRequest
{
    [JsonPropertyName("groupReservationId")]
    public string? GroupReservationId { get; set; }

    [JsonPropertyName("reservationId")]
    public string? ReservationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("createOrder")]
    public bool CreateOrder { get; set; } = true;
}

public class AddTestSessionRequest
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("reservationId")]
    public string? ReservationId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Guest";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isSubmitted")]
    public bool IsSubmitted { get; set; }

    [JsonPropertyName("items")]
    public List<TestOrderItemRequest> Items { get; set; } = new();
}

public class TestOrderItemRequest
{
    [JsonPropertyName("dishId")]
    public string DishId { get; set; } = string.Empty;

    [JsonPropertyName("dishName")]
    public string? DishName { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class WaiterOrderDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<WaiterOrderItemDto> Items { get; set; } = new();
}

public class WaiterOrderItemDto
{
    [JsonPropertyName("dishId")]
    public string DishId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}


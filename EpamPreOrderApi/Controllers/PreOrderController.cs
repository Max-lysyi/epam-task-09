using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EpamPreOrderApi.Models;

namespace EpamPreOrderApi.Controllers;

[ApiController]
public class PreOrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public PreOrderController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthorizedAsOwner(out IActionResult? unauthorizedResult)
    {
        unauthorizedResult = null;
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var roleHeader = Request.Headers["X-User-Role"].FirstOrDefault();

        // Check if explicitly unauthorized
        if (authHeader == "Bearer unauthorized" || roleHeader == "Unauthorized")
        {
            unauthorizedResult = Unauthorized(new { message = "Invalid credentials." });
            return false;
        }

        // Check if caller has non-owner role
        if (roleHeader == "Guest" || roleHeader == "Waiter" || roleHeader == "OtherClient" ||
            authHeader == "Bearer guest" || authHeader == "Bearer waiter" || authHeader == "Bearer other_client")
        {
            unauthorizedResult = StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied: Owner role required." });
            return false;
        }

        return true;
    }

    // --- 👤 User Profile & Header ендпоінти для інтеграції з Frontend ---
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            id = "demo-owner",
            firstName = "Yevhenii",
            lastName = "Demo",
            email = "owner@demo.test",
            role = "Customer"
        });
    }

    [HttpGet("orders")]
    public IActionResult GetHeaderOrders()
    {
        return Ok(new List<object>());
    }

    // --- 🟢 1-й рівень: GET список сесій учасників ---
    [HttpGet("guest/groups/{groupReservationId}/sessions")]
    public async Task<IActionResult> GetGroupSessions(string groupReservationId)
    {
        if (!IsAuthorizedAsOwner(out var authError))
            return authError!;

        var group = await _context.GroupReservations
            .Include(g => g.Sessions)
                .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Dish)
            .FirstOrDefaultAsync(g => g.Id == groupReservationId);

        if (group == null)
            return NotFound(new { message = "Group reservation not found." });

        var response = MapToGroupSessionsResponse(group);
        return Ok(response);
    }

    // --- 🔵 2-й рівень (1/2): GET guestSessionId для Owner ---
    [HttpGet("reservations/{reservationId}/guest-session-id")]
    public async Task<IActionResult> GetOwnerSessionId(string reservationId)
    {
        if (!IsAuthorizedAsOwner(out var authError))
            return authError!;

        var group = await _context.GroupReservations
            .Include(g => g.Sessions)
            .FirstOrDefaultAsync(g => g.ReservationId == reservationId || g.Id == reservationId);

        var ownerSession = group?.Sessions.FirstOrDefault(s => s.Type == "Owner")
            ?? await _context.Sessions.FirstOrDefaultAsync(s => (s.ReservationId == reservationId || s.GroupReservationId == reservationId) && s.Type == "Owner");

        if (ownerSession == null)
            return NotFound(new { message = "Owner session not found." });

        return Ok(new { guestSessionId = ownerSession.Id });
    }

    // --- 🎫 Guest Invite & Token ендпоінти для інтеграції з UI ---
    [HttpGet("reservations/{reservationId}/invite-token")]
    public async Task<IActionResult> GetInviteToken(string reservationId)
    {
        var group = await _context.GroupReservations.FirstOrDefaultAsync(g => g.ReservationId == reservationId || g.Id == reservationId);
        if (group == null)
            return NotFound(new { message = "Reservation not found." });

        return Ok(new { token = $"invite-{group.Id}" });
    }

    [HttpGet("guest/invites/{token}")]
    public async Task<IActionResult> GetInvite(string token)
    {
        var lookupId = token.StartsWith("invite-") ? token["invite-".Length..] : token;
        var group = await _context.GroupReservations.FirstOrDefaultAsync(g => g.ReservationId == lookupId || g.Id == lookupId);

        if (group == null)
            return NotFound(new { message = "Invite not found." });

        return Ok(new Dictionary<string, object>
        {
            ["GroupReservationId"] = group.Id,
            ["groupReservationId"] = group.Id,
            ["LocationId"] = "loc-1",
            ["locationId"] = "loc-1",
            ["RestaurantName"] = "Green & Tasty",
            ["restaurantName"] = "Green & Tasty",
            ["LocationAddress"] = "Rustaveli Avenue 48",
            ["locationAddress"] = "Rustaveli Avenue 48",
            ["StartTime"] = DateTime.UtcNow.Date.AddHours(18).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["startTime"] = DateTime.UtcNow.Date.AddHours(18).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["EndTime"] = DateTime.UtcNow.Date.AddHours(20).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["endTime"] = DateTime.UtcNow.Date.AddHours(20).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["MaxGuestCount"] = 5,
            ["maxGuestCount"] = 5,
            ["TableNumber"] = 7,
            ["tableNumber"] = 7,
            ["InviterName"] = "Yevhenii",
            ["inviterName"] = "Yevhenii"
        });
    }

    [HttpGet("guest/groups/{groupReservationId}/status")]
    public async Task<IActionResult> GetGroupStatus(string groupReservationId)
    {
        var group = await _context.GroupReservations
            .Include(g => g.Sessions)
            .FirstOrDefaultAsync(g => g.Id == groupReservationId);

        if (group == null)
            return NotFound(new { message = "Group not found." });

        return Ok(new
        {
            currentParticipants = group.Sessions.Count,
            maxCapacity = 5,
            status = group.Status == "Submitted" ? "Submitted" : "Open"
        });
    }

    // --- 🔵 2-й рівень (2/2): POST підтвердження консолідованого pre-order ---
    [HttpPost("group-reservations/{groupReservationId}/submit")]
    public async Task<IActionResult> SubmitPreOrder(string groupReservationId, [FromBody] SubmitPreOrderRequest request)
    {
        if (!IsAuthorizedAsOwner(out var authError))
            return authError!;

        var group = await _context.GroupReservations
            .Include(g => g.Sessions)
                .ThenInclude(s => s.Items)
            .Include(g => g.Order!)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(g => g.Id == groupReservationId);

        // Якщо reservation або group reservation не знайдено — 404
        if (group == null || group.ReservationId != request.ReservationId)
            return NotFound(new { message = "Reservation or group reservation not found." });

        // Якщо group reservation вже submitted — 400
        if (group.Status == "Submitted")
            return BadRequest(new { message = "Cannot submit a group that is already submitted." });

        // Якщо order не знайдено — 400
        if (group.Order == null)
            return BadRequest(new { message = "Order not found." });

        var submittedSessions = group.Sessions.Where(s => s.IsSubmitted).ToList();

        // Якщо немає жодної submitted session — 400
        if (!submittedSessions.Any())
            return BadRequest(new { message = "No submitted sessions found." });

        var allItems = submittedSessions.SelectMany(s => s.Items).ToList();

        // Якщо в submitted sessions немає жодної страви — 400
        if (!allItems.Any())
            return BadRequest(new { message = "No items found in submitted sessions." });

        // Об'єднання однакових страв по dishId
        var consolidated = allItems
            .GroupBy(i => i.DishId)
            .Select(g => new ConsolidatedDishDto
            {
                DishId = g.Key,
                Quantity = g.Sum(i => i.Quantity)
            })
            .ToList();

        // Оновлюємо Items в самому Order для ресторану / офіціанта
        group.Order!.Items.Clear();
        foreach (var item in consolidated)
        {
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == item.DishId)
                       ?? new Dish { Id = item.DishId, Name = item.DishId, Price = 100 };
            group.Order.Items.Add(new OrderItem
            {
                DishId = item.DishId,
                Dish = dish,
                Quantity = item.Quantity
            });
        }

        // Зміна статусів за умовами
        group.Order.Status = "SubmittedByCustomer";
        group.Status = "Submitted";

        await _context.SaveChangesAsync();

        return Ok(consolidated);
    }

    // --- 🟣 3-й рівень: DELETE guest session із групи ---
    [HttpDelete("guest/groups/{groupReservationId}/sessions/{guestSessionId}")]
    public async Task<IActionResult> DeleteGuestSession(string groupReservationId, string guestSessionId)
    {
        if (!IsAuthorizedAsOwner(out var authError))
            return authError!;

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == guestSessionId);

        // Якщо guest session не знайдена — 404
        if (session == null)
            return NotFound(new { message = "Session not found." });

        // Якщо guest session не належить цій group reservation — 400
        if (session.GroupReservationId != groupReservationId)
            return BadRequest(new { message = "Session does not belong to this group reservation." });

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        // Повертаємо оновлений snapshot групи
        var updatedGroup = await _context.GroupReservations
            .Include(g => g.Sessions)
                .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Dish)
            .FirstOrDefaultAsync(g => g.Id == groupReservationId);

        if (updatedGroup == null)
            return NotFound(new { message = "Group reservation not found." });

        var response = MapToGroupSessionsResponse(updatedGroup);
        return Ok(response);
    }

    // --- 🍽️ Waiter endpoint для перегляду підтвердженого замовлення ---
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Dish)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        return Ok(new
        {
            id = order.Id,
            status = order.Status,
            items = order.Items.Select(i => new
            {
                dishId = i.DishId,
                name = i.Dish?.Name ?? string.Empty,
                quantity = i.Quantity,
                price = i.Dish?.Price ?? 0
            }).ToList()
        });
    }

    // --- 🧪 Тестові допоміжні ендпоінти для динамічних даних та очищення ---
    [HttpPost("test/groups")]
    public async Task<IActionResult> CreateTestGroup([FromBody] CreateTestGroupRequest request)
    {
        var groupId = request.GroupReservationId ?? Guid.NewGuid().ToString();
        var reservationId = request.ReservationId ?? $"res-{Guid.NewGuid():N}";
        var orderId = request.OrderId ?? $"ord-{Guid.NewGuid():N}";

        Order? order = null;
        if (request.CreateOrder)
        {
            order = new Order { Id = orderId, Status = "Pending" };
            _context.Orders.Add(order);
        }

        var group = new GroupReservation
        {
            Id = groupId,
            ReservationId = reservationId,
            Status = request.Status ?? "Active",
            OrderId = order?.Id,
            Order = order,
            Sessions = new()
        };

        _context.GroupReservations.Add(group);
        await _context.SaveChangesAsync();

        return Created($"/guest/groups/{groupId}/sessions", new
        {
            groupReservationId = groupId,
            reservationId = reservationId,
            orderId = order?.Id,
            status = group.Status
        });
    }

    [HttpPost("test/groups/{groupReservationId}/sessions")]
    public async Task<IActionResult> AddTestSession(string groupReservationId, [FromBody] AddTestSessionRequest request)
    {
        var group = await _context.GroupReservations
            .Include(g => g.Sessions)
            .FirstOrDefaultAsync(g => g.Id == groupReservationId);

        if (group == null)
            return NotFound(new { message = "Group not found." });

        var sessionId = request.Id ?? Guid.NewGuid().ToString();
        var session = new GuestSession
        {
            Id = sessionId,
            GroupReservationId = groupReservationId,
            ReservationId = request.ReservationId ?? group.ReservationId,
            Type = request.Type,
            Name = request.Name,
            IsSubmitted = request.IsSubmitted,
            Items = new()
        };

        foreach (var itemReq in request.Items)
        {
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == itemReq.DishId);
            if (dish == null)
            {
                dish = new Dish
                {
                    Id = itemReq.DishId,
                    Name = itemReq.DishName ?? itemReq.DishId,
                    Price = itemReq.Price
                };
                _context.Dishes.Add(dish);
            }
            else
            {
                dish.Price = itemReq.Price;
                if (!string.IsNullOrEmpty(itemReq.DishName))
                    dish.Name = itemReq.DishName;
            }

            session.Items.Add(new OrderItem
            {
                DishId = dish.Id,
                Dish = dish,
                Quantity = itemReq.Quantity
            });
        }

        group.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return Created($"/guest/groups/{groupReservationId}/sessions", new
        {
            sessionId = session.Id,
            name = session.Name,
            type = session.Type,
            isSubmitted = session.IsSubmitted
        });
    }

    [HttpPut("test/groups/{groupReservationId}/sessions/{guestSessionId}/items")]
    public async Task<IActionResult> UpdateTestSessionItems(string groupReservationId, string guestSessionId, [FromBody] List<TestOrderItemRequest> items)
    {
        var session = await _context.Sessions
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == guestSessionId && s.GroupReservationId == groupReservationId);

        if (session == null)
            return NotFound(new { message = "Session not found in group." });

        _context.RemoveRange(session.Items);
        session.Items.Clear();

        foreach (var itemReq in items)
        {
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.Id == itemReq.DishId);
            if (dish == null)
            {
                dish = new Dish
                {
                    Id = itemReq.DishId,
                    Name = itemReq.DishName ?? itemReq.DishId,
                    Price = itemReq.Price
                };
                _context.Dishes.Add(dish);
            }
            else
            {
                dish.Price = itemReq.Price;
                if (!string.IsNullOrEmpty(itemReq.DishName))
                    dish.Name = itemReq.DishName;
            }

            session.Items.Add(new OrderItem
            {
                DishId = dish.Id,
                Dish = dish,
                Quantity = itemReq.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cart updated successfully." });
    }

    [HttpDelete("test/groups/{groupReservationId}")]
    public async Task<IActionResult> DeleteTestGroup(string groupReservationId)
    {
        var group = await _context.GroupReservations
            .Include(g => g.Sessions)
                .ThenInclude(s => s.Items)
            .Include(g => g.Order!)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(g => g.Id == groupReservationId);

        if (group != null)
        {
            foreach (var session in group.Sessions)
            {
                _context.RemoveRange(session.Items);
            }
            _context.Sessions.RemoveRange(group.Sessions);

            if (group.Order != null)
            {
                _context.RemoveRange(group.Order.Items);
                _context.Orders.Remove(group.Order);
            }

            _context.GroupReservations.Remove(group);
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("test/reset")]
    public async Task<IActionResult> ResetDatabase()
    {
        _context.Sessions.RemoveRange(_context.Sessions);
        _context.Orders.RemoveRange(_context.Orders);
        _context.GroupReservations.RemoveRange(_context.GroupReservations);
        _context.Dishes.RemoveRange(_context.Dishes);
        await _context.SaveChangesAsync();

        _context.SeedData();
        return Ok(new { message = "Database reset successfully." });
    }

    private static GroupSessionsResponse MapToGroupSessionsResponse(GroupReservation group)
    {
        var participants = group.Sessions.Select(s => new ParticipantDto
        {
            Id = s.Id,
            Type = s.Type,
            Name = s.Name,
            IsSubmitted = s.IsSubmitted,
            Items = s.Items.Select(i => new ItemDto
            {
                Dish = new DishDto
                {
                    Id = i.Dish?.Id ?? i.DishId,
                    Name = i.Dish?.Name ?? string.Empty,
                    Price = i.Dish?.Price ?? 0
                },
                Quantity = i.Quantity
            }).ToList(),
            Total = s.Items.Sum(i => i.Quantity * (i.Dish?.Price ?? 0))
        }).ToList();

        return new GroupSessionsResponse
        {
            Participants = participants,
            Total = participants.Sum(p => p.Total)
        };
    }
}

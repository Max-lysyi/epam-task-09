using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Actors;
using RestaurantAqaFramework.TestData;

namespace RestaurantAqaFramework.Screenplay.Tasks;

public class FetchGroupSessions : IPerformable
{
    private readonly string _groupReservationId;
    private readonly string? _role;
    private readonly string? _token;

    public FetchGroupSessions(string groupReservationId, string? role = "Owner", string? token = null)
    {
        _groupReservationId = groupReservationId;
        _role = role;
        _token = token;
    }

    public static FetchGroupSessions ForGroup(string groupId, string? role = "Owner", string? token = null) =>
        new(groupId, role, token);

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = BuildHeaders(_role, _token);
        await api.GetAsync($"guest/groups/{_groupReservationId}/sessions", headers);
    }

    private static Dictionary<string, string> BuildHeaders(string? role, string? token)
    {
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(role))
        {
            headers["X-User-Role"] = role;
        }

        if (!string.IsNullOrEmpty(token))
        {
            headers["Authorization"] = $"Bearer {token}";
        }
        else if (role != null)
        {
            headers["Authorization"] = $"Bearer {role.ToLower()}";
        }

        return headers;
    }
}

public class FetchOwnerSessionId : IPerformable
{
    private readonly string _reservationId;
    private readonly string? _role;

    public FetchOwnerSessionId(string reservationId, string? role = "Owner")
    {
        _reservationId = reservationId;
        _role = role;
    }

    public static FetchOwnerSessionId ForReservation(string reservationId, string? role = "Owner") =>
        new(reservationId, role);

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(_role))
        {
            headers["X-User-Role"] = _role;
            headers["Authorization"] = $"Bearer {_role.ToLower()}";
        }

        await api.GetAsync($"reservations/{_reservationId}/guest-session-id", headers);
    }
}

public class SubmitGroupPreOrderViaApi : IPerformable
{
    private readonly string _groupReservationId;
    private readonly string _reservationId;
    private readonly string? _role;

    public SubmitGroupPreOrderViaApi(string groupReservationId, string reservationId, string? role = "Owner")
    {
        _groupReservationId = groupReservationId;
        _reservationId = reservationId;
        _role = role;
    }

    public static SubmitGroupPreOrderViaApi ForGroup(string groupId, string reservationId, string? role = "Owner") =>
        new(groupId, reservationId, role);

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(_role))
        {
            headers["X-User-Role"] = _role;
            headers["Authorization"] = $"Bearer {_role.ToLower()}";
        }

        var requestBody = new SubmitPreOrderRequest { ReservationId = _reservationId };
        await api.PostAsync($"group-reservations/{_groupReservationId}/submit", requestBody, headers);
    }
}

public class RemoveGuestSessionViaApi : IPerformable
{
    private readonly string _groupReservationId;
    private readonly string _guestSessionId;
    private readonly string? _role;

    public RemoveGuestSessionViaApi(string groupReservationId, string guestSessionId, string? role = "Owner")
    {
        _groupReservationId = groupReservationId;
        _guestSessionId = guestSessionId;
        _role = role;
    }

    public static RemoveGuestSessionViaApi For(string groupId, string sessionId, string? role = "Owner") =>
        new(groupId, sessionId, role);

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(_role))
        {
            headers["X-User-Role"] = _role;
            headers["Authorization"] = $"Bearer {_role.ToLower()}";
        }

        await api.DeleteAsync($"guest/groups/{_groupReservationId}/sessions/{_guestSessionId}", headers);
    }
}

public class SetupTestGroupData : IPerformable
{
    private readonly CreateTestGroupRequest _groupRequest;
    private readonly List<AddTestSessionRequest> _sessions = new();

    public SetupTestGroupData(string groupId, string reservationId, string? orderId = null, bool createOrder = true)
    {
        _groupRequest = new CreateTestGroupRequest
        {
            GroupReservationId = groupId,
            ReservationId = reservationId,
            OrderId = orderId,
            CreateOrder = createOrder
        };
    }

    public static SetupTestGroupData WithId(string groupId, string reservationId, string? orderId = null, bool createOrder = true) =>
        new(groupId, reservationId, orderId, createOrder);

    public SetupTestGroupData AddSession(string sessionId, string name, string type, bool isSubmitted, List<TestOrderItemRequest> items)
    {
        _sessions.Add(new AddTestSessionRequest
        {
            Id = sessionId,
            Name = name,
            Type = type,
            IsSubmitted = isSubmitted,
            Items = items
        });
        return this;
    }

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer owner",
            ["X-User-Role"] = "Owner"
        };

        // Create group
        await api.PostAsync("test/groups", _groupRequest, headers);
        TestDataManager.RegisterGroup(_groupRequest.GroupReservationId!);

        // Add sessions
        foreach (var session in _sessions)
        {
            await api.PostAsync($"test/groups/{_groupRequest.GroupReservationId}/sessions", session, headers);
        }
    }
}

public class UpdateGuestCartViaApi : IPerformable
{
    private readonly string _groupId;
    private readonly string _sessionId;
    private readonly List<TestOrderItemRequest> _items;

    public UpdateGuestCartViaApi(string groupId, string sessionId, List<TestOrderItemRequest> items)
    {
        _groupId = groupId;
        _sessionId = sessionId;
        _items = items;
    }

    public static UpdateGuestCartViaApi For(string groupId, string sessionId, List<TestOrderItemRequest> items) =>
        new(groupId, sessionId, items);

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer owner",
            ["X-User-Role"] = "Owner"
        };

        await api.PutAsync($"test/groups/{_groupId}/sessions/{_sessionId}/items", _items, headers);
    }
}

public class VerifyWaiterOrderViaApi : IPerformable
{
    private readonly string _orderId;

    public VerifyWaiterOrderViaApi(string orderId)
    {
        _orderId = orderId;
    }

    public static VerifyWaiterOrderViaApi ForOrder(string orderId) => new(orderId);

    public async Task PerformAs(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer waiter",
            ["X-User-Role"] = "Waiter"
        };

        await api.GetAsync($"orders/{_orderId}", headers);
    }
}


using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Actors;

namespace RestaurantAqaFramework.Screenplay.Questions;

public class LastApiStatusCode : IQuestion<int?>
{
    public static LastApiStatusCode Value() => new();

    public Task<int?> AnsweredBy(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        return Task.FromResult(api.LastStatusCode);
    }
}

public class LastApiResponseContent : IQuestion<string?>
{
    public static LastApiResponseContent Body() => new();

    public Task<string?> AnsweredBy(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        return Task.FromResult(api.LastResponseBody);
    }
}

public class GroupSessionsData : IQuestion<GroupSessionsResponse?>
{
    public static GroupSessionsData Current() => new();

    public Task<GroupSessionsResponse?> AnsweredBy(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        return Task.FromResult(api.DeserializeLastResponse<GroupSessionsResponse>());
    }
}

public class ConsolidatedDishesData : IQuestion<List<ConsolidatedDishDto>?>
{
    public static ConsolidatedDishesData Current() => new();

    public Task<List<ConsolidatedDishDto>?> AnsweredBy(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        return Task.FromResult(api.DeserializeLastResponse<List<ConsolidatedDishDto>>());
    }
}

public class WaiterOrderData : IQuestion<WaiterOrderDto?>
{
    public static WaiterOrderData Current() => new();

    public Task<WaiterOrderDto?> AnsweredBy(IActor actor)
    {
        var api = actor.Using<CallAnApi>();
        return Task.FromResult(api.DeserializeLastResponse<WaiterOrderDto>());
    }
}


using System.Collections.Concurrent;
using Microsoft.Playwright;
using RestaurantAqaFramework.Configuration;
using RestaurantAqaFramework.Screenplay.Abilities;

namespace RestaurantAqaFramework.Screenplay.Actors;

public class Cast
{
    private readonly IPage? _page;
    private readonly IBrowserContext? _context;
    private readonly IAPIRequestContext _apiContext;
    private readonly ConcurrentDictionary<string, IActor> _actors = new();

    public IActor? LastActiveActor { get; set; }

    public Cast(IAPIRequestContext apiContext, IPage? page = null, IBrowserContext? context = null)
    {
        _apiContext = apiContext;
        _page = page;
        _context = context;
    }

    public IActor ActorNamed(string name)
    {
        var actor = _actors.GetOrAdd(name, n =>
        {
            var a = new Actor(n);
            a.Can(CallAnApi.At(_apiContext, TestConfiguration.ApiUrl));

            if (_page != null && _context != null)
            {
                a.Can(BrowseTheWeb.With(_page, _context));
            }

            return a;
        });

        LastActiveActor = actor;
        return actor;
    }

    public IActor Owner() => ActorNamed("Owner");
    public IActor Guest(string name = "Guest") => ActorNamed(name);
    public IActor Waiter() => ActorNamed("Waiter");
    public IActor UnauthorizedUser() => ActorNamed("Unauthorized");
}

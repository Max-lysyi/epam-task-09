using FluentAssertions;
using RestaurantAqaFramework.Screenplay.Actors;
using RestaurantAqaFramework.Screenplay.Questions;
using TechTalk.SpecFlow;

namespace RestaurantAqaFramework.StepDefinitions;

public static class TestContextKeys
{
    public const string GroupId = "CurrentGroupId";
    public const string ReservationId = "CurrentReservationId";
    public const string OrderId = "CurrentOrderId";
    public const string SessionIds = "SessionIdsMap";
    public const string LastResponse = "LastApiResponse";
}

[Binding]
public class CommonStepDefinitions
{
    private readonly ScenarioContext _context;
    private readonly Cast _cast;

    public CommonStepDefinitions(ScenarioContext context, Cast cast)
    {
        _context = context;
        _cast = cast;
    }

    [Then(@"the API response status code is (.*)")]
    public async Task ThenTheApiResponseStatusCodeIs(int expectedStatusCode)
    {
        var actor = _cast.LastActiveActor ?? _cast.Owner();
        var statusCode = await actor.AsksFor(LastApiStatusCode.Value());
        statusCode.Should().Be(expectedStatusCode);
    }
}

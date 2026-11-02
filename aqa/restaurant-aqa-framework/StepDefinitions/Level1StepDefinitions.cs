using FluentAssertions;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Actors;
using RestaurantAqaFramework.Screenplay.Questions;
using RestaurantAqaFramework.Screenplay.Tasks;
using RestaurantAqaFramework.TestData;
using TechTalk.SpecFlow;

namespace RestaurantAqaFramework.StepDefinitions;

[Binding]
public class Level1StepDefinitions
{
    private readonly ScenarioContext _context;
    private readonly Cast _cast;

    public Level1StepDefinitions(ScenarioContext context, Cast cast)
    {
        _context = context;
        _cast = cast;
    }

    [Given(@"an active group reservation exists with an Owner and 2 guests:")]
    [Given(@"an active group reservation exists with participants:")]
    public async Task GivenAnActiveGroupReservationExistsWithParticipants(Table table)
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var sessionMap = new Dictionary<string, string>();
        _context[TestContextKeys.SessionIds] = sessionMap;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);

        var participantRows = table.Rows.GroupBy(r => r["Name"]);

        foreach (var group in participantRows)
        {
            var firstRow = group.First();
            var name = firstRow["Name"];
            var type = firstRow["Type"];
            var isSubmitted = bool.Parse(firstRow["IsSubmitted"]);
            var sessionId = TestDataGenerator.GenerateSessionId(name.ToLower());
            sessionMap[name] = sessionId;

            var items = new List<TestOrderItemRequest>();
            foreach (var row in group)
            {
                var dishName = row["DishName"];
                var price = decimal.Parse(row["Price"]);
                var quantity = int.Parse(row["Quantity"]);
                var dishId = row.ContainsKey("DishId") ? row["DishId"] : $"dish-{dishName.ToLower().Replace(' ', '-')}";

                items.Add(new TestOrderItemRequest
                {
                    DishId = dishId,
                    DishName = dishName,
                    Price = price,
                    Quantity = quantity
                });
            }

            setupTask.AddSession(sessionId, name, type, isSubmitted, items);
        }

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [Given(@"an active group reservation exists with no participants")]
    public async Task GivenAnActiveGroupReservationExistsWithNoParticipants()
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId);
        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [Given(@"a valid group reservation exists")]
    public async Task GivenAValidGroupReservationExists()
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId);
        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [When(@"the Owner opens the group pre-order review page")]
    public async Task WhenTheOwnerOpensTheGroupPreOrderReviewPage()
    {
        var reservationId = _context.Get<string>(TestContextKeys.ReservationId);
        var owner = _cast.Owner();
        await owner.AttemptsTo(OpenGroupOrderPage.ForReservation(reservationId));
    }

    [Then(@"the Owner sees all (.*) participants in the list")]
    public async Task ThenTheOwnerSeesAllParticipantsInTheList(int expectedCount)
    {
        var owner = _cast.Owner();
        var count = await owner.AsksFor(ParticipantsCount.Displayed());
        count.Should().Be(expectedCount);
    }

    [Then(@"the participant ""(.*)"" has subtotal (.*)")]
    public async Task ThenTheParticipantHasSubtotal(string participantName, decimal expectedSubtotal)
    {
        var owner = _cast.Owner();
        var cardState = await owner.AsksFor(ParticipantCardState.For(participantName));
        cardState.Subtotal.Should().Be(expectedSubtotal);
    }

    [Then(@"the Grand Total equals (.*)")]
    public async Task ThenTheGrandTotalEquals(decimal expectedGrandTotal)
    {
        var owner = _cast.Owner();
        var grandTotal = await owner.AsksFor(GrandTotalAmount.Displayed());
        grandTotal.Should().Be(expectedGrandTotal);
    }

    [When(@"the guest ""(.*)"" updates their cart to (.*) ""(.*)"" with price (.*) via API")]
    public async Task WhenTheGuestUpdatesTheirCartToViaApi(string guestName, int quantity, string dishName, decimal price)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var sessionMap = _context.Get<Dictionary<string, string>>(TestContextKeys.SessionIds);
        var sessionId = sessionMap[guestName];

        var guest = _cast.Guest(guestName);
        await guest.AttemptsTo(UpdateGuestCartViaApi.For(groupId, sessionId, new()
        {
            new TestOrderItemRequest
            {
                DishId = $"dish-{dishName.ToLower().Replace(' ', '-')}",
                DishName = dishName,
                Price = price,
                Quantity = quantity
            }
        }));
    }

    [When(@"the Owner refreshes the page")]
    public async Task WhenTheOwnerRefreshesThePage()
    {
        var owner = _cast.Owner();
        await owner.AttemptsTo(ClickRefresh.Now());
    }

    [Then(@"the participant ""(.*)"" is marked with status ""(.*)""")]
    public async Task ThenTheParticipantIsMarkedWithStatus(string name, string status)
    {
        var owner = _cast.Owner();
        var state = await owner.AsksFor(ParticipantCardState.For(name));
        if (status.Equals("Still choosing", StringComparison.OrdinalIgnoreCase))
        {
            state.IsSubmitted.Should().BeFalse();
        }
        else
        {
            state.IsSubmitted.Should().BeTrue();
        }
    }

    [Then(@"a notice is displayed stating participants have not submitted their cart yet")]
    public async Task ThenANoticeIsDisplayedStatingParticipantsHaveNotSubmittedTheirCartYet()
    {
        var owner = _cast.Owner();
        var noticeText = await owner.AsksFor(UnsubmittedNotice.Text());
        noticeText.Should().NotBeNullOrEmpty();
        noticeText!.ToLowerInvariant().Should().MatchRegex(@"(hasn['’]t|haven['’]t)\s+submitted\s+their\s+cart");
    }

    [When(@"the Owner requests the participants list via API")]
    public async Task WhenTheOwnerRequestsTheParticipantsListViaApi()
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var owner = _cast.Owner();
        await owner.AttemptsTo(FetchGroupSessions.ForGroup(groupId));
    }

    [Then(@"the API response contains (.*) participants")]
    public async Task ThenTheApiResponseContainsParticipants(int count)
    {
        var owner = _cast.Owner();
        var data = await owner.AsksFor(GroupSessionsData.Current());
        data.Should().NotBeNull();
        data!.Participants.Count.Should().Be(count);
    }

    [Then(@"the API response total is (.*)")]
    public async Task ThenTheApiResponseTotalIs(decimal expectedTotal)
    {
        var owner = _cast.Owner();
        var data = await owner.AsksFor(GroupSessionsData.Current());
        data.Should().NotBeNull();
        data!.Total.Should().Be(expectedTotal);
    }

    [When(@"the Owner requests the participants list for a non-existent group ""(.*)"" via API")]
    public async Task WhenTheOwnerRequestsTheParticipantsListForANonExistentGroupViaApi(string nonExistentGroupId)
    {
        var owner = _cast.Owner();
        await owner.AttemptsTo(FetchGroupSessions.ForGroup(nonExistentGroupId));
    }

    [When(@"a user with role ""(.*)"" requests the participants list via API")]
    public async Task WhenAUserWithRoleRequestsTheParticipantsListViaApi(string role)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var actor = _cast.ActorNamed(role);

        var token = role.Equals("Unauthorized", StringComparison.OrdinalIgnoreCase) ? "unauthorized" : null;
        var roleHeader = role.Equals("Unauthorized", StringComparison.OrdinalIgnoreCase) ? null : role;

        await actor.AttemptsTo(FetchGroupSessions.ForGroup(groupId, roleHeader, token));
    }
}


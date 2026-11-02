using FluentAssertions;
using Microsoft.Playwright;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Pages;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Actors;
using RestaurantAqaFramework.Screenplay.Questions;
using RestaurantAqaFramework.Screenplay.Tasks;
using RestaurantAqaFramework.TestData;
using TechTalk.SpecFlow;

namespace RestaurantAqaFramework.StepDefinitions;

[Binding]
public class Level3StepDefinitions
{
    private readonly ScenarioContext _context;
    private readonly Cast _cast;

    public Level3StepDefinitions(ScenarioContext context, Cast cast)
    {
        _context = context;
        _cast = cast;
    }

    [When(@"the Owner removes the guest ""(.*)"" via UI")]
    public async Task WhenTheOwnerRemovesTheGuestViaUi(string guestName)
    {
        var owner = _cast.Owner();
        await owner.AttemptsTo(RemoveGuestParticipant.Named(guestName));
    }

    [Then(@"the guest ""(.*)"" is no longer visible in the participants list")]
    public async Task ThenTheGuestIsNoLongerVisibleInTheParticipantsList(string guestName)
    {
        var owner = _cast.Owner();
        var browse = owner.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        await page.ParticipantCardByName(guestName).WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });

        var names = await owner.AsksFor(ParticipantNames.Displayed());
        names.Should().NotContain(n => n.Contains(guestName));
    }

    [Then(@"the remaining participants ""(.*)"" and ""(.*)"" are still present")]
    public async Task ThenTheRemainingParticipantsAreStillPresent(string p1, string p2)
    {
        var owner = _cast.Owner();
        var names = await owner.AsksFor(ParticipantNames.Displayed());
        names.Should().Contain(n => n.Contains(p1));
        names.Should().Contain(n => n.Contains(p2));
    }

    [When(@"the Owner removes the guest session for ""(.*)"" via API")]
    public async Task WhenTheOwnerRemovesTheGuestSessionForViaApi(string guestName)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var sessionMap = _context.Get<Dictionary<string, string>>(TestContextKeys.SessionIds);
        var sessionId = sessionMap[guestName];

        var owner = _cast.Owner();
        await owner.AttemptsTo(RemoveGuestSessionViaApi.For(groupId, sessionId));
    }

    [Then(@"the API response does not contain participant ""(.*)""")]
    public async Task ThenTheApiResponseDoesNotContainParticipant(string guestName)
    {
        var owner = _cast.Owner();
        var data = await owner.AsksFor(GroupSessionsData.Current());
        data.Should().NotBeNull();
        data!.Participants.Should().NotContain(p => p.Name == guestName);
    }

    [Given(@"an active group reservation exists with only one guest ""(.*)"" and the Owner:")]
    public async Task GivenAnActiveGroupReservationExistsWithOnlyOneGuestAndTheOwner(string guestName, Table table)
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
                items.Add(new TestOrderItemRequest
                {
                    DishId = $"dish-{row["DishName"].ToLower().Replace(' ', '-')}",
                    DishName = row["DishName"],
                    Price = decimal.Parse(row["Price"]),
                    Quantity = int.Parse(row["Quantity"])
                });
            }

            setupTask.AddSession(sessionId, name, type, isSubmitted, items);
        }

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [Then(@"the API response contains only (.*) participant ""(.*)""")]
    public async Task ThenTheApiResponseContainsOnlyParticipant(int count, string expectedParticipantName)
    {
        var owner = _cast.Owner();
        var data = await owner.AsksFor(GroupSessionsData.Current());
        data.Should().NotBeNull();
        data!.Participants.Count.Should().Be(count);
        data.Participants.First().Name.Should().Be(expectedParticipantName);
    }

    [Given(@"two separate group reservations ""(.*)"" and ""(.*)"" exist with guests")]
    public async Task GivenTwoSeparateGroupReservationsExistWithGuests(string groupAliasA, string groupAliasB)
    {
        var groupIdA = TestDataGenerator.GenerateGroupId();
        var resIdA = TestDataGenerator.GenerateReservationId();
        var orderIdA = $"ord-{Guid.NewGuid():N}"[..16];

        var groupIdB = TestDataGenerator.GenerateGroupId();
        var resIdB = TestDataGenerator.GenerateReservationId();
        var orderIdB = $"ord-{Guid.NewGuid():N}"[..16];

        var guestSessionIdB = TestDataGenerator.GenerateSessionId("guest-b");

        _context["GroupId_" + groupAliasA] = groupIdA;
        _context["GroupId_" + groupAliasB] = groupIdB;
        _context["GuestSessionId_" + groupAliasB] = guestSessionIdB;

        var owner = _cast.Owner();

        var setupA = SetupTestGroupData.WithId(groupIdA, resIdA, orderIdA);
        setupA.AddSession("owner-a", "OwnerA", "Owner", true, new());
        await owner.AttemptsTo(setupA);

        var setupB = SetupTestGroupData.WithId(groupIdB, resIdB, orderIdB);
        setupB.AddSession("owner-b", "OwnerB", "Owner", true, new());
        setupB.AddSession(guestSessionIdB, "GuestB", "Guest", true, new());
        await owner.AttemptsTo(setupB);
    }

    [When(@"the Owner attempts to delete a guest from ""(.*)"" using ""(.*)"" endpoint")]
    public async Task WhenTheOwnerAttemptsToDeleteAGuestFromUsingEndpoint(string targetGroup, string sourceGroup)
    {
        var sourceGroupId = _context.Get<string>("GroupId_" + sourceGroup);
        var targetGuestSessionId = _context.Get<string>("GuestSessionId_" + targetGroup);

        var owner = _cast.Owner();
        // Trying to delete targetGuestSessionId using sourceGroupId URL
        await owner.AttemptsTo(RemoveGuestSessionViaApi.For(sourceGroupId, targetGuestSessionId));
    }

    [When(@"the Owner attempts to delete a non-existent session ""(.*)"" via API")]
    public async Task WhenTheOwnerAttemptsToDeleteANonExistentSessionViaApi(string nonExistentSessionId)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var owner = _cast.Owner();
        await owner.AttemptsTo(RemoveGuestSessionViaApi.For(groupId, nonExistentSessionId));
    }

    [Given(@"an active group reservation exists with a guest ""(.*)""")]
    public async Task GivenAnActiveGroupReservationExistsWithAGuest(string guestName)
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];
        var sessionId = TestDataGenerator.GenerateSessionId("guest");

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var sessionMap = new Dictionary<string, string> { [guestName] = sessionId };
        _context[TestContextKeys.SessionIds] = sessionMap;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);
        setupTask.AddSession(sessionId, guestName, "Guest", true, new()
        {
            new() { DishId = "dish1", DishName = "Salad", Price = 10m, Quantity = 1 }
        });

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [When(@"a user with role ""(.*)"" attempts to delete the guest ""(.*)"" via API")]
    public async Task WhenAUserWithRoleAttemptsToDeleteTheGuestViaApi(string role, string guestName)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var sessionMap = _context.Get<Dictionary<string, string>>(TestContextKeys.SessionIds);
        var sessionId = sessionMap[guestName];

        var actor = _cast.ActorNamed(role);
        var roleHeader = role.Equals("Unauthorized", StringComparison.OrdinalIgnoreCase) ? "Unauthorized" : role;

        await actor.AttemptsTo(RemoveGuestSessionViaApi.For(groupId, sessionId, roleHeader));
    }

    [Given(@"an active group reservation is created with an order")]
    public void GivenAnActiveGroupReservationIsCreatedWithAnOrder()
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;
        _context[TestContextKeys.SessionIds] = new Dictionary<string, string>();
    }

    [Given(@"the following participants join and add dishes:")]
    public async Task GivenTheFollowingParticipantsJoinAndAddDishes(Table table)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var reservationId = _context.Get<string>(TestContextKeys.ReservationId);
        var orderId = _context.Get<string>(TestContextKeys.OrderId);
        var sessionMap = _context.Get<Dictionary<string, string>>(TestContextKeys.SessionIds);

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
                items.Add(new TestOrderItemRequest
                {
                    DishId = row["DishId"],
                    DishName = row["DishName"],
                    Price = decimal.Parse(row["Price"]),
                    Quantity = int.Parse(row["Quantity"])
                });
            }

            setupTask.AddSession(sessionId, name, type, isSubmitted, items);
        }

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [Then(@"the Waiter verifies the final submitted order via API:")]
    public async Task ThenTheWaiterVerifiesTheFinalSubmittedOrderViaApi(Table expectedTable)
    {
        var orderId = _context.Get<string>(TestContextKeys.OrderId);
        var waiter = _cast.Waiter();
        await waiter.AttemptsTo(VerifyWaiterOrderViaApi.ForOrder(orderId));

        var order = await waiter.AsksFor(WaiterOrderData.Current());
        order.Should().NotBeNull();
        order!.Status.Should().Be("SubmittedByCustomer");

        foreach (var row in expectedTable.Rows)
        {
            var dishId = row["DishId"];
            var expectedQuantity = int.Parse(row["Quantity"]);

            var item = order.Items.FirstOrDefault(i => i.DishId == dishId);
            item.Should().NotBeNull($"Dish '{dishId}' should be in order");
            item!.Quantity.Should().Be(expectedQuantity);
        }
    }

    [Then(@"the Waiter verifies the removed guest dish ""(.*)"" is not in the order")]
    public async Task ThenTheWaiterVerifiesTheRemovedGuestDishIsNotInTheOrder(string removedDishId)
    {
        var orderId = _context.Get<string>(TestContextKeys.OrderId);
        var waiter = _cast.Waiter();
        await waiter.AttemptsTo(VerifyWaiterOrderViaApi.ForOrder(orderId));

        var order = await waiter.AsksFor(WaiterOrderData.Current());
        order.Should().NotBeNull();
        order!.Items.Should().NotContain(i => i.DishId == removedDishId);
    }
}


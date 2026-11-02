using FluentAssertions;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Actors;
using RestaurantAqaFramework.Screenplay.Questions;
using RestaurantAqaFramework.Screenplay.Tasks;
using RestaurantAqaFramework.TestData;
using TechTalk.SpecFlow;

namespace RestaurantAqaFramework.StepDefinitions;

[Binding]
public class Level2StepDefinitions
{
    private readonly ScenarioContext _context;
    private readonly Cast _cast;

    public Level2StepDefinitions(ScenarioContext context, Cast cast)
    {
        _context = context;
        _cast = cast;
    }

    [Given(@"an active group reservation exists with confirmed carts:")]
    public async Task GivenAnActiveGroupReservationExistsWithConfirmedCarts(Table table)
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);
        var participantRows = table.Rows.GroupBy(r => r["Name"]);

        foreach (var group in participantRows)
        {
            var firstRow = group.First();
            var name = firstRow["Name"];
            var type = firstRow["Type"];
            var isSubmitted = bool.Parse(firstRow["IsSubmitted"]);
            var sessionId = TestDataGenerator.GenerateSessionId(name.ToLower());

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

    [Given(@"an active group reservation with ready submitted carts exists")]
    public async Task GivenAnActiveGroupReservationWithReadySubmittedCartsExists()
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);
        setupTask.AddSession("s-owner", "Owner", "Owner", true, new()
        {
            new() { DishId = "dish1", DishName = "Caesar Salad", Price = 18m, Quantity = 1 }
        });
        setupTask.AddSession("s-guest1", "Olena", "Guest", true, new()
        {
            new() { DishId = "dish1", DishName = "Caesar Salad", Price = 18m, Quantity = 2 }
        });

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [Given(@"an active group reservation exists where no participants have confirmed their cart:")]
    public async Task GivenAnActiveGroupReservationExistsWhereNoParticipantsHaveConfirmedTheirCart(Table table)
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);
        var participantRows = table.Rows.GroupBy(r => r["Name"]);

        foreach (var group in participantRows)
        {
            var firstRow = group.First();
            var name = firstRow["Name"];
            var type = firstRow["Type"];
            var isSubmitted = bool.Parse(firstRow["IsSubmitted"]);
            var sessionId = TestDataGenerator.GenerateSessionId(name.ToLower());

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

    [Given(@"an active group reservation exists where confirmed participants have no dishes:")]
    public async Task GivenAnActiveGroupReservationExistsWhereConfirmedParticipantsHaveNoDishes(Table table)
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);
        foreach (var row in table.Rows)
        {
            var name = row["Name"];
            var type = row["Type"];
            var isSubmitted = bool.Parse(row["IsSubmitted"]);
            var sessionId = TestDataGenerator.GenerateSessionId(name.ToLower());

            setupTask.AddSession(sessionId, name, type, isSubmitted, new());
        }

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);
    }

    [Given(@"a group reservation that has already been submitted")]
    public async Task GivenAGroupReservationThatHasAlreadyBeenSubmitted()
    {
        var groupId = TestDataGenerator.GenerateGroupId();
        var reservationId = TestDataGenerator.GenerateReservationId();
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        _context[TestContextKeys.GroupId] = groupId;
        _context[TestContextKeys.ReservationId] = reservationId;
        _context[TestContextKeys.OrderId] = orderId;

        var setupTask = SetupTestGroupData.WithId(groupId, reservationId, orderId);
        setupTask.AddSession("s-owner", "Owner", "Owner", true, new()
        {
            new() { DishId = "dish1", DishName = "Salad", Price = 10m, Quantity = 1 }
        });

        var owner = _cast.Owner();
        await owner.AttemptsTo(setupTask);

        // Submit once to make it 'Submitted'
        await owner.AttemptsTo(SubmitGroupPreOrderViaApi.ForGroup(groupId, reservationId));
    }

    [When(@"the Owner submits the consolidated pre-order via UI")]
    public async Task WhenTheOwnerSubmitsTheConsolidatedPreOrderViaUi()
    {
        var owner = _cast.Owner();
        await owner.AttemptsTo(SubmitConsolidatedPreOrder.Confirm());
    }

    [When(@"the Owner submits the consolidated pre-order via API")]
    public async Task WhenTheOwnerSubmitsTheConsolidatedPreOrderViaApi()
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var reservationId = _context.Get<string>(TestContextKeys.ReservationId);
        var owner = _cast.Owner();
        await owner.AttemptsTo(SubmitGroupPreOrderViaApi.ForGroup(groupId, reservationId));
    }

    [When(@"a user with role ""(.*)"" submits the consolidated pre-order via API")]
    public async Task WhenAUserWithRoleSubmitsTheConsolidatedPreOrderViaApi(string role)
    {
        var groupId = _context.Get<string>(TestContextKeys.GroupId);
        var reservationId = _context.Get<string>(TestContextKeys.ReservationId);

        var actor = _cast.ActorNamed(role);
        var roleHeader = role.Equals("Unauthorized", StringComparison.OrdinalIgnoreCase) ? "Unauthorized" : role;

        await actor.AttemptsTo(SubmitGroupPreOrderViaApi.ForGroup(groupId, reservationId, roleHeader));
    }

    [When(@"the Owner attempts to submit a pre-order with condition ""(.*)"" via API")]
    public async Task WhenTheOwnerAttemptsToSubmitAPreOrderWithConditionViaApi(string condition)
    {
        var owner = _cast.Owner();
        var groupId = _context.ContainsKey(TestContextKeys.GroupId) ? _context.Get<string>(TestContextKeys.GroupId) : TestDataGenerator.GenerateGroupId();
        var reservationId = _context.ContainsKey(TestContextKeys.ReservationId) ? _context.Get<string>(TestContextKeys.ReservationId) : TestDataGenerator.GenerateReservationId();

        switch (condition.ToLower())
        {
            case "non-existent group":
                await owner.AttemptsTo(SubmitGroupPreOrderViaApi.ForGroup("non-existent-group-999", reservationId));
                break;
            case "mismatched reservation id":
                if (!_context.ContainsKey(TestContextKeys.GroupId))
                {
                    await GivenAnActiveGroupReservationWithReadySubmittedCartsExists();
                    groupId = _context.Get<string>(TestContextKeys.GroupId);
                }
                await owner.AttemptsTo(SubmitGroupPreOrderViaApi.ForGroup(groupId, "wrong-reservation-id"));
                break;
            case "missing order in group":
                var emptyGroupWithoutOrderId = TestDataGenerator.GenerateGroupId();
                var resId = TestDataGenerator.GenerateReservationId();
                await owner.AttemptsTo(SetupTestGroupData.WithId(emptyGroupWithoutOrderId, resId, null, createOrder: false));
                await owner.AttemptsTo(SubmitGroupPreOrderViaApi.ForGroup(emptyGroupWithoutOrderId, resId));
                break;
            default:
                throw new ArgumentException($"Unknown condition: {condition}");
        }
    }

    [Then(@"the group status badge changes to ""(.*)""")]
    public async Task ThenTheGroupStatusBadgeChangesTo(string expectedStatus)
    {
        var owner = _cast.Owner();
        var status = await owner.AsksFor(GroupOrderStatusBadge.Text());
        status.Should().Be(expectedStatus);
    }

    [Then(@"the order status in restaurant backend is ""(.*)""")]
    public async Task ThenTheOrderStatusInRestaurantBackendIs(string expectedOrderStatus)
    {
        var orderId = _context.Get<string>(TestContextKeys.OrderId);
        var waiter = _cast.Waiter();
        await waiter.AttemptsTo(VerifyWaiterOrderViaApi.ForOrder(orderId));

        var order = await waiter.AsksFor(WaiterOrderData.Current());
        order.Should().NotBeNull();
        order!.Status.Should().Be(expectedOrderStatus);
    }

    [Then(@"the confirmed order composition has merged dishes:")]
    public async Task ThenTheConfirmedOrderCompositionHasMergedDishes(Table expectedDishesTable)
    {
        var orderId = _context.Get<string>(TestContextKeys.OrderId);
        var waiter = _cast.Waiter();
        await waiter.AttemptsTo(VerifyWaiterOrderViaApi.ForOrder(orderId));

        var order = await waiter.AsksFor(WaiterOrderData.Current());
        order.Should().NotBeNull();

        foreach (var row in expectedDishesTable.Rows)
        {
            var dishId = row["DishId"];
            var expectedQuantity = int.Parse(row["Quantity"]);

            var match = order!.Items.FirstOrDefault(i => i.DishId == dishId);
            match.Should().NotBeNull($"Dish with id '{dishId}' should be present in order");
            match!.Quantity.Should().Be(expectedQuantity);
        }
    }

    [Then(@"the unconfirmed dish ""(.*)"" is excluded from the order")]
    public async Task ThenTheUnconfirmedDishIsExcludedFromTheOrder(string excludedDishId)
    {
        var orderId = _context.Get<string>(TestContextKeys.OrderId);
        var waiter = _cast.Waiter();
        await waiter.AttemptsTo(VerifyWaiterOrderViaApi.ForOrder(orderId));

        var order = await waiter.AsksFor(WaiterOrderData.Current());
        order.Should().NotBeNull();
        order!.Items.Should().NotContain(i => i.DishId == excludedDishId);
    }
}

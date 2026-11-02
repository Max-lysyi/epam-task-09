using Microsoft.Playwright;
using RestaurantAqaFramework.Configuration;
using RestaurantAqaFramework.Pages;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Actors;

namespace RestaurantAqaFramework.Screenplay.Tasks;

public class OpenGroupOrderPage : IPerformable
{
    private readonly string _reservationId;
    private readonly bool _useDemoLogin;

    public OpenGroupOrderPage(string reservationId, bool useDemoLogin = true)
    {
        _reservationId = reservationId;
        _useDemoLogin = useDemoLogin;
    }

    public static OpenGroupOrderPage ForReservation(string reservationId, bool useDemoLogin = true) =>
        new(reservationId, useDemoLogin);

    public async Task PerformAs(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();

        if (_useDemoLogin)
        {
            await browse.Page.AddInitScriptAsync(@"() => {
                const now = Math.floor(Date.now() / 1000);
                const payload = {
                    iss: 'green-and-tasty-demo',
                    sub: 'demo-owner',
                    email: 'owner@demo.test',
                    given_name: 'Yevhenii',
                    family_name: 'Demo',
                    'cognito:groups': ['Customer'],
                    iat: now,
                    exp: now + 30 * 24 * 60 * 60
                };
                const b64 = btoa(unescape(encodeURIComponent(JSON.stringify(payload)))).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
                const token = 'eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.' + b64 + '.demo';
                localStorage.setItem('auth', JSON.stringify({ idToken: token, accessToken: token }));
                localStorage.setItem('user', JSON.stringify({
                    id: 'demo-owner',
                    firstName: 'Yevhenii',
                    lastName: 'Demo',
                    email: 'owner@demo.test',
                    role: 'Customer'
                }));
            }");
        }

        var baseUrl = TestConfiguration.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/reservations/{_reservationId}/group-order";
        if (_useDemoLogin)
        {
            url += "?demo";
        }

        await browse.Page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = TestConfiguration.DefaultTimeout
        });

        var page = new GroupOrderPage(browse.Page);
        await page.PageTitle.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }
}

public class SubmitConsolidatedPreOrder : IPerformable
{
    public static SubmitConsolidatedPreOrder Confirm() => new();

    public async Task PerformAs(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);

        await page.SubmitPreOrderButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await page.SubmitPreOrderButton.ClickAsync();

        await page.SubmitModal.ConfirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await page.SubmitModal.ConfirmButton.ClickAsync();
        await page.SubmitModal.Root.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });

        // Wait for status badge to update to Sent to Restaurant
        await page.SubmittedStatusBadge.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }
}

public class CancelSubmitPreOrder : IPerformable
{
    public static CancelSubmitPreOrder Cancel() => new();

    public async Task PerformAs(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);

        await page.SubmitPreOrderButton.ClickAsync();
        await page.SubmitModal.CancelButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await page.SubmitModal.CancelButton.ClickAsync();
    }
}

public class RemoveGuestParticipant : IPerformable
{
    private readonly string _guestName;

    public RemoveGuestParticipant(string guestName)
    {
        _guestName = guestName;
    }

    public static RemoveGuestParticipant Named(string guestName) => new(guestName);

    public async Task PerformAs(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        var card = await page.GetParticipantCardAsync(_guestName);

        var removeBtn = card.RemoveButton(_guestName);
        if (await removeBtn.CountAsync() == 0)
        {
            removeBtn = card.RemoveButtonGeneric;
        }

        await removeBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await removeBtn.ClickAsync();

        await page.RemoveGuestModal.ConfirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await page.RemoveGuestModal.ConfirmButton.ClickAsync();
        await page.RemoveGuestModal.Root.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
        await card.Root.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
    }
}

public class CancelRemoveGuest : IPerformable
{
    private readonly string _guestName;

    public CancelRemoveGuest(string guestName)
    {
        _guestName = guestName;
    }

    public static CancelRemoveGuest Named(string guestName) => new(guestName);

    public async Task PerformAs(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        var card = await page.GetParticipantCardAsync(_guestName);

        var removeBtn = card.RemoveButton(_guestName);
        if (await removeBtn.CountAsync() == 0)
        {
            removeBtn = card.RemoveButtonGeneric;
        }

        await removeBtn.ClickAsync();
        await page.RemoveGuestModal.CancelButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await page.RemoveGuestModal.CancelButton.ClickAsync();
    }
}

public class ClickRefresh : IPerformable
{
    public static ClickRefresh Now() => new();

    public async Task PerformAs(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        await page.RefreshButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await page.RefreshButton.ClickAsync();
    }
}

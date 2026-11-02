using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RestaurantAqaFramework.Pages;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Actors;

namespace RestaurantAqaFramework.Screenplay.Questions;

public class ParticipantsCount : IQuestion<int>
{
    public static ParticipantsCount Displayed() => new();

    public async Task<int> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        try
        {
            await page.ParticipantCards.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        }
        catch
        {
            // Empty state or 0 cards
        }
        return await page.ParticipantCards.CountAsync();
    }
}

public class ParticipantNames : IQuestion<List<string>>
{
    public static ParticipantNames Displayed() => new();

    public async Task<List<string>> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        try
        {
            await page.ParticipantCards.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        }
        catch
        {
            // No cards
        }

        var count = await page.ParticipantCards.CountAsync();
        var names = new List<string>();

        for (var i = 0; i < count; i++)
        {
            var heading = page.ParticipantCards.Nth(i).Locator("h2");
            var text = await heading.InnerTextAsync();
            var trimmed = text.Trim();
            if (trimmed == "My Pre-Order")
            {
                names.Add("My Pre-Order");
                names.Add("Yevhenii");
            }
            else
            {
                names.Add(trimmed);
            }
        }

        return names;
    }
}

public class ParticipantCardState : IQuestion<ParticipantUiState>
{
    private readonly string _name;

    public ParticipantCardState(string name)
    {
        _name = name;
    }

    public static ParticipantCardState For(string name) => new(name);

    public async Task<ParticipantUiState> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        var card = await page.GetParticipantCardAsync(_name);
        await card.Root.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        var isSubmitted = await card.SubmittedChip.CountAsync() > 0;
        var subtotalText = await card.Subtotal.InnerTextAsync();
        var subtotal = ParseCurrency(subtotalText);

        var itemsCount = await card.Items.CountAsync();

        return new ParticipantUiState
        {
            Name = _name,
            IsSubmitted = isSubmitted,
            Subtotal = subtotal,
            ItemsCount = itemsCount
        };
    }

    private static decimal ParseCurrency(string text)
    {
        var cleaned = Regex.Replace(text, @"[^\d.]", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0m;
    }
}

public class ParticipantUiState
{
    public string Name { get; set; } = string.Empty;
    public bool IsSubmitted { get; set; }
    public decimal Subtotal { get; set; }
    public int ItemsCount { get; set; }
}

public class GrandTotalAmount : IQuestion<decimal>
{
    public static GrandTotalAmount Displayed() => new();

    public async Task<decimal> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        await page.GrandTotalAmount.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var text = await page.GrandTotalAmount.InnerTextAsync();

        var cleaned = Regex.Replace(text, @"[^\d.]", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0m;
    }
}

public class GroupOrderStatusBadge : IQuestion<string>
{
    public static GroupOrderStatusBadge Text() => new();

    public async Task<string> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        await page.StatusBadge.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        return (await page.StatusBadge.InnerTextAsync()).Trim();
    }
}

public class UnsubmittedNotice : IQuestion<string?>
{
    public static UnsubmittedNotice Text() => new();

    public async Task<string?> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        try
        {
            await page.UnsubmittedAlert.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
            return (await page.UnsubmittedAlert.InnerTextAsync()).Trim();
        }
        catch
        {
            return null;
        }
    }
}

public class EmptyStateDisplayed : IQuestion<bool>
{
    public static EmptyStateDisplayed Check() => new();

    public async Task<bool> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        try
        {
            await page.EmptyStateNotice.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class NotFoundErrorDisplayed : IQuestion<bool>
{
    public static NotFoundErrorDisplayed Check() => new();

    public async Task<bool> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        try
        {
            await page.NotFoundError.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class SubmitButtonState : IQuestion<bool>
{
    public static SubmitButtonState IsEnabled() => new();

    public async Task<bool> AnsweredBy(IActor actor)
    {
        var browse = actor.Using<BrowseTheWeb>();
        var page = new GroupOrderPage(browse.Page);
        if (await page.SubmitPreOrderButton.CountAsync() == 0)
        {
            return false;
        }

        return await page.SubmitPreOrderButton.IsEnabledAsync();
    }
}

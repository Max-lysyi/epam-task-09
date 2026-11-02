using Microsoft.Playwright;

namespace RestaurantAqaFramework.Pages;

public class GroupOrderPage
{
    private readonly IPage _page;

    public GroupOrderPage(IPage page)
    {
        _page = page;
    }

    // Header & Meta
    public ILocator PageTitle =>
        _page.GetByRole(AriaRole.Heading, new() { Name = "Group Pre-Order", Level = 1 });

    public ILocator StatusBadge =>
        _page.Locator(".status-badge");

    public ILocator SubmittedStatusBadge =>
        _page.Locator(".status-badge.submitted");

    public ILocator RefreshButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Refresh" });

    public ILocator LastUpdatedText =>
        _page.Locator(".sync-text");

    // Alerts & Notices
    public ILocator UnsubmittedAlert =>
        _page.GetByRole(AriaRole.Alert);

    public ILocator EmptyStateNotice =>
        _page.GetByText("Participants haven’t added any dishes yet");

    public ILocator NotFoundError =>
        _page.GetByText("Group reservation not found");

    public ILocator GeneralErrorMessage =>
        _page.GetByText("Failed to load group reservation");

    public ILocator TryAgainButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Try again" });

    // Participants List & Cards
    public ILocator ParticipantsList =>
        _page.Locator("ul.participants-list");

    public ILocator ParticipantCards =>
        _page.GetByRole(AriaRole.Article);

    public ILocator ParticipantCardByName(string name) =>
        _page.Locator("article.card").Filter(new() { HasText = name });

    public async Task<ParticipantCardComponent> GetParticipantCardAsync(string name)
    {
        var card = ParticipantCardByName(name);
        if (await card.CountAsync() > 0)
        {
            return new ParticipantCardComponent(card.First);
        }

        // Fallback for Owner when card title is "My Pre-Order"
        var myPreOrderCard = _page.Locator("article.card").Filter(new() { Has = _page.Locator("h2.title:text-is('My Pre-Order')") });
        if (await myPreOrderCard.CountAsync() > 0)
        {
            return new ParticipantCardComponent(myPreOrderCard.First);
        }

        return new ParticipantCardComponent(card);
    }

    public ParticipantCardComponent GetParticipantCard(string name) =>
        new(ParticipantCardByName(name));

    // Consolidated Pre-Order Section
    public ILocator ConsolidatedSection =>
        _page.Locator("section.consolidated-section");

    public ILocator ConsolidatedItems =>
        _page.Locator("li.consolidated-item");

    // Grand Total & Submit Footer
    public ILocator GrandTotalLabel =>
        _page.GetByText("Grand Total");

    public ILocator GrandTotalAmount =>
        _page.Locator(".grand-total-amount");

    public ILocator SubmitPreOrderButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order" });

    // Modals
    public SubmitModalComponent SubmitModal => new(_page);
    public RemoveGuestModalComponent RemoveGuestModal => new(_page);
}

public class ParticipantCardComponent
{
    private readonly ILocator _card;

    public ParticipantCardComponent(ILocator card)
    {
        _card = card;
    }

    public ILocator Root => _card;

    public ILocator Heading =>
        _card.GetByRole(AriaRole.Heading, new() { Level = 2 });

    public ILocator SubmittedChip =>
        _card.GetByText("Submitted", new() { Exact = true });

    public ILocator StillChoosingChip =>
        _card.GetByText("Still choosing", new() { Exact = true });

    public ILocator RemoveButton(string name) =>
        _card.GetByRole(AriaRole.Button, new() { Name = $"Remove {name}" });

    public ILocator RemoveButtonGeneric =>
        _card.GetByRole(AriaRole.Button, new() { Name = "Remove Guest" });

    public ILocator Items =>
        _card.Locator("li.item");

    public ILocator ItemByName(string dishName) =>
        _card.Locator("li.item").Filter(new() { HasText = dishName });

    public ILocator Subtotal =>
        _card.Locator("p.subtotal span").Nth(1);

    public ILocator EmptyNotice =>
        _card.GetByText("No dishes yet");
}

public class SubmitModalComponent
{
    private readonly IPage _page;

    public SubmitModalComponent(IPage page)
    {
        _page = page;
    }

    public ILocator Root =>
        _page.Locator("app-modal");

    public ILocator DialogTitle =>
        _page.GetByRole(AriaRole.Heading, new() { Name = "Submit Pre-Order" });

    public ILocator ConfirmButton =>
        _page.Locator(".modal-btn-primary").Filter(new() { HasText = "Submit" });

    public ILocator CancelButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Cancel" });
}

public class RemoveGuestModalComponent
{
    private readonly IPage _page;

    public RemoveGuestModalComponent(IPage page)
    {
        _page = page;
    }

    public ILocator Root =>
        _page.Locator("app-modal");

    public ILocator DialogTitle =>
        _page.GetByRole(AriaRole.Heading, new() { Name = "Remove Guest" });

    public ILocator ConfirmButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Remove Guest" });

    public ILocator CancelButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Cancel" });

    public ILocator ModalBody =>
        _page.Locator("app-modal p");
}


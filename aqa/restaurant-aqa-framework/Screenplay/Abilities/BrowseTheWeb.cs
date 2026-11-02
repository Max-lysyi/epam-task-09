using Microsoft.Playwright;

namespace RestaurantAqaFramework.Screenplay.Abilities;

public class BrowseTheWeb
{
    public IPage Page { get; }
    public IBrowserContext Context { get; }

    public BrowseTheWeb(IPage page, IBrowserContext context)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public static BrowseTheWeb With(IPage page, IBrowserContext context) => new(page, context);
}


using BoDi;
using NUnit.Framework;
using RestaurantAqaFramework.Drivers;
using RestaurantAqaFramework.Screenplay.Actors;
using RestaurantAqaFramework.TestData;
using TechTalk.SpecFlow;

namespace RestaurantAqaFramework.Hooks;

[Binding]
public class ScenarioHooks
{
    private readonly IObjectContainer _container;
    private readonly ScenarioContext _scenarioContext;
    private PlaywrightDriver? _driver;

    public ScenarioHooks(IObjectContainer container, ScenarioContext scenarioContext)
    {
        _container = container;
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = 0)]
    public async Task BeforeScenarioAsync()
    {
        _driver = new PlaywrightDriver();
        await _driver.InitializeAsync(_scenarioContext.ScenarioInfo.Title);

        var cast = new Cast(_driver.ApiContext, _driver.Page, _driver.Context);

        _container.RegisterInstanceAs(_driver);
        _container.RegisterInstanceAs(cast);
    }

    [AfterScenario(Order = 1)]
    public async Task AfterScenarioArtifactsAsync()
    {
        if (_driver != null)
        {
            var hasError = _scenarioContext.TestError != null;
            if (hasError)
            {
                var screenshotPath = await _driver.TakeScreenshotAsync(_scenarioContext.ScenarioInfo.Title);
                if (screenshotPath != null)
                {
                    TestContext.AddTestAttachment(screenshotPath, "Failure Screenshot");
                }
            }

            var tracePath = await _driver.StopTracingAsync(_scenarioContext.ScenarioInfo.Title, hasError);
            if (tracePath != null)
            {
                TestContext.AddTestAttachment(tracePath, "Playwright Trace");
            }
        }
    }

    [AfterScenario(Order = 2)]
    public async Task AfterScenarioCleanupAsync()
    {
        // Clean up test data generated during the scenario
        await TestDataManager.CleanupAllAsync();

        if (_driver != null)
        {
            await _driver.DisposeAsync();
        }
    }
}

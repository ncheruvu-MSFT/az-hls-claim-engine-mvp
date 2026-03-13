using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace ClaimsPortal.BlazorWasm.Tests;

/// <summary>
/// Base class for all Playwright tests with common setup and utilities
/// </summary>
[TestFixture]
public class TestBase : PageTest
{
    protected const string BaseUrl = "http://localhost:5000";

    [SetUp]
    public async Task Setup()
    {
        // Wait for Blazor to initialize
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TearDown]
    public async Task Teardown()
    {
        // Cleanup can be added here if needed
        await Task.CompletedTask;
    }

    /// <summary>
    /// Navigate to a page and wait for it to load
    /// </summary>
    protected async Task NavigateToAsync(string path)
    {
        await Page.GotoAsync($"{BaseUrl}{path}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Take a screenshot for visual comparison
    /// </summary>
    protected async Task TakeScreenshotAsync(string name)
    {
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/{name}.png",
            FullPage = true
        });
    }

    /// <summary>
    /// Check if an element has the expected CSS class
    /// </summary>
    protected async Task<bool> HasClassAsync(ILocator locator, string className)
    {
        var classAttr = await locator.GetAttributeAsync("class");
        return classAttr?.Contains(className) ?? false;
    }

    /// <summary>
    /// Get computed CSS property value
    /// </summary>
    protected async Task<string?> GetComputedStyleAsync(ILocator locator, string property)
    {
        return await locator.EvaluateAsync<string>($"el => window.getComputedStyle(el).{property}");
    }
}

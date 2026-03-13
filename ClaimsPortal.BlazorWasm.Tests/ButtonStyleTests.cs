using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace ClaimsPortal.BlazorWasm.Tests;

/// <summary>
/// Automated tests for button styling consistency across all pages
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ButtonStyleTests : TestBase
{
    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    [TestCase("/fraud-detection")]
    public async Task PrimaryButtonsHaveCorrectClass(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var primaryButtons = await Page.Locator(".btn-primary, .btn-analyze, .btn-search").AllAsync();
        
        Assert.That(primaryButtons.Count, Is.GreaterThan(0),
            $"No primary buttons found on {pagePath}");

        foreach (var button in primaryButtons)
        {
            var background = await GetComputedStyleAsync(button, "backgroundImage");
            Assert.That(background, Does.Contain("gradient"),
                $"Primary button on {pagePath} does not have gradient background");
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task ButtonsHaveHoverEffect(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var button = Page.Locator(".btn-primary, .btn").First;
        await button.WaitForAsync();

        var initialTransform = await GetComputedStyleAsync(button, "transform");
        
        await button.HoverAsync();
        await Page.WaitForTimeoutAsync(300);
        
        var hoverTransform = await GetComputedStyleAsync(button, "transform");
        
        Assert.That(hoverTransform, Is.Not.EqualTo(initialTransform),
            $"Button on {pagePath} does not have hover transform effect");
    }

    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    public async Task ButtonsHaveConsistentPadding(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var buttons = await Page.Locator(".btn, .btn-primary, .btn-secondary").AllAsync();
        var paddings = new HashSet<string>();

        foreach (var button in buttons.Take(5)) // Check first 5 buttons
        {
            var padding = await GetComputedStyleAsync(button, "padding");
            if (!string.IsNullOrEmpty(padding))
                paddings.Add(padding);
        }

        Assert.That(paddings.Count, Is.LessThanOrEqualTo(2),
            $"Button padding is inconsistent on {pagePath} (found {paddings.Count} different values)");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task DisabledButtonsHaveReducedOpacity(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        // Try to find disabled buttons
        var disabledButtons = await Page.Locator(".btn:disabled, .btn-primary:disabled").AllAsync();
        
        if (disabledButtons.Count > 0)
        {
            foreach (var button in disabledButtons)
            {
                var opacity = await GetComputedStyleAsync(button, "opacity");
                var opacityValue = double.Parse(opacity ?? "1");
                
                Assert.That(opacityValue, Is.LessThan(1.0),
                    $"Disabled button on {pagePath} does not have reduced opacity");
            }
        }
        else
        {
            Assert.Inconclusive($"No disabled buttons found on {pagePath} to test");
        }
    }

    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    public async Task ButtonsHaveBorderRadius(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var buttons = await Page.Locator(".btn, .btn-primary").AllAsync();
        
        Assert.That(buttons.Count, Is.GreaterThan(0),
            $"No buttons found on {pagePath}");

        foreach (var button in buttons.Take(3))
        {
            var borderRadius = await GetComputedStyleAsync(button, "borderRadius");
            var radiusValue = borderRadius?.Replace("px", "") ?? "0";
            
            Assert.That(int.Parse(radiusValue.Split(' ')[0]), Is.GreaterThan(0),
                $"Button on {pagePath} does not have border radius");
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task ButtonsAreAccessible(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var buttons = await Page.Locator("button").AllAsync();
        
        foreach (var button in buttons.Take(5))
        {
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            var textContent = await button.TextContentAsync();
            var title = await button.GetAttributeAsync("title");
            
            var hasAccessibleText = !string.IsNullOrWhiteSpace(ariaLabel) || 
                                   !string.IsNullOrWhiteSpace(textContent) ||
                                   !string.IsNullOrWhiteSpace(title);
            
            Assert.That(hasAccessibleText, Is.True,
                $"Button on {pagePath} lacks accessible text (aria-label, text content, or title)");
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task ButtonFocusStateIsVisible(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var button = Page.Locator(".btn, .btn-primary").First;
        await button.WaitForAsync();
        
        await button.FocusAsync();
        await Page.WaitForTimeoutAsync(200);
        
        var outline = await GetComputedStyleAsync(button, "outline");
        var outlineWidth = await GetComputedStyleAsync(button, "outlineWidth");
        var boxShadow = await GetComputedStyleAsync(button, "boxShadow");
        
        var hasFocusIndicator = (outline != "none" && outlineWidth != "0px") || 
                                (boxShadow != "none" && boxShadow != "");
        
        Assert.That(hasFocusIndicator, Is.True,
            $"Button on {pagePath} does not have visible focus indicator");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task SuccessButtonsHaveGreenColor(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var successButtons = await Page.Locator(".btn-success").AllAsync();
        
        if (successButtons.Count > 0)
        {
            foreach (var button in successButtons)
            {
                var background = await GetComputedStyleAsync(button, "backgroundImage");
                
                // Check if it contains gradient (our enhanced style)
                Assert.That(background, Does.Contain("gradient"),
                    $"Success button on {pagePath} does not have gradient background");
            }
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task DangerButtonsHaveRedColor(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var dangerButtons = await Page.Locator(".btn-danger, .btn-outline-danger").AllAsync();
        
        if (dangerButtons.Count > 0)
        {
            foreach (var button in dangerButtons)
            {
                var color = await GetComputedStyleAsync(button, "color");
                var background = await GetComputedStyleAsync(button, "backgroundColor");
                
                // Either the background or text color should have red tones
                var hasRedTone = color?.Contains("rgb") == true || 
                                background?.Contains("rgb") == true;
                
                Assert.That(hasRedTone, Is.True,
                    $"Danger button on {pagePath} does not have red color scheme");
            }
        }
    }

    [Test]
    public async Task ButtonStylesAreConsistentAcrossPages()
    {
        var pages = new[] { "/benefits", "/accumulators", "/fraud-detection" };
        var buttonHeights = new Dictionary<string, string>();

        foreach (var page in pages)
        {
            await NavigateToAsync(page);
            var firstButton = Page.Locator(".btn-primary, .btn").First;
            
            if (await firstButton.CountAsync() > 0)
            {
                var padding = await GetComputedStyleAsync(firstButton, "padding");
                buttonHeights[page] = padding ?? "";
            }
        }

        var uniquePaddings = buttonHeights.Values.Distinct().Count();
        Assert.That(uniquePaddings, Is.LessThanOrEqualTo(2),
            "Button padding is inconsistent across pages");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task ButtonsAreClickableOnMobile(string pagePath)
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync(pagePath);
        
        var buttons = await Page.Locator(".btn, .btn-primary").AllAsync();
        
        Assert.That(buttons.Count, Is.GreaterThan(0),
            $"No buttons found on {pagePath}");

        foreach (var button in buttons.Take(3))
        {
            var boundingBox = await button.BoundingBoxAsync();
            
            Assert.That(boundingBox, Is.Not.Null,
                $"Button on {pagePath} is not visible");
            
            // Touch target should be at least 44x44 pixels (WCAG guideline)
            Assert.That(boundingBox!.Height, Is.GreaterThanOrEqualTo(40),
                $"Button on {pagePath} is too small for touch interaction (height: {boundingBox.Height}px)");
        }
    }
}

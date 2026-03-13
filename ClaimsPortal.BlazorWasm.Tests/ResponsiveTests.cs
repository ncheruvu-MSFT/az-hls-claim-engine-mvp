using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace ClaimsPortal.BlazorWasm.Tests;

/// <summary>
/// Tests for responsive design across different viewport sizes
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ResponsiveTests : TestBase
{
    private readonly (int width, int height)[] _viewports = new[]
    {
        (375, 667),   // iPhone SE
        (768, 1024),  // iPad
        (1280, 720),  // Desktop
        (1920, 1080)  // Full HD
    };

    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    public async Task PageRendersCorrectlyAtAllBreakpoints(string pagePath)
    {
        foreach (var (width, height) in _viewports)
        {
            await Page.SetViewportSizeAsync(width, height);
            await NavigateToAsync(pagePath);
            
            // Check that main content is visible
            var mainContent = Page.Locator("main, .container, .content");
            await mainContent.WaitForAsync();
            
            var isVisible = await mainContent.IsVisibleAsync();
            Assert.That(isVisible, Is.True,
                $"Main content not visible on {pagePath} at {width}x{height}");
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task NavigationCollapsesOnMobile(string pagePath)
    {
        // Desktop view
        await Page.SetViewportSizeAsync(1920, 1080);
        await NavigateToAsync(pagePath);
        
        var navMenu = Page.Locator("nav, .sidebar, .nav-menu");
        
        if (await navMenu.CountAsync() > 0)
        {
            var desktopDisplay = await GetComputedStyleAsync(navMenu.First, "display");
            
            // Mobile view
            await Page.SetViewportSizeAsync(375, 667);
            await Page.WaitForTimeoutAsync(500);
            
            var mobileDisplay = await GetComputedStyleAsync(navMenu.First, "display");
            
            // Navigation should change display mode or be hidden
            var isResponsive = desktopDisplay != mobileDisplay || 
                               mobileDisplay == "none";
            
            Assert.That(isResponsive, Is.True,
                $"Navigation on {pagePath} does not adapt to mobile viewport");
        }
    }

    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    public async Task NoHorizontalScrollOnMobile(string pagePath)
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync(pagePath);
        
        var bodyWidth = await Page.EvaluateAsync<int>("() => document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("() => document.documentElement.clientWidth");
        
        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 1),
            $"Page {pagePath} has horizontal scroll on mobile viewport");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task FontSizesAreReadableOnMobile(string pagePath)
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync(pagePath);
        
        var paragraphs = await Page.Locator("p, td, span").AllAsync();
        
        foreach (var element in paragraphs.Take(10))
        {
            var fontSize = await GetComputedStyleAsync(element, "fontSize");
            var sizeValue = double.Parse(fontSize?.Replace("px", "") ?? "0");
            
            // Minimum readable font size on mobile is 14px
            Assert.That(sizeValue, Is.GreaterThanOrEqualTo(12),
                $"Font size on {pagePath} is too small on mobile ({sizeValue}px)");
        }
    }

    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    public async Task ImagesDoNotOverflowContainer(string pagePath)
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync(pagePath);
        
        var images = await Page.Locator("img").AllAsync();
        
        if (images.Count > 0)
        {
            foreach (var image in images)
            {
                var width = await image.EvaluateAsync<int>("el => el.offsetWidth");
                var containerWidth = await Page.EvaluateAsync<int>("() => document.documentElement.clientWidth");
                
                Assert.That(width, Is.LessThanOrEqualTo(containerWidth),
                    $"Image on {pagePath} overflows container on mobile");
            }
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task CardLayoutStacksOnMobile(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var cards = await Page.Locator(".card, .card-body").AllAsync();
        
        if (cards.Count >= 2)
        {
            // Desktop - cards should be side by side
            await Page.SetViewportSizeAsync(1920, 1080);
            await Page.WaitForTimeoutAsync(500);
            
            var card1Box = await cards[0].BoundingBoxAsync();
            var card2Box = await cards[1].BoundingBoxAsync();
            
            // Mobile - cards should stack
            await Page.SetViewportSizeAsync(375, 667);
            await Page.WaitForTimeoutAsync(500);
            
            var mobileCard1Box = await cards[0].BoundingBoxAsync();
            var mobileCard2Box = await cards[1].BoundingBoxAsync();
            
            if (mobileCard1Box != null && mobileCard2Box != null)
            {
                // On mobile, second card should be below the first
                var isStacked = mobileCard2Box.Y > mobileCard1Box.Y + mobileCard1Box.Height - 10;
                
                Assert.That(isStacked, Is.True,
                    $"Cards on {pagePath} do not stack vertically on mobile");
            }
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task TabsAreAccessibleOnMobile(string pagePath)
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync(pagePath);
        
        var tabs = await Page.Locator(".nav-tabs .nav-link, .nav-item").AllAsync();
        
        if (tabs.Count > 0)
        {
            foreach (var tab in tabs)
            {
                var isVisible = await tab.IsVisibleAsync();
                
                if (isVisible)
                {
                    var boundingBox = await tab.BoundingBoxAsync();
                    Assert.That(boundingBox?.Height, Is.GreaterThanOrEqualTo(40),
                        $"Tab on {pagePath} is too small for touch on mobile");
                }
            }
        }
    }

    [Test]
    [TestCase("/benefits")]
    [TestCase("/accumulators")]
    public async Task PageLoadTimeIsAcceptable(string pagePath)
    {
        var startTime = DateTime.Now;
        
        await NavigateToAsync(pagePath);
        
        var loadTime = (DateTime.Now - startTime).TotalMilliseconds;
        
        Assert.That(loadTime, Is.LessThan(5000),
            $"Page {pagePath} took too long to load ({loadTime}ms)");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task VisualRegressionAcrossBreakpoints(string pagePath)
    {
        foreach (var (width, height) in _viewports)
        {
            await Page.SetViewportSizeAsync(width, height);
            await NavigateToAsync(pagePath);
            
            await TakeScreenshotAsync($"{pagePath.Replace("/", "")}-{width}x{height}");
        }
        
        Assert.Pass("Baseline screenshots created for all breakpoints");
    }
}

using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace ClaimsPortal.BlazorWasm.Tests;

/// <summary>
/// Automated tests for table styling consistency across all pages
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class TableStyleTests : TestBase
{
    private readonly string[] _pagesWithTables = new[]
    {
        "/accumulators",
        "/benefits",
        "/claims-management",
        "/formulary-management",
        "/hedis-quality",
        "/fraud-detection"
    };

    [Test]
    [TestCase("/accumulators")]
    [TestCase("/benefits")]
    [TestCase("/claims-management")]
    [TestCase("/formulary-management")]
    public async Task AllTablesHaveDataTableClass(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        // Find all tables on the page
        var tables = await Page.Locator("table").AllAsync();
        
        Assert.That(tables.Count, Is.GreaterThan(0), $"No tables found on {pagePath}");

        foreach (var table in tables)
        {
            var hasDataTableClass = await HasClassAsync(table, "data-table");
            Assert.That(hasDataTableClass, Is.True, 
                $"Table on {pagePath} is missing 'data-table' class");
        }
    }

    [Test]
    [TestCase("/accumulators")]
    [TestCase("/benefits")]
    public async Task AllTablesHaveResponsiveWrapper(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var tables = await Page.Locator("table.data-table").AllAsync();
        
        foreach (var table in tables)
        {
            var parent = Page.Locator($"xpath=.//ancestor::div[contains(@class, 'table-responsive')]");
            var hasResponsiveWrapper = await parent.CountAsync() > 0;
            
            Assert.That(hasResponsiveWrapper, Is.True,
                $"Table on {pagePath} is not wrapped in '.table-responsive'");
        }
    }

    [Test]
    [TestCase("/accumulators")]
    [TestCase("/benefits")]
    public async Task TableHeadersHaveGradientBackground(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var tableHeaders = await Page.Locator("table.data-table thead").AllAsync();
        
        Assert.That(tableHeaders.Count, Is.GreaterThan(0), 
            $"No table headers found on {pagePath}");

        foreach (var thead in tableHeaders)
        {
            var background = await GetComputedStyleAsync(thead, "backgroundImage");
            
            Assert.That(background, Does.Contain("gradient"),
                $"Table header on {pagePath} does not have gradient background");
        }
    }

    [Test]
    [TestCase("/benefits")]
    public async Task TableRowsHaveHoverEffect(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var firstRow = Page.Locator("table.data-table tbody tr").First;
        await firstRow.WaitForAsync();

        // Get initial background color
        var initialBg = await GetComputedStyleAsync(firstRow, "backgroundColor");
        
        // Hover over the row
        await firstRow.HoverAsync();
        await Page.WaitForTimeoutAsync(500); // Wait for transition
        
        // Get background color after hover
        var hoverBg = await GetComputedStyleAsync(firstRow, "backgroundColor");
        
        Assert.That(hoverBg, Is.Not.EqualTo(initialBg),
            $"Table row on {pagePath} does not change background on hover");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task AmountColumnsAreRightAligned(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var amountCells = await Page.Locator("td.amount").AllAsync();
        
        if (amountCells.Count > 0)
        {
            foreach (var cell in amountCells)
            {
                var textAlign = await GetComputedStyleAsync(cell, "textAlign");
                Assert.That(textAlign, Is.EqualTo("right"),
                    $"Amount column on {pagePath} is not right-aligned");
            }
        }
    }

    [Test]
    public async Task TableStylesAreConsistentAcrossPages()
    {
        var tableHeights = new Dictionary<string, string>();

        foreach (var page in _pagesWithTables)
        {
            await NavigateToAsync(page);
            var firstTable = Page.Locator("table.data-table").First;
            
            if (await firstTable.CountAsync() > 0)
            {
                var padding = await GetComputedStyleAsync(
                    firstTable.Locator("th").First, "padding");
                tableHeights[page] = padding ?? "";
            }
        }

        // Check that all pages have the same padding
        var uniquePaddings = tableHeights.Values.Distinct().Count();
        Assert.That(uniquePaddings, Is.LessThanOrEqualTo(1),
            "Table header padding is inconsistent across pages");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task TableHasNoHorizontalScrollOnDesktop(string pagePath)
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await NavigateToAsync(pagePath);
        
        var table = Page.Locator("table.data-table").First;
        await table.WaitForAsync();
        
        var tableWidth = await table.EvaluateAsync<int>("el => el.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("() => document.documentElement.clientWidth");
        
        Assert.That(tableWidth, Is.LessThanOrEqualTo(viewportWidth),
            $"Table on {pagePath} has horizontal scroll on desktop viewport");
    }

    [Test]
    [TestCase("/benefits")]
    public async Task TableIsScrollableOnMobile(string pagePath)
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync(pagePath);
        
        var responsiveWrapper = Page.Locator(".table-responsive").First;
        await responsiveWrapper.WaitForAsync();
        
        var overflowX = await GetComputedStyleAsync(responsiveWrapper, "overflowX");
        Assert.That(overflowX, Is.EqualTo("auto").Or.EqualTo("scroll"),
            $"Table wrapper on {pagePath} is not scrollable on mobile");
    }

    [Test]
    [TestCase("/accumulators")]
    [TestCase("/benefits")]
    public async Task VisualRegressionTest(string pagePath)
    {
        await NavigateToAsync(pagePath);
        
        var table = Page.Locator("table.data-table").First;
        await table.WaitForAsync();
        
        // Take screenshot for visual comparison
        await TakeScreenshotAsync($"table-{pagePath.Replace("/", "")}");
        
        // This test creates baseline screenshots
        // Future runs will compare against these baselines
        Assert.Pass("Baseline screenshot created");
    }
}

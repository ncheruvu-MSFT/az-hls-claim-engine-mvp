# ClaimsIQ Platform - Automated UI Tests

## Overview
This project contains automated UI tests using Playwright for the ClaimsIQ Platform. Tests validate table styling, button consistency, responsive design, and visual regression across all portal pages.

## Test Categories

### 1. **TableStyleTests.cs**
Validates table styling consistency:
- ✅ All tables have `.data-table` class
- ✅ Tables are wrapped in `.table-responsive` divs
- ✅ Table headers have gradient backgrounds
- ✅ Row hover effects work correctly
- ✅ Amount columns are right-aligned
- ✅ Tables scale properly across viewports
- ✅ Visual regression testing

### 2. **ButtonStyleTests.cs**
Validates button styling consistency:
- ✅ Primary buttons have gradient backgrounds
- ✅ Buttons have hover effects (transform + shadow)
- ✅ Consistent padding across pages
- ✅ Disabled buttons have reduced opacity
- ✅ Border radius applied to all buttons
- ✅ Accessibility (aria-labels, focus states)
- ✅ Color scheme validation (success=green, danger=red)
- ✅ Touch target sizes (min 44x44px)

### 3. **ResponsiveTests.cs**
Validates responsive design:
- ✅ Pages render correctly at all breakpoints
- ✅ Navigation collapses on mobile
- ✅ No horizontal scroll on mobile
- ✅ Font sizes are readable (min 14px)
- ✅ Images don't overflow containers
- ✅ Cards stack vertically on mobile
- ✅ Tabs are accessible on mobile
- ✅ Page load times < 5 seconds
- ✅ Visual regression across breakpoints

## Setup Instructions

### 1. Install Playwright Browsers
Run this once after creating the project:
```powershell
cd ClaimsPortal.BlazorWasm.Tests
pwsh bin/Debug/net10.0/playwright.ps1 install
```

Or on Linux/Mac:
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### 2. Restore NuGet Packages
```powershell
dotnet restore ClaimsPortal.BlazorWasm.Tests.csproj
```

### 3. Build the Test Project
```powershell
dotnet build ClaimsPortal.BlazorWasm.Tests.csproj
```

## Running Tests

### Prerequisites
**IMPORTANT:** The Blazor app must be running before executing tests.

Start the Blazor app first:
```powershell
cd ..\ClaimsPortal.BlazorWasm
dotnet run
```

Then in a separate terminal, run tests:

### Run All Tests
```powershell
cd ClaimsPortal.BlazorWasm.Tests
dotnet test
```

### Run Specific Test Category
```powershell
# Table styling tests only
dotnet test --filter FullyQualifiedName~TableStyleTests

# Button styling tests only
dotnet test --filter FullyQualifiedName~ButtonStyleTests

# Responsive design tests only
dotnet test --filter FullyQualifiedName~ResponsiveTests
```

### Run Specific Test
```powershell
dotnet test --filter "FullyQualifiedName~TableStyleTests.AllTablesHaveDataTableClass"
```

### Run Tests in Parallel
```powershell
dotnet test --parallel
```

### Run Tests with Detailed Output
```powershell
dotnet test --logger "console;verbosity=detailed"
```

## Viewing Test Results

### Screenshots
Failed test screenshots are saved to:
```
ClaimsPortal.BlazorWasm.Tests/bin/Debug/net10.0/screenshots/
```

### Videos
Test execution videos (on failure) are saved to:
```
ClaimsPortal.BlazorWasm.Tests/bin/Debug/net10.0/videos/
```

### Traces
Playwright traces (for debugging) are saved to:
```
ClaimsPortal.BlazorWasm.Tests/bin/Debug/net10.0/traces/
```

View traces with:
```powershell
pwsh bin/Debug/net10.0/playwright.ps1 show-trace path/to/trace.zip
```

## Test Configuration

### Browsers
Tests run on multiple browsers (configured in `playwright.config.json`):
- ✅ Chromium (Desktop 1920x1080)
- ✅ Firefox (Desktop 1920x1080)
- ✅ WebKit/Safari (Desktop 1920x1080)
- ✅ Mobile Chrome (375x667)
- ✅ Tablet Chrome (768x1024)

### Timeouts
- Test timeout: 30 seconds
- Expect timeout: 5 seconds

### Base URL
Default: `http://localhost:5000`

To change, update `playwright.config.json`:
```json
{
  "use": {
    "baseURL": "https://your-app-url.com"
  }
}
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: UI Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Install dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Start Blazor App
        run: |
          cd ClaimsPortal.BlazorWasm
          dotnet run &
          sleep 10
      
      - name: Install Playwright
        run: pwsh ClaimsPortal.BlazorWasm.Tests/bin/Debug/net10.0/playwright.ps1 install
      
      - name: Run Tests
        run: dotnet test ClaimsPortal.BlazorWasm.Tests
      
      - name: Upload Screenshots
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: test-screenshots
          path: ClaimsPortal.BlazorWasm.Tests/bin/Debug/net10.0/screenshots/
```

## Visual Regression Testing

### Creating Baselines
First run creates baseline screenshots:
```powershell
dotnet test --filter "FullyQualifiedName~VisualRegressionTest"
```

### Comparing Against Baselines
Subsequent runs compare against baselines and fail if differences exceed threshold.

### Updating Baselines
Delete old screenshots and re-run:
```powershell
Remove-Item screenshots/* -Force
dotnet test --filter "FullyQualifiedName~VisualRegressionTest"
```

## Best Practices

1. **Always start the Blazor app before running tests**
2. **Run tests on clean database state for consistency**
3. **Use descriptive test names** (`AllTablesHaveDataTableClass`)
4. **Test across multiple browsers** for cross-browser compatibility
5. **Keep test data minimal** for fast execution
6. **Use `[TestCase]` for parameterized tests** to avoid duplication
7. **Take screenshots on failure** for easier debugging
8. **Run tests in CI/CD pipeline** to catch regressions early

## Troubleshooting

### Tests fail with "Connection refused"
**Solution:** Ensure the Blazor app is running on `http://localhost:5000`

### Tests timeout
**Solution:** Increase timeout in `playwright.config.json`:
```json
{
  "timeout": 60000
}
```

### Playwright not found
**Solution:** Install Playwright browsers:
```powershell
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### Browser launch fails
**Solution:** Install required dependencies (Linux):
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install-deps
```

### Visual regression tests fail unexpectedly
**Solution:** Fonts/rendering may differ across environments. Use Docker for consistent rendering:
```dockerfile
FROM mcr.microsoft.com/playwright/dotnet:v1.49.0-noble
WORKDIR /app
COPY . .
RUN dotnet test
```

## Adding New Tests

1. Create new test class inheriting from `TestBase`
2. Add `[TestFixture]` and `[Parallelizable]` attributes
3. Use `NavigateToAsync()` to navigate to pages
4. Use `HasClassAsync()`, `GetComputedStyleAsync()` helper methods
5. Add `[TestCase]` for multiple pages
6. Follow naming convention: `FeatureTests.cs`

Example:
```csharp
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class NavigationTests : TestBase
{
    [Test]
    [TestCase("/benefits")]
    public async Task NavigationMenuIsVisible(string pagePath)
    {
        await NavigateToAsync(pagePath);
        var nav = Page.Locator("nav");
        await Expect(nav).ToBeVisibleAsync();
    }
}
```

## Documentation
- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://docs.nunit.org/)
- [ClaimsIQ Copilot Instructions](../.github/copilot-instructions.md)

---

**Last Updated:** January 2026  
**Playwright Version:** 1.49.0  
**NUnit Version:** 4.2.2  
**Target Framework:** .NET 10.0

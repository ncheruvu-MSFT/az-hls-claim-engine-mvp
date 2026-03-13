# UI/UX Testing Agent for ClaimsIQ Platform

## Agent Mission
Validate UI consistency, verify Bootstrap 5.3 class usage, check responsive design implementation, and ensure accessibility standards across all Blazor pages.

## Activation Triggers
- Keywords: `@ui-agent`, `@style-check`, `@design-validate`, `check styling`, `validate design`, `test ui`, `responsive check`
- File patterns: `*.razor`, `*.css`, `enhanced-components.css`, `responsive.css`
- Context: User mentions "inconsistent styling", "responsive", "buttons", "tables", "accessibility"

## Specialized Knowledge

### 1. Bootstrap 5.3 Standards
- **Grid System**: `.container`, `.row`, `.col-*`, `.col-sm-*`, `.col-md-*`, `.col-lg-*`, `.col-xl-*`
- **Tables**: `.table`, `.table-responsive`, `.table-striped`, `.table-hover`, `.table-bordered`
- **Buttons**: `.btn`, `.btn-primary`, `.btn-secondary`, `.btn-success`, `.btn-danger`, `.btn-warning`, `.btn-info`
- **Cards**: `.card`, `.card-header`, `.card-body`, `.card-footer`, `.card-title`
- **Navigation**: `.nav`, `.nav-tabs`, `.nav-pills`, `.nav-item`, `.nav-link`
- **Forms**: `.form-control`, `.form-select`, `.form-label`, `.form-group`
- **Utilities**: `.d-flex`, `.justify-content-*`, `.align-items-*`, `.mb-*`, `.mt-*`, `.p-*`, `.text-*`

### 2. ClaimsIQ Design System (enhanced-components.css)
#### Tables
```css
/* Required classes */
.data-table                    /* Base table class */
.table-responsive             /* Wrapper for horizontal scroll */
.amount                       /* Right-aligned currency columns */
.centered                     /* Centered text columns */

/* Visual requirements */
- Gradient header: linear-gradient(135deg, #667eea 0%, #764ba2 100%)
- Hover effect on tbody rows
- Sticky header (position: sticky)
- Border radius: var(--radius-md)
- Box shadow: var(--shadow)
```

#### Buttons
```css
/* Button classes */
.btn-primary, .btn-analyze, .btn-search, .btn-ask  /* Primary actions */
.btn-secondary                                       /* Secondary actions */
.btn-success                                         /* Success/confirm actions */
.btn-danger                                          /* Destructive actions */
.btn-warning                                         /* Warning actions */
.btn-outline-*                                       /* Outline variants */
.btn-sm, .btn-lg                                     /* Size variants */
.btn-icon                                            /* Icon-only buttons */

/* Visual requirements */
- Gradient backgrounds (not solid colors)
- Transform on hover: translateY(-2px)
- Box shadow elevation on hover
- Border radius: var(--radius-md)
- Disabled state: opacity 0.5
- Loading state with spinner animation
```

#### Badges
```css
.badge.bg-primary              /* Blue gradient */
.badge.bg-success              /* Green gradient */
.badge.bg-warning              /* Orange gradient */
.badge.bg-danger               /* Red gradient */
.badge.bg-info                 /* Teal gradient */
```

#### Form Controls
```css
.form-control                  /* Input fields */
.form-select                   /* Select dropdowns */
.form-label                    /* Field labels */

/* Visual requirements */
- Border: 2px solid var(--border-color)
- Focus: border-color #667eea, box-shadow with glow
- Transform on focus: translateY(-1px)
```

### 3. Responsive Breakpoints
```css
/* Mobile: 0-575px */
- Single column layouts
- Stacked cards
- Collapsible navigation
- Font size: min 14px
- Touch targets: min 44x44px

/* Tablet: 576-991px */
- Two-column layouts
- Larger font sizes
- Expanded navigation

/* Desktop: 992px+ */
- Multi-column layouts
- Full navigation
- Optimized spacing
```

### 4. Accessibility (WCAG 2.1 AA)
- **Color Contrast**: Minimum 4.5:1 for text, 3:1 for UI components
- **Focus Indicators**: Visible outline (3px solid #667eea) with 2px offset
- **Keyboard Navigation**: All interactive elements accessible via Tab key
- **Touch Targets**: Minimum 44x44 pixels (iOS guidelines)
- **Screen Readers**: aria-label, aria-labelledby, role attributes
- **Semantic HTML**: Proper heading hierarchy (h1 → h6), landmarks (nav, main, aside)

### 5. CSS Variables (app.css & responsive.css)
```css
:root {
  /* Colors */
  --azure-primary: #0078d4;
  --primary-hover: #106ebe;
  --success-color: #107c10;
  --warning-color: #ff8c00;
  --error-color: #d13438;
  --info-color: #00bcf2;
  
  /* Backgrounds */
  --background: #f5f5f5;
  --card-bg: #ffffff;
  --border-color: #e0e0e0;
  
  /* Text */
  --text-primary: #323130;
  --text-secondary: #605e5c;
  
  /* Shadows */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  --shadow-md: 0 4px 12px rgba(0, 0, 0, 0.12);
  --shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.15);
  --shadow-hover: 0 4px 16px rgba(0, 0, 0, 0.15);
  
  /* Border Radius */
  --radius-sm: 4px;
  --radius-md: 8px;
  --radius-lg: 12px;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  
  /* Transitions */
  --transition-fast: 0.2s ease;
  --transition-normal: 0.3s ease;
  --transition-slow: 0.5s ease;
}
```

## Code Patterns to Detect

### ✅ CORRECT Patterns

#### Table Implementation
```razor
<div class="table-responsive">
    <table class="data-table">
        <thead>
            <tr>
                <th>Column 1</th>
                <th>Column 2</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in items)
            {
                <tr>
                    <td>@item.Name</td>
                    <td class="amount">@($"{item.Value:N2}")</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

#### Button Implementation
```razor
<button class="btn-primary" @onclick="HandleClick">
    Primary Action
</button>

<button class="btn-outline-danger" @onclick="HandleDelete">
    Delete
</button>
```

#### Form Implementation
```razor
<div class="form-group">
    <label class="form-label">Member ID</label>
    <input type="text" class="form-control" @bind="memberId" />
</div>
```

#### Responsive Card Grid
```razor
<div class="row">
    <div class="col-12 col-md-6 col-lg-4">
        <div class="card">
            <div class="card-header">
                <h3>Title</h3>
            </div>
            <div class="card-body">
                Content
            </div>
        </div>
    </div>
</div>
```

### ❌ INCORRECT Patterns (Anti-Patterns)

#### Missing Responsive Wrapper
```razor
<!-- ❌ BAD: Table without responsive wrapper -->
<table class="data-table">
    <thead>...</thead>
    <tbody>...</tbody>
</table>

<!-- ✅ GOOD: Add responsive wrapper -->
<div class="table-responsive">
    <table class="data-table">...</table>
</div>
```

#### Using Inline Styles
```razor
<!-- ❌ BAD: Inline styles -->
<button style="background-color: blue; color: white;" @onclick="HandleClick">
    Click Me
</button>

<!-- ✅ GOOD: Use CSS classes -->
<button class="btn-primary" @onclick="HandleClick">
    Click Me
</button>
```

#### Incorrect Number Formatting
```razor
<!-- ❌ BAD: ToString in string interpolation displays literally -->
<td class="amount">$@value.ToString("N2")</td>

<!-- ✅ GOOD: Use Razor format specifier -->
<td class="amount">@($"{value:N2}")</td>
```

#### Missing Accessibility Attributes
```razor
<!-- ❌ BAD: Button without accessible text -->
<button class="btn-icon" @onclick="HandleEdit">
    <i class="bi-pencil"></i>
</button>

<!-- ✅ GOOD: Add aria-label -->
<button class="btn-icon" @onclick="HandleEdit" aria-label="Edit item">
    <i class="bi-pencil"></i>
</button>
```

#### Custom CSS Instead of Bootstrap
```razor
<!-- ❌ BAD: Custom grid classes -->
<div class="custom-grid">
    <div class="custom-col">Content</div>
</div>

<!-- ✅ GOOD: Use Bootstrap grid -->
<div class="row">
    <div class="col-md-6">Content</div>
</div>
```

#### Non-Responsive Layout
```razor
<!-- ❌ BAD: Fixed width -->
<div style="width: 1200px;">
    <div style="float: left; width: 300px;">Sidebar</div>
    <div style="float: left; width: 900px;">Content</div>
</div>

<!-- ✅ GOOD: Responsive grid -->
<div class="row">
    <div class="col-lg-3">Sidebar</div>
    <div class="col-lg-9">Content</div>
</div>
```

## Validation Checklist

### Page-Level Checks
- [ ] All tables have `.data-table` class
- [ ] All tables wrapped in `.table-responsive` div
- [ ] All buttons use `.btn-*` classes (no inline styles)
- [ ] All forms use `.form-control` and `.form-label`
- [ ] All cards use `.card`, `.card-header`, `.card-body`
- [ ] Responsive grid uses `.row` and `.col-*` classes
- [ ] No fixed pixel widths (use % or Bootstrap columns)
- [ ] Page header has gradient background
- [ ] Navigation uses `.nav-tabs` or `.nav-pills`
- [ ] Currency values use `.amount` class with right alignment
- [ ] All interactive elements have hover states
- [ ] Focus states are visible (keyboard navigation)

### Accessibility Checks
- [ ] All buttons have accessible text (aria-label or text content)
- [ ] All form inputs have associated labels
- [ ] Color contrast meets WCAG AA (4.5:1 for text)
- [ ] Focus indicators visible (3px outline)
- [ ] Touch targets ≥ 44x44 pixels
- [ ] Proper heading hierarchy (h1, h2, h3, etc.)
- [ ] Images have alt text
- [ ] Links have descriptive text (not "click here")

### Responsive Design Checks
- [ ] Layout adapts at breakpoints (576px, 768px, 992px, 1200px)
- [ ] No horizontal scroll on mobile (375px width)
- [ ] Font sizes readable on mobile (≥ 14px)
- [ ] Touch targets large enough (≥ 44x44px)
- [ ] Images scale with container (max-width: 100%)
- [ ] Cards stack vertically on mobile
- [ ] Navigation collapses or adapts on mobile
- [ ] Tables scrollable horizontally on mobile

### Performance Checks
- [ ] Page loads in < 5 seconds
- [ ] Images optimized (WebP format, lazy loading)
- [ ] Minimal inline styles (use CSS classes)
- [ ] CSS animations use `transform` (GPU-accelerated)
- [ ] Transitions use `will-change` for performance

## Testing Commands

### Run UI Tests
```powershell
# All tests
dotnet test ClaimsPortal.BlazorWasm.Tests

# Table styling tests
dotnet test --filter "FullyQualifiedName~TableStyleTests"

# Button styling tests
dotnet test --filter "FullyQualifiedName~ButtonStyleTests"

# Responsive design tests
dotnet test --filter "FullyQualifiedName~ResponsiveTests"
```

### Visual Inspection (Manual)
1. Open browser DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M)
3. Test at multiple viewport sizes:
   - Mobile: 375x667 (iPhone SE)
   - Tablet: 768x1024 (iPad)
   - Desktop: 1920x1080 (Full HD)
4. Check accessibility:
   - Run Lighthouse audit (Performance, Accessibility, Best Practices)
   - Test keyboard navigation (Tab key)
   - Check color contrast (DevTools > Inspect > Accessibility)

## Agent Response Templates

### Pattern 1: Table Validation
```
🔍 **UI/UX Check: Table Styling**

I found [X] tables in [FileName.razor]:
✅ Lines [XX-XX]: Table uses `.data-table` and `.table-responsive`
❌ Lines [YY-YY]: Table missing `.table-responsive` wrapper

**Recommended fix:**
- Wrap table in `<div class="table-responsive">...</div>`
- Ensure header has gradient: `<thead>` should use `.data-table thead`
- Add `.amount` class to currency columns
```

### Pattern 2: Button Validation
```
🔍 **UI/UX Check: Button Styling**

I found [X] buttons in [FileName.razor]:
✅ Lines [XX-XX]: Button uses `.btn-primary` with proper gradient
❌ Lines [YY-YY]: Button uses inline styles instead of CSS class

**Recommended fix:**
- Replace `style="..."` with `.btn-primary`, `.btn-secondary`, etc.
- Add `aria-label` for icon-only buttons
- Ensure hover state works (check if button is inside form that prevents events)
```

### Pattern 3: Responsive Design Validation
```
🔍 **UI/UX Check: Responsive Design**

I checked responsive layout in [FileName.razor]:
✅ Uses Bootstrap grid (`.row`, `.col-*`)
❌ Fixed width detected: `style="width: 1200px;"` on line [XX]

**Recommended fix:**
- Remove fixed widths
- Use Bootstrap columns: `.col-12 .col-md-6 .col-lg-4`
- Test at mobile breakpoint (375px width)
```

### Pattern 4: Accessibility Validation
```
🔍 **UI/UX Check: Accessibility**

I checked accessibility in [FileName.razor]:
✅ All form inputs have labels
❌ Button on line [XX] missing `aria-label` (icon-only)
❌ Color contrast may not meet WCAG AA (text on gradient)

**Recommended fix:**
- Add `aria-label="Descriptive action"` to icon buttons
- Test color contrast: Use DevTools > Inspect > Accessibility
- Ensure focus indicators visible (outline or box-shadow)
```

## Integration with Playwright Tests

The UI/UX agent works alongside automated Playwright tests:
- **Agent**: Identifies issues during development (code review)
- **Playwright**: Validates fixes in CI/CD pipeline (automated regression)

### Workflow
1. Developer writes Razor page
2. **@ui-agent** validates code patterns (immediate feedback)
3. Developer fixes issues
4. **Playwright tests** run to confirm (automated validation)
5. Visual regression tests capture screenshots (baseline for future changes)

## Common Scenarios

### Scenario 1: User adds new page with tables
**User:** "I created a new page `Reports.razor` with tables but styling looks off"

**Agent Response:**
```
🔍 Checking Reports.razor for styling issues...

Found 3 styling issues:
1. ❌ Line 25: Table missing `.data-table` class
2. ❌ Line 24: Table not wrapped in `.table-responsive`
3. ❌ Line 45: Button uses inline style instead of `.btn-primary`

I'll fix these for you now...
```

### Scenario 2: User wants to improve button consistency
**User:** "@ui-agent check all pages for button styling consistency"

**Agent Response:**
```
🔍 Scanning all .razor files for button styling...

**Summary:**
- ✅ Benefits.razor: All buttons use proper classes
- ✅ Accumulators.razor: All buttons use proper classes
- ❌ FraudDetection.razor: 2 buttons use inline styles (lines 45, 67)
- ❌ ClaimsManagement.razor: 1 button missing hover effect (line 123)

Would you like me to fix the issues in FraudDetection.razor and ClaimsManagement.razor?
```

### Scenario 3: User wants responsive check
**User:** "@style-check responsive design on member portal"

**Agent Response:**
```
🔍 Checking responsive design in MemberPortal.razor...

**Desktop (1920px):** ✅ Looks good
**Tablet (768px):** ⚠️ Cards don't stack, causing horizontal scroll
**Mobile (375px):** ❌ Navigation doesn't collapse, font size too small

**Recommended fixes:**
1. Add responsive columns: `.col-12 .col-md-6 .col-lg-4`
2. Add collapsible navigation for mobile
3. Increase font size: min 14px for readability

Run Playwright tests to validate: `dotnet test --filter ResponsiveTests`
```

## Summary

The UI/UX Agent ensures ClaimsIQ Platform maintains:
- ✅ **Consistent styling** across all pages
- ✅ **Bootstrap 5.3 best practices**
- ✅ **Responsive design** at all breakpoints
- ✅ **Accessibility** (WCAG 2.1 AA compliance)
- ✅ **Professional healthcare UI** with gradients, shadows, hover effects
- ✅ **Automated validation** via Playwright tests

---

**Last Updated:** January 2026  
**Platform:** .NET 10 Blazor WebAssembly  
**CSS Framework:** Bootstrap 5.3 + Custom enhanced-components.css

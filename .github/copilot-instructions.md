# GitHub Copilot Instructions for ClaimsIQ Platform

## Product Overview
**ClaimsIQ Platform** - Healthcare payer operations and external portal system

### Application Structure
- **Internal Tools (Payer Operations)**: Product Configuration, Claims Engine, Claims Management, Formulary Management, Eligibility Verification, Member Search, Fraud Analytics, Quality Measures
- **External Portals**: Member Portal, Provider Portal (EPIC-style)

## Styling Guidelines

### 1. CSS Variables
Always use CSS custom properties defined in `:root`:
```css
--primary-color: #0078d4;
--primary-hover: #106ebe;
--success-color: #107c10;
--warning-color: #ff8c00;
--error-color: #d13438;
--background: #f5f5f5;
--card-bg: #ffffff;
--border-color: #e0e0e0;
--text-primary: #323130;
--text-secondary: #605e5c;
--shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
--shadow-hover: 0 4px 16px rgba(0, 0, 0, 0.15);
```

### 2. Card-Based Layouts
Use consistent card structure:
```html
<div class="card">
    <div class="card-header">
        <h3>Title</h3>
        <button class="btn-action">Action</button>
    </div>
    <div class="card-body">
        <!-- Content -->
    </div>
</div>
```

**Card CSS Pattern:**
```css
.card {
    background: var(--card-bg);
    border-radius: 8px;
    box-shadow: var(--shadow);
    padding: 1.5rem;
    margin-bottom: 1.5rem;
}

.card:hover {
    box-shadow: var(--shadow-hover);
}
```

### 3. Page Headers
Standard page header structure:
```html
<div class="page-header">
    <h3>🔍 Page Title</h3>
    <p class="subtitle">Brief description of the page</p>
</div>
```

**Page Header CSS:**
```css
.page-header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 2rem;
    border-radius: 8px;
    margin-bottom: 2rem;
}

.page-header h3 {
    margin: 0 0 0.5rem 0;
    font-size: 1.75rem;
}

.subtitle {
    margin: 0;
    opacity: 0.9;
    font-size: 1rem;
}
```

### 4. FHIR Display Guidelines

#### FHIR JSON Display
- **Default State**: ALWAYS hidden by default
- **Toggle Button**: ALWAYS provide toggle button to show/hide
- **Button Text**: "Show FHIR JSON" (hidden) / "Hide FHIR JSON" (visible)

**Implementation Pattern:**
```razor
<!-- Razor Component -->
<div class="fhir-section">
    <div class="fhir-header">
        <h4>🔗 FHIR Resource Name</h4>
        <button class="btn-toggle-fhir" @onclick="() => showFhirJson = !showFhirJson">
            @(showFhirJson ? "Hide FHIR JSON" : "Show FHIR JSON")
        </button>
    </div>
    @if (showFhirJson)
    {
        <pre class="fhir-code"><code>@fhirJsonContent</code></pre>
    }
</div>

@code {
    private bool showFhirJson = false; // ALWAYS start as false
}
```

**FHIR CSS Pattern:**
```css
.fhir-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
}

.btn-toggle-fhir {
    background: var(--primary-color);
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 6px;
    cursor: pointer;
    transition: background 0.2s ease;
}

.btn-toggle-fhir:hover {
    background: var(--primary-hover);
}

.fhir-code {
    background: #f8f9fa;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 1rem;
    overflow-x: auto;
    font-family: 'Courier New', monospace;
    font-size: 0.875rem;
}
```

### 5. Table Styling

#### Standard Table Structure
```html
<table class="data-table">
    <thead>
        <tr>
            <th>Column 1</th>
            <th>Column 2</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Data 1</td>
            <td>Data 2</td>
        </tr>
    </tbody>
</table>
```

**Table CSS Pattern:**
```css
.data-table {
    width: 100%;
    border-collapse: collapse;
    background: white;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: var(--shadow);
}

.data-table thead {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
}

.data-table th {
    padding: 1rem;
    text-align: left;
    font-weight: 600;
}

.data-table td {
    padding: 1rem;
    border-bottom: 1px solid var(--border-color);
}

.data-table tbody tr:hover {
    background: #f8f9fa;
}
```

### 6. Number Formatting in Razor

#### Currency Values
**CORRECT** - Use Razor string interpolation with format specifiers:
```razor
<td class="amount">@($"{value:N2}")</td>
<td class="amount">@($"{value:C}")</td>
```

**INCORRECT** - Do NOT use ToString() in string interpolation:
```razor
<!-- WRONG! This displays literally as "ToString('N2')" -->
<td class="amount">$@value.ToString("N2")</td>
```

#### Format Specifiers
- `N0`: Number with no decimals (e.g., 1,234)
- `N2`: Number with 2 decimals (e.g., 1,234.56)
- `C`: Currency with symbol (e.g., $1,234.56)
- `C0`: Currency with no decimals (e.g., $1,234)
- `P0`: Percentage with no decimals (e.g., 75%)
- `P2`: Percentage with 2 decimals (e.g., 75.50%)

#### Date Formatting
```razor
<td>@($"{dateValue:MM/dd/yyyy}")</td>
<td>@($"{dateValue:yyyy-MM-dd}")</td>
```

### 7. Button Styling

#### Primary Action Buttons
```css
.btn-primary, .btn-analyze, .btn-search {
    background: var(--primary-color);
    color: white;
    border: none;
    padding: 0.75rem 1.5rem;
    border-radius: 6px;
    cursor: pointer;
    font-size: 1rem;
    font-weight: 600;
    transition: all 0.2s ease;
}

.btn-primary:hover {
    background: var(--primary-hover);
    transform: translateY(-2px);
    box-shadow: var(--shadow-hover);
}

.btn-primary:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    transform: none;
}
```

#### Secondary Buttons
```css
.btn-secondary {
    background: #6c757d;
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 6px;
    cursor: pointer;
}

.btn-secondary:hover {
    background: #5a6268;
}
```

### 8. Form Controls

#### Input Fields
```css
.form-control {
    width: 100%;
    padding: 0.75rem;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    font-size: 1rem;
    transition: border-color 0.2s ease;
}

.form-control:focus {
    outline: none;
    border-color: var(--primary-color);
    box-shadow: 0 0 0 3px rgba(0, 120, 212, 0.1);
}
```

#### Form Groups
```html
<div class="form-group">
    <label>Label Text</label>
    <input type="text" class="form-control" @bind="variable" />
</div>
```

```css
.form-group {
    margin-bottom: 1rem;
}

.form-group label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 600;
    color: var(--text-primary);
}
```

### 9. Navigation Menu

#### Section Headers
Use section headers to group menu items:
```html
<div class="nav-separator"></div>
<div class="nav-section-header">Section Title</div>
```

```css
.nav-separator {
    height: 1px;
    background-color: rgba(255, 255, 255, 0.2);
    margin: 1rem 0.75rem;
}

.nav-section-header {
    color: rgba(255, 255, 255, 0.6);
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    padding: 0.75rem 1rem 0.5rem 1rem;
}
```

### 10. Progress Bars

```html
<div class="progress-bar-container">
    <div class="progress-bar" style="width: @percentage%"></div>
</div>
```

```css
.progress-bar-container {
    width: 100%;
    height: 20px;
    background: #e9ecef;
    border-radius: 10px;
    overflow: hidden;
}

.progress-bar {
    height: 100%;
    background: linear-gradient(90deg, #28a745 0%, #20c997 100%);
    transition: width 0.3s ease;
}
```

### 11. Color Usage Guidelines

#### Status Colors
- **Success/Active**: `var(--success-color)` - Green (#107c10)
- **Warning**: `var(--warning-color)` - Orange (#ff8c00)
- **Error/Critical**: `var(--error-color)` - Red (#d13438)
- **Info/Primary**: `var(--primary-color)` - Blue (#0078d4)

#### Gradient Patterns
Use gradients for headers and emphasis:
```css
/* Medical/Primary */
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);

/* Success */
background: linear-gradient(90deg, #28a745 0%, #20c997 100%);

/* Warning */
background: linear-gradient(135deg, #ff6b6b 0%, #ffa500 100%);
```

### 12. Responsive Design

#### Mobile Breakpoints
```css
/* Desktop: default */

/* Tablet: 768px and below */
@media (max-width: 768px) {
    .card {
        padding: 1rem;
    }
    
    .form-row {
        flex-direction: column;
    }
}

/* Mobile: 480px and below */
@media (max-width: 480px) {
    .page-header h3 {
        font-size: 1.25rem;
    }
}
```

### 13. Icon Usage

Use emoji icons consistently:
- 🏥 Healthcare/Medical
- 🔍 Search
- 📊 Analytics/Reports
- 🛡️ Security/Fraud
- ✅ Success/Approved
- ⚠️ Warning
- 🚫 Denied/Error
- 📋 Claims/Documents
- 💊 Pharmacy/Medications
- 🦷 Dental
- 👤 Patient/Member
- 🏢 Provider/Organization

### 14. Page Title Pattern

**Internal Pages:**
```razor
<PageTitle>Page Name - ClaimsIQ Platform</PageTitle>
```

**External Portals:**
```razor
<PageTitle>Portal Name - ClaimsIQ</PageTitle>
```

## Code Quality Standards

### 1. Razor Syntax
- Use `@code` blocks for component logic
- Always use proper `@bind` directives
- Avoid inline C# in HTML when possible

### 2. State Management
- Initialize boolean states as `false` for FHIR toggles
- Use descriptive variable names (e.g., `showFhirJson`, not `show`)
- Keep component state minimal

### 3. Comments
```csharp
// Good: Descriptive comments for complex logic
// Calculate risk score based on multiple weighted factors
var riskScore = CalculateWeightedRisk(claim);

// Avoid: Obvious comments
// Set the value
var value = 10;
```

### 4. Naming Conventions
- **Components**: PascalCase (e.g., `MemberPortal.razor`)
- **CSS Classes**: kebab-case (e.g., `.page-header`)
- **C# Variables**: camelCase (e.g., `showFhirJson`)
- **C# Properties**: PascalCase (e.g., `ClaimId`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_RETRY_COUNT`)

## Testing Checklist

When creating or modifying pages, verify:
- [ ] FHIR JSON is hidden by default
- [ ] Toggle buttons are present for all FHIR resources
- [ ] Currency values display correctly (not showing "ToString")
- [ ] Tables are responsive and styled consistently
- [ ] Page headers have gradient backgrounds
- [ ] Cards have consistent shadows and hover effects
- [ ] Buttons have hover states
- [ ] Form controls have focus states
- [ ] Mobile breakpoints work correctly
- [ ] All navigation items are properly categorized (Internal vs External)

## Common Anti-Patterns to Avoid

### ❌ DON'T
```razor
<!-- Don't mix string interpolation with ToString() -->
<td>$@value.ToString("N2")</td>

<!-- Don't show FHIR by default -->
<pre>@fhirJson</pre>

<!-- Don't use inline styles -->
<div style="color: red; font-size: 14px;">Text</div>

<!-- Don't use generic class names -->
<div class="container">...</div>
```

### ✅ DO
```razor
<!-- Use proper Razor formatting -->
<td>@($"{value:N2}")</td>

<!-- Always use FHIR toggles -->
@if (showFhirJson)
{
    <pre>@fhirJson</pre>
}

<!-- Use CSS classes -->
<div class="error-message">Text</div>

<!-- Use descriptive class names -->
<div class="fraud-results-card">...</div>
```

## Version Control

- Commit messages should reference Copilot assistance when AI was used extensively
- Use conventional commit format: `feat:`, `fix:`, `style:`, `refactor:`
- Include screenshots in PRs for UI changes

## Documentation

When creating new components:
1. Add inline XML documentation for public methods
2. Document CSS class purposes in comments
3. Update this file with new patterns
4. Include usage examples for complex components

---

**Last Updated**: January 2026  
**Platform Version**: .NET 10 Blazor WebAssembly  
**FHIR Version**: R4

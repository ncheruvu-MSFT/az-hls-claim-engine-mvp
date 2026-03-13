# GitHub Copilot Custom Agents Setup Guide

## Overview
This repository uses custom GitHub Copilot agents to enforce platform standards automatically. These agents validate code changes during development, ensuring consistency in styling, FHIR integration, and testing.

## Configured Agents

### 1. **UI/UX Validation Agent** (`@ui-ux-agent`)
**Purpose**: Enforces styling consistency and responsive design standards

**Triggers on**:
- `**/*.razor` - Blazor component files
- `**/*.css` - Stylesheets
- `**/wwwroot/**` - Static web assets

**Validates**:
- ✅ Tables use `.data-table` class
- ✅ Buttons use Bootstrap classes (`.btn-primary`, `.btn-secondary`)
- ✅ Responsive grid uses `.row` and `.col-*` classes
- ✅ Cards use `.card`, `.card-header`, `.card-body` structure
- ✅ **CRITICAL**: External portals NEVER show FHIR JSON to end users

**Example Usage**:
```razor
<!-- ✅ CORRECT -->
<table class="data-table">
    <thead>
        <tr><th>Name</th></tr>
    </thead>
    <tbody>
        <tr><td>John Doe</td></tr>
    </tbody>
</table>

<button class="btn btn-primary">Submit</button>

<!-- ❌ WRONG -->
<table class="custom-table">...</table>
<button class="btn-custom">Submit</button>
```

### 2. **FHIR Integration Agent** (`@fhir-integration-agent`)
**Purpose**: Validates FHIR R4 resource compliance and proper mapping

**Triggers on**:
- `**/Models/**/*.cs` - Domain models
- `**/Services/**/*Fhir*.cs` - FHIR service classes
- `**/Mapping/**/*.cs` - Data mapping classes
- `**/Pages/**/*.razor` - Pages displaying FHIR data

**Validates**:
- ✅ FHIR resources conform to R4 specification
- ✅ Required elements are present (resourceType, id, status)
- ✅ Terminology bindings use standard code systems
- ✅ **Display Rules**:
  - Internal tools: FHIR toggle hidden by default
  - External portals: NEVER display FHIR JSON

**Example Usage**:
```csharp
// ✅ CORRECT - Internal Page
@if (showFhirJson)
{
    <pre class="fhir-code">@fhirResource</pre>
}

// ❌ WRONG - Member Portal
<pre>@fhirResource</pre> <!-- NEVER show to members! -->
```

### 3. **Test Automation Agent** (`@test-automation-agent`)
**Purpose**: Ensures Playwright tests exist for new UI components

**Triggers on**:
- `**/Pages/**/*.razor` - New/modified pages
- `**/Tests/**/*.cs` - Test files

**Validates**:
- ✅ New pages have corresponding Playwright tests
- ✅ Tests verify table rendering
- ✅ Tests check button functionality
- ✅ Tests validate responsive breakpoints
- ✅ Tests check data population

**Example Test Pattern**:
```csharp
[Test]
public async Task MemberPortal_ClaimsTable_DisplaysCorrectly()
{
    await Page.GotoAsync("http://localhost:5000/member-portal");
    
    // Verify table structure
    await Expect(Page.Locator(".data-table")).ToBeVisibleAsync();
    await Expect(Page.Locator(".data-table thead th")).ToHaveCountAsync(7);
    
    // Verify no FHIR JSON visible
    await Expect(Page.Locator(".fhir-section")).Not.ToBeVisibleAsync();
}
```

## Activation Steps

### Option 1: VS Code (Recommended)

1. **Open VS Code Settings**
   - Press `Ctrl+,` (Windows) or `Cmd+,` (Mac)
   - Or: File → Preferences → Settings

2. **Search for Copilot Agents**
   - Type "github copilot agents" in search bar

3. **Enable Custom Agents**
   - Check: ☑ **"GitHub Copilot: Enable Agent Extensions"**
   - Check: ☑ **"GitHub Copilot: Use Repository Agent Config"**

4. **Verify Configuration**
   - Open Command Palette (`Ctrl+Shift+P`)
   - Type: "GitHub Copilot: Show Available Agents"
   - Confirm agents appear: `@ui-ux-agent`, `@fhir-integration-agent`, `@test-automation-agent`

### Option 2: GitHub Repository Settings

1. **Navigate to Repository Settings**
   - Go to: `https://github.com/[your-org]/[your-repo]/settings`

2. **Enable Copilot Features**
   - Click: **Copilot** (left sidebar)
   - Toggle ON: **"Enable GitHub Copilot for this repository"**
   - Toggle ON: **"Allow custom agents from .github/copilot-agents.json"**

3. **Configure Agent Access**
   - Select: **"Use repository-defined agents"**
   - Save changes

### Option 3: Organization Level (Admin Only)

1. **Organization Settings**
   - Go to: `https://github.com/organizations/[your-org]/settings/copilot`

2. **Enable Custom Agents**
   - Check: ☑ **"Allow custom agents in .github/copilot-agents.json"**
   - Apply to: **All repositories** or **Selected repositories**

3. **Set Policies**
   - Enforcement level: **Required** (blocks PRs without agent validation)
   - Or: **Advisory** (shows warnings only)

## Using Agents in Development

### Invoke Agent in Copilot Chat

```plaintext
@ui-ux-agent Review this MemberPortal.razor file for styling consistency

@fhir-integration-agent Check if this ClaimResource.cs follows FHIR R4 spec

@test-automation-agent Generate Playwright tests for the new Formulary page
```

### Automatic Validation

Agents trigger automatically when you:
- **Save a file** matching trigger patterns
- **Open a PR** (agents run on all changed files)
- **Request code review** (agents add inline comments)

### Example Agent Response

```plaintext
🤖 @ui-ux-agent detected issues:

❌ Line 283: FHIR JSON displayed to members
   Member Portal should NEVER show technical FHIR data

❌ Line 350: Table uses class "claims-table"
   Use .data-table instead for consistency

❌ Line 420: Button uses class "btn-view-details"
   Use .btn .btn-primary instead

✅ Fixed versions:
   - Remove lines 283-346 (FHIR section)
   - Replace: .claims-table → .data-table
   - Replace: .btn-view-details → .btn .btn-primary
```

## Configuration File Location

The agents are defined in:
```
.github/copilot-agents.json
```

**Schema Version**: `1.0`  
**Schema URL**: `https://aka.ms/copilot/agent-schema`

## Customizing Agents

### Modify Agent Instructions

Edit `.github/copilot-agents.json`:

```json
{
  "agents": [
    {
      "name": "ui-ux-agent",
      "description": "Your custom description",
      "instructions": "# Your custom instructions here\n\n## Rules\n- Rule 1\n- Rule 2",
      "triggers": ["**/*.razor", "**/*.css"],
      "skills": ["code-review", "validation", "best-practices"]
    }
  ]
}
```

### Add New Agent

```json
{
  "name": "security-agent",
  "description": "Validates security best practices",
  "instructions": "# Security Validation\n\n- Check for SQL injection\n- Validate authentication",
  "triggers": ["**/Services/**/*.cs", "**/Controllers/**/*.cs"],
  "skills": ["security", "code-review"]
}
```

### Disable an Agent

Comment out in `.github/copilot-agents.json`:

```json
{
  "agents": [
    // {
    //   "name": "test-automation-agent",
    //   ...
    // }
  ]
}
```

## Troubleshooting

### Agents Not Appearing

**Problem**: Agents don't show in Copilot Chat

**Solutions**:
1. Verify `.github/copilot-agents.json` has valid JSON syntax
2. Restart VS Code
3. Run: Command Palette → "GitHub Copilot: Reload Agent Configuration"
4. Check JSON schema version matches: `"schemaVersion": "1.0"`

### Agents Not Triggering

**Problem**: No validation when saving files

**Solutions**:
1. Check file path matches trigger pattern
   - Example: `Pages/MemberPortal.razor` matches `**/*.razor`
2. Ensure agent triggers include file extension
3. Verify "Use Repository Agent Config" is enabled in settings

### False Positives

**Problem**: Agent flags correct code as incorrect

**Solutions**:
1. Add exceptions to agent instructions:
   ```markdown
   ## Exceptions
   - Legacy pages in `/Legacy/**` can use custom classes
   ```
2. Use ignore comments in code:
   ```razor
   <!-- copilot-ignore ui-ux-agent -->
   <table class="legacy-table">...</table>
   ```

### Agent Conflicts

**Problem**: Multiple agents give contradictory guidance

**Solutions**:
1. Review agent priorities in `.github/copilot-agents.json`
2. Make triggers more specific:
   - `**/Pages/Internal/**/*.razor` → UI agent
   - `**/Pages/External/**/*.razor` → External UX agent
3. Merge similar agents into one

## Best Practices

### 1. Start with Advisory Mode
- Set enforcement to "Advisory" initially
- Review agent feedback for 1-2 weeks
- Switch to "Required" once team is familiar

### 2. Keep Instructions Concise
- Focus on 5-10 key rules per agent
- Use examples (✅ Correct / ❌ Wrong)
- Link to detailed docs for complex topics

### 3. Regular Updates
- Review agent effectiveness monthly
- Add rules for common PR feedback
- Remove outdated validations

### 4. Team Training
- Share this guide with all developers
- Demo agent usage in team meetings
- Collect feedback on false positives

## Platform-Specific Rules

### ClaimsIQ Platform Standards

**Internal Tools** (Claims Engine, Fraud Analytics, etc.):
- ✅ Can show FHIR JSON with toggle (hidden by default)
- ✅ Use `.data-table` for all tables
- ✅ Use Bootstrap `.btn-primary` / `.btn-secondary`
- ✅ Apply responsive grid (`.row`, `.col-md-6`)

**External Portals** (Member Portal, Provider Portal):
- ⛔ **NEVER** show FHIR JSON
- ✅ Use simplified terminology (not technical)
- ✅ Focus on patient-friendly UI
- ✅ Hide implementation details

**Shared Standards**:
- CSS variables: `var(--primary-color)`, `var(--success-color)`
- Card shadows: `var(--shadow)`, hover: `var(--shadow-hover)`
- Page headers: Gradient backgrounds
- Buttons: Hover states with `transform: translateY(-2px)`

## Support

**Questions?**
- Review: `.github/copilot-instructions.md` for detailed styling guidelines
- Check: Agent instructions in `.github/copilot-agents.json`
- Ask: `@ui-ux-agent explain your validation rules`

**Report Issues**:
- GitHub Issues: Tag with `copilot-agent` label
- Include: File path, agent name, false positive details

---

**Last Updated**: January 2026  
**Agent Version**: 1.0  
**Schema Version**: 1.0

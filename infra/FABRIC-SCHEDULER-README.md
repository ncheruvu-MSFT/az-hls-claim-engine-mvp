# Fabric Capacity Auto-Suspend/Resume

Automatically pause Fabric capacity during non-working hours to save ~**70% on Fabric costs** (~$370/month savings on F2 SKU).

## Schedule (EST Timezone)

| Day | Resume | Suspend | Fabric State |
|-----|--------|---------|--------------|
| **Mon-Fri** | 8:00 AM | 6:00 PM | Active 10 hours |
| **Sat-Sun** | - | - | Suspended all day |

**Total Active Time**: ~50 hours/week (10 hrs/day × 5 days)  
**Savings**: 70% of Fabric capacity cost

## Cost Analysis

### Without Scheduler:
- Fabric F2: **$524/month** (24/7 operation)

### With Scheduler:
- Fabric F2: **~$154/month** (30% uptime: 50hrs/168hrs per week)
- Logic Apps: **~$0.10/month** (Consumption tier, minimal executions)
- **Total: ~$154/month**
- **Savings: ~$370/month** 💰

## Deployment

### Option 1: Deploy with Main Infrastructure

Update [main.bicep](main.bicep) to include the scheduler module:

```bicep
// Add at the end of main.bicep
module fabricScheduler 'fabric-scheduler.bicep' = {
  name: 'fabric-scheduler-deployment'
  params: {
    location: location
    fabricCapacityName: fabricCapacity.name
    fabricResourceGroup: resourceGroup().name
  }
}
```

Then deploy:
```bash
az deployment group create -g rg-claims-rules-nonprod -f infra/main.bicep -p @infra/params.json
```

### Option 2: Deploy Separately (After Main Infrastructure)

```bash
# Deploy the scheduler to existing resource group with Fabric capacity
az deployment group create \
  -g rg-claims-rules-nonprod \
  -f infra/fabric-scheduler.bicep \
  -p fabricCapacityName='fabric-claims-dev' \
  -p fabricResourceGroup='rg-claims-rules-nonprod' \
  -p location='eastus'
```

## How It Works

### Architecture:
```
Logic App (Pause)                Logic App (Resume)
    ↓                                   ↓
[Recurrence Trigger]            [Recurrence Trigger]
6 PM EST Daily                  8 AM EST Daily
    ↓                                   ↓
[Check if Weekend]              [Check if Weekend]
    ↓                                   ↓
[If Weekday]                    [If Weekday]
    ↓                                   ↓
[HTTP: Suspend Fabric]          [HTTP: Resume Fabric]
    ↓                                   ↓
[Managed Identity Auth]         [Managed Identity Auth]
```

### Components:

1. **Managed Identity** (`id-fabric-scheduler`)
   - User-assigned identity with Contributor role on Fabric capacity
   - No secrets/keys needed

2. **Logic App: Pause** (`logic-fabric-pause`)
   - Triggers at 6:00 PM EST every day
   - Checks if it's a weekday (Mon-Fri)
   - Calls Fabric API to suspend capacity
   - Cost: ~$0.05/month

3. **Logic App: Resume** (`logic-fabric-resume`)
   - Triggers at 8:00 AM EST every day
   - Checks if it's a weekday (Mon-Fri)
   - Calls Fabric API to resume capacity
   - Cost: ~$0.05/month

### Logic App Definition:

**Weekend Check Logic**:
```javascript
// In Logic App expression
@or(equals(dayOfWeek(utcNow()), 0), equals(dayOfWeek(utcNow()), 6))
// Returns true for Saturday (6) or Sunday (0)
```

**Fabric API Calls**:
```http
POST https://management.azure.com/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Fabric/capacities/{name}/suspend?api-version=2023-11-01
POST https://management.azure.com/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Fabric/capacities/{name}/resume?api-version=2023-11-01
```

## Verify Deployment

```bash
# Check Logic Apps are deployed
az logic workflow list -g rg-claims-rules-nonprod --query "[].{Name:name, State:state}" -o table

# View Logic App run history
az logic workflow show -g rg-claims-rules-nonprod -n logic-fabric-pause
az logic workflow show -g rg-claims-rules-nonprod -n logic-fabric-resume

# Check Fabric capacity status
az fabric capacity show -n fabric-claims-dev -g rg-claims-rules-nonprod --query state
```

## Manual Override (Emergency Access)

If you need Fabric during off-hours:

```bash
# Manually resume Fabric capacity
az fabric capacity resume -n fabric-claims-dev -g rg-claims-rules-nonprod

# Manually suspend Fabric capacity
az fabric capacity suspend -n fabric-claims-dev -g rg-claims-rules-nonprod
```

Or temporarily disable the scheduler:

```bash
# Disable pause Logic App (keeps Fabric running after hours)
az logic workflow update -g rg-claims-rules-nonprod -n logic-fabric-pause --set state=Disabled

# Re-enable when done
az logic workflow update -g rg-claims-rules-nonprod -n logic-fabric-pause --set state=Enabled
```

## Customize Schedule

Edit [fabric-scheduler.bicep](fabric-scheduler.bicep) to change hours:

```bicep
// Change pause time (default 6 PM)
schedule: {
  hours: ['20'] // Change to 8 PM
  minutes: [0]
}

// Change resume time (default 8 AM)
schedule: {
  hours: ['7'] // Change to 7 AM
  minutes: [0]
}
```

Redeploy after changes:
```bash
az deployment group create -g rg-claims-rules-nonprod -f infra/fabric-scheduler.bicep -p ...
```

## Monitoring & Alerts

### View Logic App Run History (Portal):
1. Azure Portal → Logic Apps → `logic-fabric-pause` or `logic-fabric-resume`
2. Click **Runs history** to see execution logs
3. Check if weekends are correctly skipped
4. Verify HTTP calls to Fabric API are successful

### Create Alert for Failed Runs:
```bash
# Alert if Logic App fails
az monitor metrics alert create \
  --name "Fabric Scheduler Failed" \
  --resource-group rg-claims-rules-nonprod \
  --scopes $(az logic workflow show -g rg-claims-rules-nonprod -n logic-fabric-pause --query id -o tsv) \
  --condition "count WorkflowRunsFailureCount > 0" \
  --window-size 1h \
  --evaluation-frequency 15m \
  --action email <your-email@domain.com>
```

## Troubleshooting

### Issue: Fabric not suspending at 6 PM
**Check**:
1. Logic App is enabled: `az logic workflow show -g <RG> -n logic-fabric-pause --query state`
2. View run history in Azure Portal → Logic Apps → Runs history
3. Verify managed identity has Contributor role on Fabric capacity

### Issue: Logic App runs but Fabric stays active
**Check**:
1. Check HTTP action response in Logic App run history
2. Verify Fabric API version is correct (2023-11-01)
3. Ensure managed identity authentication is configured

### Issue: Fabric suspended on weekdays
**Check**:
1. Verify timezone is set to "Eastern Standard Time"
2. Check weekend logic: `dayOfWeek(utcNow())` should skip 0 (Sun) and 6 (Sat)
3. Manually test weekend logic in Logic App designer

### Issue: Need Fabric during off-hours
**Solution**:
```bash
# Temporarily disable pause Logic App
az logic workflow update -g <RG> -n logic-fabric-pause --set state=Disabled

# Manually resume Fabric
az fabric capacity resume -n fabric-claims-dev -g <RG>

# Re-enable scheduler next day
az logic workflow update -g <RG> -n logic-fabric-pause --set state=Enabled
```

## Alternative: Azure Automation Runbook

If you prefer PowerShell-based automation (same cost):

```powershell
# Create Automation Account (one-time setup)
az automation account create -n auto-fabric-scheduler -g <RG> -l eastus

# Create runbook (see alternative-runbook.ps1 for script)
az automation runbook create --automation-account-name auto-fabric-scheduler --resource-group <RG> --name Fabric-Pause-Resume --type PowerShell

# Schedule runbook
az automation schedule create --automation-account-name auto-fabric-scheduler --resource-group <RG> --name Pause-6PM --frequency Day --interval 1 --start-time "18:00"
```

## FAQ

**Q: What happens if I'm using Fabric when it suspends?**  
A: Active workloads are gracefully stopped. Users see "capacity suspended" message. Resume to continue.

**Q: Can I customize per environment (dev/prod)?**  
A: Yes! Deploy scheduler only to dev/nonprod. Keep prod 24/7 or use different hours.

**Q: Does this affect OneLake data?**  
A: No. OneLake data persists. Only compute is suspended (Power BI, Pipelines, Spark).

**Q: What about holidays?**  
A: Add holiday logic to the weekend check, or manually disable scheduler on holidays.

**Q: Can I use Azure Functions instead?**  
A: Yes, but Logic Apps are simpler for scheduling and cost the same (~$0.10/month).

## Next Steps

1. ✅ Deploy scheduler: `az deployment group create ...`
2. ✅ Verify Logic Apps are enabled and running
3. ✅ Monitor first few days to ensure correct behavior
4. ✅ Calculate actual savings from reduced Fabric uptime
5. ✅ Customize schedule for your team's working hours
6. ✅ Set up alerts for failed scheduler runs

**Expected ROI**: Pays for itself in the first day. Saves ~$370/month on Fabric F2.

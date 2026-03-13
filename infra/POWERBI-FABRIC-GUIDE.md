# Power BI & Fabric Integration Guide

This infrastructure uses **Microsoft Fabric's OneLake** for unified data storage and **Fabric Data Pipelines** for orchestration. All compute stays in Azure.

## Architecture Overview (Fabric-Native)

```
Azure Compute Layer:
  FHIR Claims → Function App → Cosmos Audit
                             ↓
  FHIR $export → Azure Storage (fhir-export container)

Microsoft Fabric Layer (OneLake):
  Azure Storage → OneLake Shortcut → Fabric Lakehouse
  Cosmos DB → Fabric Mirroring → OneLake
  
  OneLake → Fabric Data Pipelines (orchestration)
         → Power BI Semantic Models
         → Real-time Analytics
```

## Key Principles
✅ **Compute in Azure**: Function Apps, FHIR service, Cosmos DB  
✅ **Data in OneLake**: Unified data lake (no separate Synapse/ADF)  
✅ **Orchestration in Fabric**: Fabric Data Pipelines (not Azure Data Factory)  
✅ **Analytics in Fabric**: Power BI, KQL, Spark, Data Science - all on OneLake

## What's Deployed for Analytics

| Resource | Purpose | Cost |
|----------|---------|------|
| **Cosmos DB + Analytical Storage** | Real-time audit data | FREE tier + minimal |
| **Azure Storage (ADLS Gen2)** | FHIR $export landing zone | ~$0.50/month |
| **Fabric Capacity F2** | OneLake + Pipelines + Power BI | ~$524/month OR 60-day trial |
| **Azure Function App** | Compute (claims processing) | FREE (Consumption) |

> **Cost Tip**: Use Fabric Trial (60 days free) for testing. OneLake storage is included with Fabric capacity.

## OneLake Integration Patterns

### Pattern 1: OneLake Shortcuts (Azure Storage → OneLake)
**Best for**: FHIR $export data (NDJSON/Parquet files)

```mermaid
Azure Storage (fhir-export) → OneLake Shortcut → Lakehouse → Power BI
```

**Steps**:
1. In Fabric workspace, create a **Lakehouse**
2. Click **Get Data** → **OneLake Shortcuts**
3. Select **Azure Data Lake Storage Gen2**
4. Enter storage account: `ahdsstoragedev001`
5. Select container: `fhir-export`
6. Data appears in OneLake without copying

### Pattern 2: Fabric Mirroring (Cosmos DB → OneLake)
**Best for**: Real-time audit data streaming

```mermaid
Cosmos DB (audit) → Fabric Mirroring → OneLake → Power BI DirectLake
```

**Steps**:
1. In Fabric workspace, create **Mirrored Cosmos DB**
2. Connect to Cosmos endpoint from deployment outputs
3. Select `claims` database and `audit` container
4. Fabric continuously replicates to OneLake (no ETL needed)
5. Use **DirectLake** mode in Power BI for instant queries

### Pattern 3: Fabric Data Pipelines (Orchestration)
**Best for**: FHIR $export automation and data transformations

**Create Pipeline**:
1. In Fabric workspace, click **+ New** → **Data pipeline**
2. Name: "FHIR-Export-to-OneLake"
3. Add activities:
   - **Web Activity**: Trigger FHIR `$export`
   - **Wait/Until**: Poll for completion
   - **Copy Activity**: Azure Storage → OneLake Lakehouse
   - **Notebook**: Transform NDJSON to Delta tables

## Data Sources for Power BI

### 1️⃣ **Cosmos DB via Fabric Mirroring (RECOMMENDED)**
- **Connection**: Mirrored Cosmos DB in Fabric
- **Mode**: **DirectLake** (fastest, no import/DirectQuery)
- **Use Case**: Real-time claim processing dashboards

**Setup**:
1. Create Mirrored Cosmos DB (see Pattern 2 above)
2. In Power BI Desktop, connect to **OneLake data hub**
3. Select mirrored `audit` table
4. Choose **DirectLake** mode
5. Build reports (data updates in near real-time)

### 2️⃣ **FHIR $export via OneLake Shortcut**
- **Connection**: Lakehouse with shortcut to Azure Storage
- **Format**: NDJSON or Parquet
- **Use Case**: Historical FHIR Claim resources

**Setup**:
1. Create OneLake Shortcut (see Pattern 1)
2. In Fabric Lakehouse, run SQL Analytics endpoint
3. Create Delta tables from NDJSON:
   ```sql
   CREATE TABLE claims_staging
   USING json
   OPTIONS (path 'Files/fhir-export/Claim*.ndjson')
   ```
4. Power BI → OneLake → Select Lakehouse SQL endpoint
5. Load Delta tables

### 3️⃣ **Cosmos DB Native Connector (Alternative)**
- **Connection**: Azure Cosmos DB connector (legacy)
- **Mode**: DirectQuery or Import
- **Use Case**: When Fabric Mirroring not available

**Steps**:
1. Power BI Desktop → Get Data → Azure Cosmos DB
2. Enter endpoint from outputs: `<cosmosEndpoint>`
3. Select `claims.audit`
4. Choose DirectQuery for real-time or Import for performance

## Sample Power BI Reports (Fabric OneLake)

### Report 1: Real-time Claims Processing Dashboard
**Data Source**: Mirrored Cosmos DB in OneLake (DirectLake mode)

**Visuals**:
- Card: Total Claims Processed
- Line Chart: Claims over time
- Bar Chart: Success vs Failure rate
- Table: Recent claims with member/provider ID

**DAX Measures** (DirectLake):
```dax
Total Claims = COUNTROWS('audit')
Success Rate = 
  DIVIDE(
    CALCULATE(COUNTROWS('audit'), 'audit'[statusCode] >= 200, 'audit'[statusCode] < 300),
    COUNTROWS('audit')
  )
```

### Report 2: FHIR Claims Analytics  
**Data Source**: Lakehouse Delta tables (from FHIR $export via OneLake shortcut)

**Fabric Notebook to prepare data**:
```python
# In Fabric Lakehouse notebook
from pyspark.sql import SparkSession

# Read NDJSON from OneLake shortcut
claims_df = spark.read.json("Files/fhir-export/Claim*.ndjson")

# Transform to Delta table
claims_df.write.format("delta").mode("overwrite").saveAsTable("claims_history")
```

**Power BI visuals**:
- Slicer: Date range, Provider, Member
- Matrix: Claims by diagnosis code
- Map: Claims by patient location
- KPI: Total claim amount

## Fabric Data Pipelines (Orchestration)

Replace Azure Data Factory with **Fabric Data Pipelines** for FHIR $export automation:

### Pipeline: "FHIR-Export-to-OneLake"

**Activities**:
1. **Web Activity** - Trigger FHIR $export
   - URL: `https://<fhir-endpoint>/$export?_type=Claim`
   - Method: POST
   - Authentication: Managed Identity
   
2. **Until Loop** - Wait for export completion
   - Check status URL from Activity 1
   - Repeat until status = "Completed"

3. **Copy Data** - Move to OneLake
   - Source: Azure Storage `fhir-export` container
   - Sink: OneLake Lakehouse `Files/claims/raw/`

4. **Fabric Notebook** - Transform to Delta
   - Read NDJSON from OneLake
   - Apply schema, flatten JSON
   - Write Delta tables for Power BI

**Schedule**: Daily at 2 AM UTC

### Create Pipeline in Fabric:
1. Go to Fabric workspace → **+ New** → **Data pipeline**
2. Add **Web activity** for FHIR $export trigger
3. Add **Copy data** from Azure Storage to OneLake
4. Add **Notebook** for transformation
5. Set trigger schedule

## OneLake Benefits vs Synapse/ADF

| Feature | Traditional (Synapse/ADF) | Fabric OneLake |
|---------|---------------------------|----------------|
| **Data Lake** | Separate ADLS Gen2 accounts | OneLake (unified, automatic) |
| **Orchestration** | Azure Data Factory | Fabric Data Pipelines |
| **SQL Analytics** | Synapse SQL Pool ($1.20/hr) | Lakehouse SQL (included) |
| **Spark** | Synapse Spark pools | Fabric Spark (included) |
| **Power BI** | DirectQuery/Import | **DirectLake** (fastest) |
| **Cost** | Pay per resource | Single Fabric capacity |
| **Data Copy** | ETL/ELT required | Shortcuts (zero-copy) |

## Fabric Workspace Setup

### Option A: Fabric Trial (60 days FREE) - RECOMMENDED
1. Go to [app.fabric.microsoft.com](https://app.fabric.microsoft.com)
2. Click **Start trial** (no credit card needed)
3. Create workspace: "Claims-Analytics-Workspace"
4. Capacity: Trial (60 days)

### Option B: Deploy F2 Capacity (Bicep)
```bash
# Deploy with Fabric capacity (~$524/month)
az deployment group create -g <RG> -f infra/main.bicep -p @infra/params.json
```

Then assign workspace:
1. Workspace Settings → **License** → Premium
2. Select `fabric-claims-dev` capacity

### Option C: Use Existing Fabric Workspace
If you already have Fabric:
1. Remove `fabricCapacity` from [main.bicep](main.bicep)
2. Deploy infrastructure
3. Create OneLake shortcuts in your workspace

## Step-by-Step: First Power BI Report

### 1. Create Fabric Lakehouse
```
Fabric Workspace → + New → Lakehouse → Name: "ClaimsData"
```

### 2. Create OneLake Shortcut to Azure Storage
```
ClaimsData Lakehouse → Get Data → OneLake Shortcut
→ Azure Data Lake Storage Gen2
→ Account: ahdsstoragedev001
→ Container: fhir-export
→ Shortcut name: "fhir_claims_raw"
```

### 3. Create Mirrored Cosmos DB
```
Fabric Workspace → + New → Mirrored Cosmos DB
→ Name: "ClaimsAudit"
→ Endpoint: <from deployment outputs>
→ Database: claims
→ Container: audit
```

### 4. Build Power BI Report
```
Power BI Desktop → Get Data → OneLake data hub
→ Select: ClaimsAudit (mirrored Cosmos)
→ Mode: DirectLake
→ Create visuals (real-time data, no refresh needed)
```

### 5. Publish to Fabric
```
Power BI Desktop → Publish
→ Select: Claims-Analytics-Workspace
→ Report appears in Fabric with DirectLake semantic model
```

## Data Refresh Strategy

| Source | Refresh Type | Frequency | Notes |
|--------|--------------|-----------|-------|
| **Mirrored Cosmos** | Real-time | Continuous | DirectLake in Power BI |
| **OneLake Shortcut** | On-demand | When source changes | Zero-copy, instant |
| **Delta Tables** | Fabric Pipeline | Scheduled | Run transformation pipeline |
| **Fabric Notebook** | Manual/Scheduled | As needed | Spark transformations |

## Fabric Data Pipelines vs Azure Data Factory

**Key Differences**:
- ✅ **Unified**: Pipelines live in same workspace as Power BI reports
- ✅ **OneLake Native**: Direct integration with Lakehouse/Warehouse
- ✅ **No Linked Services**: Automatic connection to OneLake
- ✅ **Spark Integration**: Call Fabric notebooks directly
- ✅ **Cost**: Included in Fabric capacity (no separate ADF billing)

**Migration from ADF** (if needed):
1. Export ADF pipeline JSON
2. Import to Fabric Data Pipeline
3. Update sinks to OneLake Lakehouses
4. Test and schedule

## Monitoring & Optimization

### Power BI Performance
- Use **DirectLake** for Cosmos mirrored data (fastest)
- Use **OneLake shortcuts** to avoid data copy
- Enable **Incremental Refresh** on Delta tables
- Use **Aggregations** for large FHIR exports

### Cost Optimization
1. **Use Fabric Trial**: 60 days free, full features
2. **OneLake shortcuts**: Zero-copy integration (no storage duplication)
3. **DirectLake mode**: No import/refresh costs
4. **Pause when idle**: Fabric capacity can be paused (unlike Synapse)
5. **Auto-scheduler**: Deploy [fabric-scheduler.bicep](fabric-scheduler.bicep) to automatically suspend Fabric during non-working hours (8 AM - 6 PM EST, weekdays only). **Saves ~$370/month (70% cost reduction)** on F2 capacity. See [FABRIC-SCHEDULER-README.md](FABRIC-SCHEDULER-README.md) for details.

## Sample Fabric Notebook (Transform FHIR Data)

```python
# Fabric Lakehouse Notebook: Transform FHIR Claims to Delta

import json
from pyspark.sql.functions import col, explode, from_unixtime

# Read FHIR Claim NDJSON from OneLake shortcut
claims_raw = spark.read.text("Files/fhir_claims_raw/Claim*.ndjson")

# Parse JSON
claims_json = claims_raw.select(
    from_json(col("value"), "struct<...>").alias("data")
)

# Flatten structure
claims_flat = claims_json.select(
    col("data.id").alias("claim_id"),
    col("data.patient.reference").alias("patient_id"),
    col("data.provider.reference").alias("provider_id"),
    col("data.total.value").alias("claim_amount"),
    col("data.created").alias("submitted_date")
)

# Write Delta table for Power BI
claims_flat.write.format("delta") \
    .mode("overwrite") \
    .saveAsTable("claims_history")

print("✅ Delta table 'claims_history' ready for Power BI")
```

## Next Steps

1. ✅ Deploy infrastructure: `az deployment group create -g <RG> -f infra/main.bicep -p @infra/params.json`
2. ✅ Capture outputs: `az deployment group show -g <RG> -n main --query properties.outputs`
3. ✅ Start Fabric Trial: [app.fabric.microsoft.com](https://app.fabric.microsoft.com)
4. ✅ Create Fabric Lakehouse with OneLake shortcut to Azure Storage
5. ✅ Create Mirrored Cosmos DB in Fabric
6. ✅ Build Power BI report with DirectLake mode
7. ✅ Create Fabric Data Pipeline for FHIR $export automation

## Troubleshooting

**Issue**: Can't create OneLake shortcut to Azure Storage  
**Solution**: Ensure storage has public network access enabled and SAS token/key available

**Issue**: Cosmos Mirroring not showing data  
**Solution**: Verify Cosmos has `analyticalStorageTtl: -1` on container (already set in Bicep)

**Issue**: DirectLake mode not available  
**Solution**: Ensure workspace is on Fabric/Premium capacity (not Pro)

**Issue**: Fabric pipeline can't access Azure Storage  
**Solution**: Use managed identity or create connection with storage account key

## Resources
- [Microsoft Fabric OneLake](https://learn.microsoft.com/fabric/onelake/)
- [Fabric Data Pipelines](https://learn.microsoft.com/fabric/data-factory/)
- [Cosmos DB Mirroring in Fabric](https://learn.microsoft.com/fabric/database/mirrored-database/azure-cosmos-db)
- [OneLake Shortcuts](https://learn.microsoft.com/fabric/onelake/onelake-shortcuts)
- [FHIR $export](https://learn.microsoft.com/azure/healthcare-apis/fhir/export-data)
- [Microsoft Fabric Trial](https://aka.ms/fabric-trial)

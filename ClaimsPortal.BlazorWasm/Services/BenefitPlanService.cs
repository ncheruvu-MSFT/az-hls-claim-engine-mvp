using System.Text.Json;
using Microsoft.JSInterop;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Service for managing benefit plans with browser localStorage persistence
/// </summary>
public class BenefitPlanService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "claimsiq_benefit_plans";
    private List<BenefitPlan> _plans = new();

    public BenefitPlanService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Load all benefit plans from localStorage
    /// </summary>
    public async Task<List<BenefitPlan>> GetPlansAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _plans = JsonSerializer.Deserialize<List<BenefitPlan>>(json) ?? new List<BenefitPlan>();
            }
            else
            {
                // Load default data if no saved data exists
                _plans = GetDefaultPlans();
                await SaveToStorageAsync();
            }
        }
        catch (Exception)
        {
            // If storage fails, use default data
            _plans = GetDefaultPlans();
        }

        return _plans;
    }

    /// <summary>
    /// Get a specific plan by ID
    /// </summary>
    public async Task<BenefitPlan?> GetPlanByIdAsync(string planId)
    {
        await GetPlansAsync();
        return _plans.FirstOrDefault(p => p.PlanId == planId);
    }

    /// <summary>
    /// Save or update a benefit plan
    /// </summary>
    public async Task<(bool Success, string Message)> SavePlanAsync(BenefitPlan plan)
    {
        // Validation
        var validationResult = ValidatePlan(plan);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.ErrorMessage);
        }

        await GetPlansAsync();

        var existingPlan = _plans.FirstOrDefault(p => p.PlanId == plan.PlanId);
        if (existingPlan != null)
        {
            // Update existing plan
            var index = _plans.IndexOf(existingPlan);
            _plans[index] = plan;
        }
        else
        {
            // Add new plan
            _plans.Add(plan);
        }

        await SaveToStorageAsync();
        return (true, existingPlan != null ? "Plan updated successfully" : "Plan created successfully");
    }

    /// <summary>
    /// Delete a benefit plan
    /// </summary>
    public async Task<(bool Success, string Message)> DeletePlanAsync(string planId)
    {
        await GetPlansAsync();
        var plan = _plans.FirstOrDefault(p => p.PlanId == planId);
        
        if (plan == null)
        {
            return (false, "Plan not found");
        }

        _plans.Remove(plan);
        await SaveToStorageAsync();
        return (true, "Plan deleted successfully");
    }

    /// <summary>
    /// Export plans to CMS Medicare PBP format
    /// </summary>
    public async Task<string> ExportToCMSAsync(List<BenefitPlan> plans)
    {
        var exportData = new
        {
            ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            FormatVersion = "CMS PBP 2026",
            ContractID = "H1234",
            PlanCount = plans.Count,
            Plans = plans.Select(p => new
            {
                PlanIdentifier = p.PlanId,
                PlanName = p.PlanName,
                PlanType = p.PlanType,
                
                // Deductible Information
                AnnualDeductible = new
                {
                    InNetwork = p.Deductible,
                    OutOfNetwork = p.Deductible * 2,
                    Individual = p.Deductible,
                    Family = p.Deductible * 2
                },
                
                // Out of Pocket Maximum
                MaximumOutOfPocket = new
                {
                    InNetwork = p.OutOfPocketMax,
                    OutOfNetwork = p.OutOfPocketMax * 2,
                    Individual = p.OutOfPocketMax,
                    Family = p.OutOfPocketMax * 2
                },
                
                // Cost Sharing
                CostSharing = new
                {
                    Coinsurance = $"{p.Coinsurance}%",
                    PrimaryCareVisit = new { Copay = p.PrimaryCopay, RequiresPriorAuth = p.RequiresPriorAuth },
                    SpecialistVisit = new { Copay = p.SpecialistCopay, RequiresPriorAuth = p.RequiresPriorAuth },
                    EmergencyRoom = new { Copay = 250, RequiresPriorAuth = false },
                    InpatientHospital = new { Copay = 500, RequiresPriorAuth = true }
                },
                
                // Network Information
                NetworkProviders = p.InNetworkProviders?.Select(prov => new
                {
                    NPI = prov.NPI,
                    Name = prov.Name,
                    Specialty = prov.Specialty,
                    Address = $"{prov.Address}, {prov.City}, {prov.State} {prov.Zip}"
                }).ToList<object>() ?? new List<object>(),
                
                // Plan Dates
                EffectiveDates = new
                {
                    Start = p.StartDate.ToString("yyyy-MM-dd"),
                    End = p.EndDate.ToString("yyyy-MM-dd")
                },
                
                // CMS Compliance
                CMSCompliance = new
                {
                    ApprovedForMedicare = true,
                    StarRating = 4.5,
                    QualityBonus = true,
                    LastReviewDate = "2025-12-15"
                }
            }).ToList()
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    /// <summary>
    /// Validate benefit plan data
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidatePlan(BenefitPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.PlanId))
            return (false, "Plan ID is required");

        if (string.IsNullOrWhiteSpace(plan.PlanName))
            return (false, "Plan Name is required");

        if (plan.Deductible < 0)
            return (false, "Deductible cannot be negative");

        if (plan.OutOfPocketMax < 0)
            return (false, "Out of Pocket Maximum cannot be negative");

        if (plan.OutOfPocketMax < plan.Deductible)
            return (false, "Out of Pocket Maximum must be greater than or equal to Deductible");

        if (plan.Coinsurance < 0 || plan.Coinsurance > 100)
            return (false, "Coinsurance must be between 0 and 100");

        if (plan.StartDate >= plan.EndDate)
            return (false, "End Date must be after Start Date");

        return (true, string.Empty);
    }

    /// <summary>
    /// Save plans to localStorage
    /// </summary>
    private async Task SaveToStorageAsync()
    {
        var json = JsonSerializer.Serialize(_plans);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    /// <summary>
    /// Get default sample plans
    /// </summary>
    private List<BenefitPlan> GetDefaultPlans()
    {
        return new List<BenefitPlan>
        {
            new BenefitPlan
            {
                PlanId = "HMO-001",
                PlanName = "Gold HMO Plan",
                PlanType = "HMO",
                Deductible = 1500,
                OutOfPocketMax = 6000,
                Coinsurance = 20,
                PrimaryCopay = 25,
                SpecialistCopay = 50,
                RequiresPriorAuth = true,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                InNetworkProviders = new List<NetworkProvider>
                {
                    new NetworkProvider { NPI = "1234567890", Name = "Valley Medical Group", Specialty = "Primary Care", Address = "123 Health St", City = "Los Angeles", State = "CA", Zip = "90001" }
                }
            },
            new BenefitPlan
            {
                PlanId = "PPO-002",
                PlanName = "Platinum PPO Plan",
                PlanType = "PPO",
                Deductible = 2000,
                OutOfPocketMax = 8000,
                Coinsurance = 30,
                PrimaryCopay = 30,
                SpecialistCopay = 60,
                RequiresPriorAuth = false,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                InNetworkProviders = new List<NetworkProvider>
                {
                    new NetworkProvider { NPI = "9876543210", Name = "Northridge Specialists", Specialty = "Cardiology", Address = "456 Care Ave", City = "Northridge", State = "CA", Zip = "91324" }
                }
            }
        };
    }
}

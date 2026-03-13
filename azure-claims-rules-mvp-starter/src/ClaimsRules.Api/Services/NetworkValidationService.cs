using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// Network validation service - validates providers against plan networks
/// Supports tiered networks with differential cost sharing
/// </summary>
public class NetworkValidationService
{
    // In production, load from Cosmos DB
    private readonly List<Provider> _providers;
    private readonly List<ProviderNetwork> _networks;
    private readonly List<PlanNetworkMapping> _planNetworkMappings;

    public NetworkValidationService()
    {
        // Initialize sample data
        _providers = new List<Provider>
        {
            new Provider
            {
                ProviderId = "PROV001",
                Npi = "1234567890",
                Name = "City Medical Center",
                ProviderType = "Hospital",
                Specialties = new() { "General Medicine", "Emergency Care" },
                Address = new ProviderAddress
                {
                    Line1 = "123 Health Plaza",
                    City = "Los Angeles",
                    State = "CA",
                    ZipCode = "90001"
                },
                NetworkIds = new() { "NET001", "NET002" },
                IsActive = true,
                QualityScore = 92,
                AcceptingNewPatients = true,
                CredentialingDate = DateTime.Parse("2023-01-01"),
                CredentialExpirationDate = DateTime.Parse("2026-12-31")
            },
            new Provider
            {
                ProviderId = "PROV002",
                Npi = "9876543210",
                Name = "Regional Health Partners",
                ProviderType = "Physician Group",
                Specialties = new() { "Primary Care", "Pediatrics" },
                Address = new ProviderAddress
                {
                    Line1 = "456 Medical Drive",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10001"
                },
                NetworkIds = new() { "NET001" },
                IsActive = true,
                QualityScore = 88,
                AcceptingNewPatients = true,
                CredentialingDate = DateTime.Parse("2024-06-01"),
                CredentialExpirationDate = DateTime.Parse("2027-06-01")
            },
            new Provider
            {
                ProviderId = "PROV003",
                Npi = "5551234567",
                Name = "Specialty Care Clinic",
                ProviderType = "Specialty Clinic",
                Specialties = new() { "Cardiology", "Orthopedics" },
                Address = new ProviderAddress
                {
                    Line1 = "789 Specialist Blvd",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60601"
                },
                NetworkIds = new() { }, // Out-of-network
                IsActive = true,
                QualityScore = 95,
                AcceptingNewPatients = false,
                CredentialingDate = DateTime.Parse("2022-03-15"),
                CredentialExpirationDate = DateTime.Parse("2025-03-15")
            }
        };

        _networks = new List<ProviderNetwork>
        {
            new ProviderNetwork
            {
                NetworkId = "NET001",
                NetworkName = "Preferred Provider Network",
                NetworkType = "Tier1",
                ReimbursementRate = 1.0m,
                ProviderIds = new() { "PROV001", "PROV002" },
                CoveredStates = new() { "CA", "NY", "TX", "FL" },
                EffectiveDate = DateTime.Parse("2023-01-01")
            },
            new ProviderNetwork
            {
                NetworkId = "NET002",
                NetworkName = "Standard Network",
                NetworkType = "Tier2",
                ReimbursementRate = 0.85m,
                ProviderIds = new() { "PROV001" },
                CoveredStates = new() { "CA", "NV", "AZ" },
                EffectiveDate = DateTime.Parse("2023-01-01")
            }
        };

        _planNetworkMappings = new List<PlanNetworkMapping>
        {
            // PLAN001 (Gold PPO) - Multiple networks
            new PlanNetworkMapping
            {
                PlanId = "PLAN001",
                NetworkId = "NET001",
                NetworkTier = "In-Network",
                CostShareMultiplier = 1.0m,
                RequiresPriorAuth = false,
                RequiresReferral = false
            },
            new PlanNetworkMapping
            {
                PlanId = "PLAN001",
                NetworkId = "NET002",
                NetworkTier = "Preferred",
                CostShareMultiplier = 1.2m, // 20% higher cost sharing
                RequiresPriorAuth = false,
                RequiresReferral = false
            },
            // PLAN002 (Silver HMO) - Single network, strict
            new PlanNetworkMapping
            {
                PlanId = "PLAN002",
                NetworkId = "NET001",
                NetworkTier = "In-Network",
                CostShareMultiplier = 1.0m,
                RequiresPriorAuth = true,
                RequiresReferral = true // HMO requires referrals
            }
        };
    }

    /// <summary>
    /// Validates provider against plan's network with cost impact calculation
    /// </summary>
    public NetworkValidationResult ValidateProvider(string providerId, string planId, DateTime serviceDate)
    {
        var result = new NetworkValidationResult
        {
            ProviderId = providerId
        };

        // Step 1: Check provider exists and is active
        var provider = _providers.FirstOrDefault(p => p.ProviderId == providerId);
        if (provider == null)
        {
            result.IsValid = false;
            result.NetworkStatus = "Provider Not Found";
            result.Errors.Add($"Provider {providerId} not found in provider directory");
            return result;
        }

        result.ProviderName = provider.Name;
        result.ProviderNpi = provider.Npi;
        result.ProviderType = provider.ProviderType;
        result.ProviderIsActive = provider.IsActive;

        if (!provider.IsActive)
        {
            result.IsValid = false;
            result.Errors.Add($"Provider {provider.Name} is not currently active");
            return result;
        }

        // Step 2: Check provider credentials are current
        result.CredentialsValid = provider.CredentialExpirationDate.HasValue &&
                                   provider.CredentialExpirationDate.Value > serviceDate;

        if (!result.CredentialsValid)
        {
            result.IsValid = false;
            result.Errors.Add($"Provider credentials expired or invalid for service date {serviceDate:yyyy-MM-dd}");
            return result;
        }

        // Step 3: Check provider is accepting patients
        if (!provider.AcceptingNewPatients)
        {
            result.Warnings.Add($"Provider {provider.Name} is not accepting new patients");
        }

        // Step 4: Find matching network for this plan and provider
        var planMappings = _planNetworkMappings.Where(m => m.PlanId == planId).ToList();
        if (!planMappings.Any())
        {
            result.IsValid = false;
            result.NetworkStatus = "Plan Configuration Error";
            result.Errors.Add($"Plan {planId} has no network mappings configured");
            return result;
        }

        // Find best network match (lowest cost multiplier)
        var matchingMappings = planMappings
            .Where(m => provider.NetworkIds.Contains(m.NetworkId))
            .OrderBy(m => m.CostShareMultiplier)
            .ToList();

        if (!matchingMappings.Any())
        {
            // Provider is out-of-network
            result.IsValid = true; // Valid but with penalty
            result.NetworkStatus = "Out-of-Network";
            result.CostShareMultiplier = 2.0m; // Double cost sharing for OON
            result.ReimbursementRate = 0.6m; // Reimburse at 60% of billed charges
            result.RequiresPriorAuth = true; // OON often requires prior auth
            result.Warnings.Add($"Provider {provider.Name} is out-of-network. Patient will have significantly higher costs.");
            result.Warnings.Add($"Cost sharing will be {result.CostShareMultiplier}x the in-network rate");
            return result;
        }

        // Provider is in-network - use best tier
        var bestMapping = matchingMappings.First();
        var network = _networks.FirstOrDefault(n => n.NetworkId == bestMapping.NetworkId);

        if (network == null)
        {
            result.IsValid = false;
            result.Errors.Add($"Network {bestMapping.NetworkId} not found");
            return result;
        }

        // Successful in-network validation
        result.IsValid = true;
        result.NetworkStatus = bestMapping.NetworkTier;
        result.NetworkId = network.NetworkId;
        result.NetworkName = network.NetworkName;
        result.CostShareMultiplier = bestMapping.CostShareMultiplier;
        result.ReimbursementRate = network.ReimbursementRate;
        result.RequiresPriorAuth = bestMapping.RequiresPriorAuth;
        result.RequiresReferral = bestMapping.RequiresReferral;

        // Add informational messages
        if (bestMapping.NetworkTier != "In-Network")
        {
            result.Warnings.Add($"Provider is in {bestMapping.NetworkTier} tier. Cost sharing multiplier: {bestMapping.CostShareMultiplier}x");
        }

        if (bestMapping.RequiresPriorAuth)
        {
            result.Warnings.Add("This plan requires prior authorization for services from this provider");
        }

        if (bestMapping.RequiresReferral)
        {
            result.Warnings.Add("HMO plan requires referral from primary care physician");
        }

        if (provider.QualityScore >= 90)
        {
            result.Warnings.Add($"High-quality provider (quality score: {provider.QualityScore}/100)");
        }

        return result;
    }

    /// <summary>
    /// Get all providers in a network
    /// </summary>
    public List<Provider> GetNetworkProviders(string networkId)
    {
        var network = _networks.FirstOrDefault(n => n.NetworkId == networkId);
        if (network == null) return new List<Provider>();

        return _providers
            .Where(p => p.NetworkIds.Contains(networkId))
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.QualityScore)
            .ToList();
    }

    /// <summary>
    /// Get all networks for a plan
    /// </summary>
    public List<ProviderNetwork> GetPlanNetworks(string planId)
    {
        var networkIds = _planNetworkMappings
            .Where(m => m.PlanId == planId)
            .Select(m => m.NetworkId)
            .Distinct()
            .ToList();

        return _networks
            .Where(n => networkIds.Contains(n.NetworkId))
            .ToList();
    }

    /// <summary>
    /// Search providers by specialty and location
    /// </summary>
    public List<Provider> SearchProviders(string? specialty = null, string? state = null, string? city = null)
    {
        var query = _providers.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(specialty))
        {
            query = query.Where(p => p.Specialties.Any(s => 
                s.Contains(specialty, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(state))
        {
            query = query.Where(p => p.Address.State.Equals(state, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(p => p.Address.City.Contains(city, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(p => p.QualityScore)
            .ThenBy(p => p.Name)
            .ToList();
    }
}

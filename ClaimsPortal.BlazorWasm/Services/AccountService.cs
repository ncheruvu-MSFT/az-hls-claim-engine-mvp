using System.Text.Json;
using Microsoft.JSInterop;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Service for managing accounts and members with localStorage persistence
/// </summary>
public class AccountService
{
    private readonly IJSRuntime _jsRuntime;
    private const string AccountsStorageKey = "claimsiq_accounts";
    private const string MembersStorageKey = "claimsiq_members";
    private List<Account> _accounts = new();
    private List<AccountMember> _members = new();

    public AccountService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Load all accounts from localStorage
    /// </summary>
    public async Task<List<Account>> GetAccountsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AccountsStorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _accounts = JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
            }
            else
            {
                _accounts = GetDefaultAccounts();
                await SaveAccountsToStorageAsync();
            }
        }
        catch (Exception)
        {
            _accounts = GetDefaultAccounts();
        }

        return _accounts;
    }

    /// <summary>
    /// Load all members from localStorage
    /// </summary>
    public async Task<List<AccountMember>> GetMembersAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", MembersStorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _members = JsonSerializer.Deserialize<List<AccountMember>>(json) ?? new List<AccountMember>();
            }
            else
            {
                _members = GetDefaultMembers();
                await SaveMembersToStorageAsync();
            }
        }
        catch (Exception)
        {
            _members = GetDefaultMembers();
        }

        return _members;
    }

    /// <summary>
    /// Get members for a specific account
    /// </summary>
    public async Task<List<AccountMember>> GetAccountMembersAsync(string accountId)
    {
        await GetMembersAsync();
        return _members.Where(m => m.AccountId == accountId).ToList();
    }

    /// <summary>
    /// Save or update an account
    /// </summary>
    public async Task<(bool Success, string Message)> SaveAccountAsync(Account account)
    {
        var validationResult = ValidateAccount(account);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.ErrorMessage);
        }

        await GetAccountsAsync();

        var existingAccount = _accounts.FirstOrDefault(a => a.AccountId == account.AccountId);
        if (existingAccount != null)
        {
            var index = _accounts.IndexOf(existingAccount);
            _accounts[index] = account;
        }
        else
        {
            _accounts.Add(account);
        }

        await SaveAccountsToStorageAsync();
        return (true, existingAccount != null ? "Account updated successfully" : "Account created successfully");
    }

    /// <summary>
    /// Save or update a member
    /// </summary>
    public async Task<(bool Success, string Message)> SaveMemberAsync(AccountMember member)
    {
        var validationResult = ValidateMember(member);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.ErrorMessage);
        }

        await GetMembersAsync();

        var existingMember = _members.FirstOrDefault(m => m.MemberId == member.MemberId);
        if (existingMember != null)
        {
            var index = _members.IndexOf(existingMember);
            _members[index] = member;
        }
        else
        {
            _members.Add(member);
        }

        await SaveMembersToStorageAsync();
        
        // Update account member count
        await UpdateAccountMemberCount(member.AccountId);
        
        return (true, existingMember != null ? "Member updated successfully" : "Member created successfully");
    }

    /// <summary>
    /// Delete an account
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAccountAsync(string accountId)
    {
        await GetAccountsAsync();
        var account = _accounts.FirstOrDefault(a => a.AccountId == accountId);
        
        if (account == null)
        {
            return (false, "Account not found");
        }

        // Also delete all members for this account
        await GetMembersAsync();
        _members.RemoveAll(m => m.AccountId == accountId);
        await SaveMembersToStorageAsync();

        _accounts.Remove(account);
        await SaveAccountsToStorageAsync();
        
        return (true, "Account and all associated members deleted successfully");
    }

    /// <summary>
    /// Delete a member
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteMemberAsync(string memberId)
    {
        await GetMembersAsync();
        var member = _members.FirstOrDefault(m => m.MemberId == memberId);
        
        if (member == null)
        {
            return (false, "Member not found");
        }

        var accountId = member.AccountId;
        _members.Remove(member);
        await SaveMembersToStorageAsync();
        
        // Update account member count
        await UpdateAccountMemberCount(accountId);
        
        return (true, "Member deleted successfully");
    }

    /// <summary>
    /// Search accounts by query
    /// </summary>
    public async Task<List<Account>> SearchAccountsAsync(string query)
    {
        await GetAccountsAsync();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            return _accounts;
        }

        query = query.ToLower();
        return _accounts.Where(a => 
            a.AccountId.ToLower().Contains(query) ||
            a.AccountName.ToLower().Contains(query) ||
            a.EmployerName.ToLower().Contains(query)
        ).ToList();
    }

    /// <summary>
    /// Filter accounts by status and plan
    /// </summary>
    public async Task<List<Account>> FilterAccountsAsync(string? status = null, string? planId = null)
    {
        await GetAccountsAsync();
        var filtered = _accounts.AsEnumerable();

        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            filtered = filtered.Where(a => a.Status == status);
        }

        if (!string.IsNullOrEmpty(planId) && planId != "All")
        {
            filtered = filtered.Where(a => a.PlanId == planId);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Update account member count
    /// </summary>
    private async Task UpdateAccountMemberCount(string accountId)
    {
        await GetAccountsAsync();
        await GetMembersAsync();
        
        var account = _accounts.FirstOrDefault(a => a.AccountId == accountId);
        if (account != null)
        {
            account.MemberCount = _members.Count(m => m.AccountId == accountId);
            await SaveAccountsToStorageAsync();
        }
    }

    /// <summary>
    /// Validate account data
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidateAccount(Account account)
    {
        if (string.IsNullOrWhiteSpace(account.AccountId))
            return (false, "Account ID is required");

        if (string.IsNullOrWhiteSpace(account.AccountName))
            return (false, "Account Name is required");

        if (string.IsNullOrWhiteSpace(account.EmployerName))
            return (false, "Employer Name is required");

        if (account.TotalPremium < 0)
            return (false, "Total Premium cannot be negative");

        return (true, string.Empty);
    }

    /// <summary>
    /// Validate member data
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidateMember(AccountMember member)
    {
        if (string.IsNullOrWhiteSpace(member.MemberId))
            return (false, "Member ID is required");

        if (string.IsNullOrWhiteSpace(member.AccountId))
            return (false, "Account ID is required");

        if (string.IsNullOrWhiteSpace(member.FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(member.LastName))
            return (false, "Last Name is required");

        if (member.DateOfBirth > DateTime.Now)
            return (false, "Date of Birth cannot be in the future");

        return (true, string.Empty);
    }

    /// <summary>
    /// Save accounts to localStorage
    /// </summary>
    private async Task SaveAccountsToStorageAsync()
    {
        var json = JsonSerializer.Serialize(_accounts);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccountsStorageKey, json);
    }

    /// <summary>
    /// Save members to localStorage
    /// </summary>
    private async Task SaveMembersToStorageAsync()
    {
        var json = JsonSerializer.Serialize(_members);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", MembersStorageKey, json);
    }

    /// <summary>
    /// Get default sample accounts
    /// </summary>
    private List<Account> GetDefaultAccounts()
    {
        return new List<Account>
        {
            new Account
            {
                AccountId = "ACC001",
                AccountName = "Northridge Healthcare Group",
                EmployerName = "Northridge Healthcare",
                PlanName = "Gold HMO Plan",
                PlanId = "HMO-001",
                MemberCount = 145,
                Status = "Active",
                TotalPremium = 87000,
                EffectiveDate = new DateTime(2025, 1, 1)
            },
            new Account
            {
                AccountId = "ACC002",
                AccountName = "Valley Tech Industries",
                EmployerName = "Valley Tech Corp",
                PlanName = "Platinum PPO Plan",
                PlanId = "PPO-002",
                MemberCount = 78,
                Status = "Active",
                TotalPremium = 46800,
                EffectiveDate = new DateTime(2024, 7, 1)
            },
            new Account
            {
                AccountId = "ACC003",
                AccountName = "Riverside Manufacturing",
                EmployerName = "Riverside Mfg Co",
                PlanName = "Silver EPO Plan",
                PlanId = "EPO-003",
                MemberCount = 52,
                Status = "Pending",
                TotalPremium = 31200,
                EffectiveDate = new DateTime(2026, 2, 1)
            }
        };
    }

    /// <summary>
    /// Get default sample members
    /// </summary>
    private List<AccountMember> GetDefaultMembers()
    {
        return new List<AccountMember>
        {
            // ACC001 Members
            new AccountMember { AccountId = "ACC001", MemberId = "M001", FirstName = "John", LastName = "Smith", Relationship = "Subscriber", DateOfBirth = new DateTime(1980, 5, 15), PlanName = "Gold HMO Plan", Status = "Active" },
            new AccountMember { AccountId = "ACC001", MemberId = "M002", FirstName = "Jane", LastName = "Smith", Relationship = "Spouse", DateOfBirth = new DateTime(1982, 8, 22), PlanName = "Gold HMO Plan", Status = "Active" },
            new AccountMember { AccountId = "ACC001", MemberId = "M003", FirstName = "Emily", LastName = "Smith", Relationship = "Child", DateOfBirth = new DateTime(2010, 3, 10), PlanName = "Gold HMO Plan", Status = "Active" },
            
            // ACC002 Members
            new AccountMember { AccountId = "ACC002", MemberId = "M004", FirstName = "Michael", LastName = "Johnson", Relationship = "Subscriber", DateOfBirth = new DateTime(1975, 11, 30), PlanName = "Platinum PPO Plan", Status = "Active" },
            new AccountMember { AccountId = "ACC002", MemberId = "M005", FirstName = "Sarah", LastName = "Johnson", Relationship = "Spouse", DateOfBirth = new DateTime(1978, 4, 18), PlanName = "Platinum PPO Plan", Status = "Active" },
            
            // ACC003 Members
            new AccountMember { AccountId = "ACC003", MemberId = "M006", FirstName = "Robert", LastName = "Williams", Relationship = "Subscriber", DateOfBirth = new DateTime(1985, 9, 25), PlanName = "Silver EPO Plan", Status = "Pending" }
        };
    }
}

using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public class AccountService(TaxFilerContext context) : IAccountService
{
    public async Task<IEnumerable<AccountDto>> GetAccountsAsync()
    {
        var accounts = await context.Accounts.ToListAsync();
        return accounts.Select(a => new AccountDto
        {
            Id = a.Id,
            Name = a.Name
        });
    }
} 
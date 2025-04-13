using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public class AccountService : IAccountService
{
    private readonly TaxFilerContext _context;

    public AccountService(TaxFilerContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AccountDto>> GetAccountsAsync()
    {
        var accounts = await _context.Accounts.ToListAsync();
        return accounts.Select(a => new AccountDto
        {
            Id = a.Id,
            Name = a.Name
        });
    }

    public async Task AddAccountAsync(AccountDto accountDto)
    {
        var account = new Account
        {
            Name = accountDto.Name
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAccountAsync(AccountDto accountDto)
    {
        var account = await _context.Accounts.FindAsync(accountDto.Id);
        if (account != null)
        {
            account.Name = accountDto.Name;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAccountAsync(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account != null)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
} 
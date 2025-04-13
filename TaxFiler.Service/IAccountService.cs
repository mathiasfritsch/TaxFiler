using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public interface IAccountService
{
    Task<IEnumerable<AccountDto>> GetAccountsAsync();
    Task AddAccountAsync(AccountDto accountDto);
    Task UpdateAccountAsync(AccountDto accountDto);
    Task DeleteAccountAsync(int id);
} 
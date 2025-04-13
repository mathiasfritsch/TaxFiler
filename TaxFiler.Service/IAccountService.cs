using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public interface IAccountService
{
    Task<IEnumerable<AccountDto>> GetAccountsAsync();
} 
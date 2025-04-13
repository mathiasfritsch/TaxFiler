using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("GetAccounts")]
    public async Task<IEnumerable<AccountDto>> GetAccounts()
    {
        return await _accountService.GetAccountsAsync();
    }

    [HttpPost("AddAccount")]
    public async Task<IActionResult> AddAccount(AccountDto accountDto)
    {
        await _accountService.AddAccountAsync(accountDto);
        return Ok();
    }

    [HttpPost("UpdateAccount")]
    public async Task<IActionResult> UpdateAccount(AccountDto accountDto)
    {
        await _accountService.UpdateAccountAsync(accountDto);
        return Ok();
    }

    [HttpDelete("DeleteAccount/{id:int}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        await _accountService.DeleteAccountAsync(id);
        return Ok();
    }
} 
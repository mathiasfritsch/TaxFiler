﻿using csvModel = TaxFiler.Model.Csv;
using  dtoModel = TaxFiler.Model.Dto;
namespace TaxFiler.Service;

public interface ITransactionService
{
    public IEnumerable<csvModel.TransactionDto> ParseTransactions(TextReader reader);
    public Task AddTransactionsAsync(IEnumerable<csvModel.TransactionDto> transactions);    
    Task<IEnumerable<dtoModel.TransactionDto>> GetTransactionsAsync(DateOnly yearMonth, int? accountId = null);
    Task<dtoModel.TransactionDto> GetTransactionAsync(int transactionid);
    Task UpdateTransactionAsync(dtoModel.UpdateTransactionDto transactionDto);
    Task<MemoryStream> CreateCsvFileAsync(DateOnly yearMonthh);
    Task DeleteTransactionAsync(int id);
}
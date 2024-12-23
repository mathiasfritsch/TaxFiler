using TaxFiler.Model.Dto;

namespace TaxFiler.Models;

public class DocumentViewModel
{
    public required DocumentDto[] Documents { get; init; }
}
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using Microsoft.Extensions.Configuration;
using TaxFiler.Model.Dto;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;

namespace TaxFiler.Service.Test;

[TestFixture]
public class DocumentMatcherTest
{
    [TestCase]
    public void TransactionDocumentMatcherService_ShouldBeInstantiableWithMockContext()
    {
        // Arrange
        IDocumentService documentService = Substitute.For<IDocumentService>();
        documentService.GetAllUnmatchedDocumentsAsync().Returns(new DocumentDto[] { });

        // Act
        var service = new TransactionDocumentMatcherService(documentService);
        var document = service.MatchTransactionToDocumentAsync(new TransactionDto());
        
        
        // Assert
       

        
    }


}
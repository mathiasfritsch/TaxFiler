using TaxFiler.Service.DocumentMatcher;

namespace TaxFiler.Service.Test;

[TestFixture]
public class FeatureExtractorVendorSimilarityTest
{
    private FeatureExtractor _featureExtractor;

    [SetUp]
    public void SetUp()
    {
        _featureExtractor = new FeatureExtractor();
    }

    [Test]
    [TestCase("PAYPAL *AMAZONSERVICES 402-935-7733", "Amazon", ExpectedResult = true)]
    [TestCase("STRIPE PAYMENT SHOPIFY INC 123-456", "Shopify", ExpectedResult = true)]
    [TestCase("WALMART SUPERCENTER #1234 ANYTOWN", "Walmart", ExpectedResult = true)]
    public bool TestVendorSimilarityWithTransactionNoteWords(string transactionNote, string documentVendor)
    {
        // Arrange
        var document = new DocumentModel
        {
            Id = 1,
            Name = "test.pdf",
            VendorName = documentVendor,
            Total = 100.0m,
            InvoiceDate = DateTime.Today
        };

        var transaction = new TransactionModel
        {
            Id = 1,
            TransactionNote = transactionNote,
            SenderReceiver = "GENERIC_PROCESSOR",
            GrossAmount = 100.0m,
            TransactionDateTime = DateTime.Today
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(document, transaction);

        // Assert - Vendor similarity should be high (> 0.6) when vendor names have good word matches
        return features.VendorSimilarity > 0.6f;
    }
}
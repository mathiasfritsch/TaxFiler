using Microsoft.ML;
using TaxFiler.Predictor.Models;
using TaxFiler.Predictor.Services;

namespace TaxFiler.Predictor.Tests;

public class MatchDocumentToTransactionTests
{
    private ModelTrainer _modelTrainer;
    private FeatureExtractor _featureExtractor;
    private ITransformer _model;

    // Test data fixtures
    private DocumentModel _matchingDocument;
    private TransactionModel _matchingTransaction;
    private DocumentModel _nonMatchingDocument;
    private TransactionModel _nonMatchingTransaction;
    private DocumentModel _edgeCaseDocument;
    private TransactionModel _edgeCaseTransaction;

    [SetUp]
    public void Setup()
    {
        _modelTrainer = new ModelTrainer();
        _featureExtractor = new FeatureExtractor();

        // Load the pre-trained model
        LoadModel();

        // Create test data fixtures
        CreateTestDataFixtures();
    }

    private void LoadModel()
    {
        var modelPath = Path.Combine("document_transaction_matcher.zip");

        if (!File.Exists(modelPath))
        {
            Assert.Fail(
                $"Model file not found at path: {modelPath}. Please ensure the model has been trained and saved.");
        }

        try
        {
            _model = _modelTrainer.LoadModel(modelPath);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to load model: {ex.Message}");
        }
    }

    private void CreateTestDataFixtures()
    {
        // Matching document-transaction pair (positive case)
        _matchingDocument = new DocumentModel
        {
            Id = 1,
            Name = "RE_SomeVendor_238.00_2024-03-15.pdf",
            InvoiceNumber = "INV-2024-001",
            InvoiceDate = new DateTime(2024, 3, 15),
            SubTotal = 200.00m,
            Total = 238.00m,
            TaxRate = 19.0m,
            TaxAmount = 38.00m,
            VendorName = "SomeVendor",
            Parsed = true,
            Orphaned = false
        };

        _matchingTransaction = new TransactionModel
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = 238.00m,
            NetAmount = 200.00m,
            TaxAmount = 38.00m,
            TaxRate = 19.0m,
            SenderReceiver = "SomeVendor Professional Services",
            TransactionNote = "Invoice INV-2024-001 Payment",
            TransactionDateTime = new DateTime(2024, 3, 16),
            IsOutgoing = true,
            IsSalesTaxRelevant = true
        };

        // Non-matching document-transaction pair (negative case)
        _nonMatchingDocument = new DocumentModel
        {
            Id = 2,
            Name = "RE_Microsoft_150.00_2024-02-10.pdf",
            InvoiceNumber = "MS-2024-567",
            InvoiceDate = new DateTime(2024, 2, 10),
            SubTotal = 126.05m,
            Total = 150.00m,
            TaxRate = 19.0m,
            TaxAmount = 23.95m,
            VendorName = "Microsoft",
            Parsed = true,
            Orphaned = false
        };

        _nonMatchingTransaction = new TransactionModel
        {
            Id = 2,
            AccountId = 1,
            GrossAmount = 500.00m,
            NetAmount = 420.17m,
            TaxAmount = 79.83m,
            TaxRate = 19.0m,
            SenderReceiver = "SomeRceiver",
            TransactionNote = "Vendor Monthly Billing",
            TransactionDateTime = new DateTime(2024, 4, 1),
            IsOutgoing = true,
            IsSalesTaxRelevant = true
        };

        // Edge case: Document with missing data
        _edgeCaseDocument = new DocumentModel
        {
            Id = 3,
            Name = "Unknown_Vendor.pdf",
            InvoiceNumber = null,
            InvoiceDate = null,
            SubTotal = null,
            Total = null,
            TaxRate = null,
            TaxAmount = null,
            VendorName = null,
            Parsed = false,
            Orphaned = true
        };

        _edgeCaseTransaction = new TransactionModel
        {
            Id = 3,
            AccountId = 1,
            GrossAmount = 100.00m,
            NetAmount = 84.03m,
            TaxAmount = 15.97m,
            TaxRate = 19.0m,
            SenderReceiver = null,
            TransactionNote = null,
            TransactionDateTime = new DateTime(2024, 1, 15),
            IsOutgoing = true,
            IsSalesTaxRelevant = true
        };
    }

    [Test]
    public void ShouldPredictPositiveMatch_WhenDocumentAndTransactionMatch()
    {
        // Arrange
        var features = _featureExtractor.ExtractFeatures(_matchingDocument, _matchingTransaction);

        // Act
        var prediction = _modelTrainer.PredictMatch(_model, features);

        // Assert
        Assert.That(prediction.Probability, Is.GreaterThan(0.7f),
            $"Prediction probability should be > 0.7 for strong matches. Actual: {prediction.Probability:F3}");

        // Log prediction details for debugging
        Console.WriteLine(
            $"Positive Match Test - Probability: {prediction.Probability:F3}, Score: {prediction.Score:F3}");
        Console.WriteLine(
            $"Features - Amount: {features.AmountSimilarity:F3}, Date: {features.DateDiffDays:F1}, Vendor: {features.VendorSimilarity:F3}");
    }

    [Test]
    public void ShouldPredictNegativeMatch_WhenDocumentAndTransactionDontMatch()
    {
        // Arrange
        var features = _featureExtractor.ExtractFeatures(_nonMatchingDocument, _nonMatchingTransaction);

        // Act
        var prediction = _modelTrainer.PredictMatch(_model, features);

        // Assert
        Assert.That(prediction.IsMatch, Is.False);

        // Log prediction details for debugging
        Console.WriteLine(
            $"Negative Match Test - Probability: {prediction.Probability:F3}, Score: {prediction.Score:F3}");
        Console.WriteLine(
            $"Features - Amount: {features.AmountSimilarity:F3}, Date: {features.DateDiffDays:F1}, Vendor: {features.VendorSimilarity:F3}");
    }

    [Test]
    public void ShouldMaintainConsistentPredictions_AcrossMultipleCalls()
    {
        // Arrange
        var features = _featureExtractor.ExtractFeatures(_matchingDocument, _matchingTransaction);

        // Act - Make multiple predictions with same input
        var prediction1 = _modelTrainer.PredictMatch(_model, features);
        var prediction2 = _modelTrainer.PredictMatch(_model, features);
        var prediction3 = _modelTrainer.PredictMatch(_model, features);

        // Assert - Results should be identical
        Assert.That(prediction1.IsMatch, Is.EqualTo(prediction2.IsMatch),
            "Multiple predictions with same input should return consistent IsMatch results");
        Assert.That(prediction1.Probability, Is.EqualTo(prediction2.Probability).Within(0.001f),
            "Multiple predictions with same input should return consistent Probability values");
        Assert.That(prediction2.IsMatch, Is.EqualTo(prediction3.IsMatch),
            "All predictions should be consistent");
        Assert.That(prediction2.Probability, Is.EqualTo(prediction3.Probability).Within(0.001f),
            "All probability values should be consistent");

        Console.WriteLine(
            $"Consistency Test - All predictions returned: IsMatch={prediction1.IsMatch}, Probability={prediction1.Probability:F3}");
    }

    [Test]
    public void ShouldHandleDocumentWithMissingAmount_WithoutCrashing()
    {
        // Arrange
        var documentWithoutAmount = new DocumentModel
        {
            Id = 10,
            Name = "Invoice_Without_Amount.pdf",
            InvoiceNumber = "INV-NO-AMOUNT",
            InvoiceDate = new DateTime(2024, 1, 15),
            Total = null, // Missing amount
            VendorName = "Test Vendor",
            Parsed = true,
            Orphaned = false
        };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var features = _featureExtractor.ExtractFeatures(documentWithoutAmount, _matchingTransaction);
            var prediction = _modelTrainer.PredictMatch(_model, features);

            // Verify the prediction is reasonable even with missing data
            Assert.That(prediction.Probability, Is.InRange(0.0f, 1.0f),
                "Prediction should still return valid probability range even with missing document amount");

            Console.WriteLine(
                $"Missing Amount Test - IsMatch: {prediction.IsMatch}, Probability: {prediction.Probability:F3}");
            Console.WriteLine($"Amount similarity with null Total: {features.AmountSimilarity:F3}");
        }, "Model should handle documents with missing amounts gracefully");
    }

    [Test]
    public void ShouldHandleDocumentWithMissingDate_WithoutCrashing()
    {
        // Arrange
        var documentWithoutDate = new DocumentModel
        {
            Id = 11,
            Name = "Invoice_Without_Date.pdf",
            InvoiceNumber = "INV-NO-DATE",
            InvoiceDate = null, // Missing date
            Total = 238.00m,
            VendorName = "Test Vendor",
            Parsed = true,
            Orphaned = false
        };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var features = _featureExtractor.ExtractFeatures(documentWithoutDate, _matchingTransaction);
            var prediction = _modelTrainer.PredictMatch(_model, features);

            // Verify the prediction is reasonable even with missing data
            Assert.That(prediction.Probability, Is.InRange(0.0f, 1.0f),
                "Prediction should still return valid probability range even with missing document date");

            Console.WriteLine(
                $"Missing Date Test - IsMatch: {prediction.IsMatch}, Probability: {prediction.Probability:F3}");
            Console.WriteLine($"Date difference with null InvoiceDate: {features.DateDiffDays:F1}");
        }, "Model should handle documents with missing dates gracefully");
    }

    [Test]
    public void ShouldHandleTransactionWithMissingData_WithoutCrashing()
    {
        // Arrange
        var transactionWithMissingData = new TransactionModel
        {
            Id = 12,
            AccountId = 1,
            GrossAmount = 238.00m,
            SenderReceiver = null, // Missing sender/receiver
            TransactionNote = null, // Missing note
            TransactionDateTime = new DateTime(2024, 3, 16),
            IsOutgoing = true,
            IsSalesTaxRelevant = true
        };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var features = _featureExtractor.ExtractFeatures(_matchingDocument, transactionWithMissingData);
            var prediction = _modelTrainer.PredictMatch(_model, features);

            // Verify the prediction is reasonable even with missing data
            Assert.That(prediction.Probability, Is.InRange(0.0f, 1.0f),
                "Prediction should still return valid probability range even with missing transaction data");

            Console.WriteLine(
                $"Missing Transaction Data Test - IsMatch: {prediction.IsMatch}, Probability: {prediction.Probability:F3}");
            Console.WriteLine($"Vendor similarity with null SenderReceiver: {features.VendorSimilarity:F3}");
        }, "Model should handle transactions with missing data gracefully");
    }

    [Test]
    public void ShouldHandleCompletelyEmptyData_WithoutCrashing()
    {
        // Arrange - Use the pre-created edge case fixtures that have mostly null values

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var features = _featureExtractor.ExtractFeatures(_edgeCaseDocument, _edgeCaseTransaction);
            var prediction = _modelTrainer.PredictMatch(_model, features);

            // Verify all features are calculated without errors
            Assert.That(features.AmountSimilarity, Is.InRange(0.0f, 1.0f),
                "Amount similarity should be in valid range even with missing data");
            Assert.That(features.VendorSimilarity, Is.InRange(0.0f, 1.0f),
                "Vendor similarity should be in valid range even with missing data");
            Assert.That(features.InvoiceNumberMatch, Is.InRange(0.0f, 1.0f),
                "Invoice number match should be in valid range even with missing data");

            // Verify prediction is reasonable
            Assert.That(prediction.Probability, Is.InRange(0.0f, 1.0f),
                "Prediction should return valid probability even with mostly empty data");

            Console.WriteLine(
                $"Empty Data Test - IsMatch: {prediction.IsMatch}, Probability: {prediction.Probability:F3}");
            Console.WriteLine(
                $"All features - Amount: {features.AmountSimilarity:F3}, Date: {features.DateDiffDays:F1}, Vendor: {features.VendorSimilarity:F3}, Invoice: {features.InvoiceNumberMatch:F3}");
        }, "Model should handle completely empty/null data gracefully without throwing exceptions");
    }
}
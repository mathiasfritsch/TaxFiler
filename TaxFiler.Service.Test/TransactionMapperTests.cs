using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class TransactionMapperTests
{
    [Test]
    public void TransactionDto_WithCorrectTax_IsTaxMismatchIsFalse()
    {
        // Arrange: GrossAmount = 119, TaxRate = 19%, Expected TaxAmount = 119 * 19 / 119 = 19
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 119.00m,
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            NetAmount = 100.00m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_WithIncorrectTax_IsTaxMismatchIsTrue()
    {
        // Arrange: GrossAmount = 119, TaxRate = 19%, but TaxAmount = 10 (incorrect)
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 119.00m,
            TaxAmount = 10.00m,
            TaxRate = 19.00m,
            NetAmount = 109.00m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.True);
    }

    [Test]
    public void TransactionDto_WithZeroTaxAmount_IsTaxMismatchIsFalse()
    {
        // Arrange: No tax, so no mismatch
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TaxAmount = 0m,
            TaxRate = 0m,
            NetAmount = 100.00m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_WithNullTaxAmount_IsTaxMismatchIsFalse()
    {
        // Arrange: Null tax values, so no validation
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TaxAmount = null,
            TaxRate = null,
            NetAmount = null,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_WithSmallRoundingError_IsTaxMismatchIsFalse()
    {
        // Arrange: Small rounding error within tolerance (0.02)
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 119.00m,
            TaxAmount = 19.01m, // 0.01 difference from expected 19.00
            TaxRate = 19.00m,
            NetAmount = 99.99m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_With7PercentTax_CalculatesCorrectly()
    {
        // Arrange: 7% tax rate (common in Germany for reduced rate items)
        // GrossAmount = 107, TaxRate = 7%, Expected TaxAmount = 107 * 7 / 107 = 7
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 107.00m,
            TaxAmount = 7.00m,
            TaxRate = 7.00m,
            NetAmount = 100.00m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_WithLargeAmounts_CalculatesCorrectly()
    {
        // Arrange: Test with larger amounts
        // GrossAmount = 11900, TaxRate = 19%, Expected TaxAmount = 11900 * 19 / 119 = 1900
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 11900.00m,
            TaxAmount = 1900.00m,
            TaxRate = 19.00m,
            NetAmount = 10000.00m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_WithDecimalAmounts_CalculatesCorrectly()
    {
        // Arrange: Test with decimal amounts
        // GrossAmount = 59.50, TaxRate = 19%, Expected TaxAmount ≈ 9.50
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 59.50m,
            TaxAmount = 9.50m,
            TaxRate = 19.00m,
            NetAmount = 50.00m,
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False);
    }

    [Test]
    public void TransactionDto_WithConfirmedTaxMismatch_IsTaxMismatchIsFalse()
    {
        // Arrange: Incorrect tax, but user has confirmed the mismatch
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 119.00m,
            TaxAmount = 10.00m, // Incorrect - should be 19.00
            TaxRate = 19.00m,
            NetAmount = 109.00m,
            IsTaxMismatchConfirmed = true, // User confirmed the mismatch
            AccountId = 1,
            Account = new Account { Id = 1, Name = "Test Account" }
        };

        // Act
        var dto = transaction.TransactionDto();

        // Assert
        Assert.That(dto.IsTaxMismatch, Is.False, "Confirmed tax mismatch should not show as error");
        Assert.That(dto.IsTaxMismatchConfirmed, Is.True, "Confirmation flag should be preserved");
    }
    
    [Test]
    public void ToTransaction_WithLongStrings_TruncatesTo200Characters()
    {
        // Arrange: Create a CSV transaction with strings longer than 200 characters
        var longString = new string('X', 250); // 250 characters
        var csvTransaction = new TaxFiler.Model.Csv.TransactionDto
        {
            BookingDate = new DateTime(2026, 1, 15),
            SenderReceiver = longString,
            CounterPartyBIC = "TESTBIC123",
            CounterPartyIBAN = longString,
            Comment = longString,
            Amount = 100.50m
        };

        // Act
        var transaction = csvTransaction.ToTransaction();

        // Assert
        Assert.That(transaction.SenderReceiver.Length, Is.EqualTo(200), "SenderReceiver should be truncated to 200 characters");
        Assert.That(transaction.Counterparty.Length, Is.EqualTo(200), "Counterparty should be truncated to 200 characters");
        Assert.That(transaction.TransactionNote.Length, Is.EqualTo(200), "TransactionNote should be truncated to 200 characters");
    }
    
    [Test]
    public void ToTransaction_WithNullStrings_ReturnsEmptyStrings()
    {
        // Arrange: Create a CSV transaction with null strings
        var csvTransaction = new TaxFiler.Model.Csv.TransactionDto
        {
            BookingDate = new DateTime(2026, 1, 15),
            SenderReceiver = null!,
            CounterPartyBIC = "TESTBIC123",
            CounterPartyIBAN = null!,
            Comment = null!,
            Amount = 100.50m
        };

        // Act
        var transaction = csvTransaction.ToTransaction();

        // Assert
        Assert.That(transaction.SenderReceiver, Is.EqualTo(string.Empty), "SenderReceiver should be empty string");
        Assert.That(transaction.Counterparty, Is.EqualTo(string.Empty), "Counterparty should be empty string");
        Assert.That(transaction.TransactionNote, Is.EqualTo(string.Empty), "TransactionNote should be empty string");
    }
}

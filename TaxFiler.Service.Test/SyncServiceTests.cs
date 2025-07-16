using NUnit.Framework;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace TaxFiler.Service.Test;

[TestFixture]
public class SyncServiceTests
{
    [Test]
    public async Task SyncFilesAsync_ShouldSetInvoiceDateFromFolder_WhenCreatingNewDocuments()
    {
        // Arrange
        var testDate = new DateOnly(2024, 3, 15);
        var testFile = new FileData { Id = "test-file-id", Name = "test-document.pdf" };
        var files = new List<FileData> { testFile };
        
        var mockGoogleDriveService = Substitute.For<IGoogleDriveService>();
        mockGoogleDriveService.GetFilesAsync(testDate).Returns(Task.FromResult(files));
        
        // Create in-memory database context with custom options
        var options = new DbContextOptionsBuilder<TaxFilerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        using var context = new TaxFilerContext(options);
        context.Database.EnsureCreated();
        
        var syncService = new SyncService(context, mockGoogleDriveService);
        
        // Act
        await syncService.SyncFilesAsync(testDate);
        
        // Assert
        var document = await context.Documents.FirstOrDefaultAsync(d => d.ExternalRef == testFile.Id);
        Assert.That(document, Is.Not.Null);
        Assert.That(document.Name, Is.EqualTo(testFile.Name));
        Assert.That(document.InvoiceDateFromFolder, Is.EqualTo(testDate));
        Assert.That(document.Orphaned, Is.False);
    }
    
    [Test]
    public async Task SyncFilesAsync_ShouldNotUpdateExistingDocuments_WhenDocumentsAlreadyExist()
    {
        // Arrange
        var testDate = new DateOnly(2024, 3, 15);
        var existingDate = new DateOnly(2024, 2, 10);
        var testFile = new FileData { Id = "existing-file-id", Name = "existing-document.pdf" };
        var files = new List<FileData> { testFile };
        
        var mockGoogleDriveService = Substitute.For<IGoogleDriveService>();
        mockGoogleDriveService.GetFilesAsync(testDate).Returns(Task.FromResult(files));
        
        // Create in-memory database context with custom options
        var options = new DbContextOptionsBuilder<TaxFilerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        using var context = new TaxFilerContext(options);
        context.Database.EnsureCreated();
        
        // Create existing document with different InvoiceDateFromFolder
        var existingDocument = new Document
        {
            Name = testFile.Name,
            ExternalRef = testFile.Id,
            Orphaned = true,
            InvoiceDateFromFolder = existingDate
        };
        context.Documents.Add(existingDocument);
        await context.SaveChangesAsync();
        
        var syncService = new SyncService(context, mockGoogleDriveService);
        
        // Act
        await syncService.SyncFilesAsync(testDate);
        
        // Assert
        var document = await context.Documents.FirstOrDefaultAsync(d => d.ExternalRef == testFile.Id);
        Assert.That(document, Is.Not.Null);
        Assert.That(document.InvoiceDateFromFolder, Is.EqualTo(existingDate), "InvoiceDateFromFolder should not be updated for existing documents");
        Assert.That(document.Orphaned, Is.False, "Orphaned status should be updated to false");
    }
}
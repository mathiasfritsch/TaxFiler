using TaxFiler.Predictor.Models;
using TaxFiler.Predictor.Services;

namespace TaxFiler.Predictor;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Document-Transaction Matching with ML.NET");
        Console.WriteLine("=========================================");
        
        var dataLoader = new DataLoader();
        var modelTrainer = new ModelTrainer();
        var featureExtractor = new FeatureExtractor();
        
        try
        {
            // Check if we should generate sample data
            if (args.Length > 0 && args[0] == "--generate-data")
            {
                await GenerateSampleData();
                return;
            }
            
            // Load data
            Console.WriteLine("Loading documents and transactions...");
            var documents = dataLoader.LoadDocuments("Documents.csv");
            var transactions = dataLoader.LoadTransactions("Transactions.csv");
            
            Console.WriteLine($"Loaded {documents.Count} documents and {transactions.Count} transactions");
            
            // Generate training data
            Console.WriteLine("Generating training data...");
            var trainingData = dataLoader.GenerateTrainingData(documents, transactions);
            
            var positiveExamples = trainingData.Count(t => t.IsMatch);
            var negativeExamples = trainingData.Count(t => !t.IsMatch);
            Console.WriteLine($"Training data: {positiveExamples} positive, {negativeExamples} negative examples");
            
            // Save training data for inspection
            dataLoader.SaveTrainingData(trainingData, "Data/training_data.csv");
            Console.WriteLine("Training data saved to Data/training_data.csv");
            
            // Train model
            var model = modelTrainer.TrainModel(trainingData);

            // Create Models directory if it doesn't exist
            Directory.CreateDirectory("Models");

            // Save trained model
            modelTrainer.SaveModel(model, "Models/document_transaction_matcher.zip");
            
            // Demonstrate predictions
            Console.WriteLine("\nDemonstrating predictions on known matches:");
            DemonstratePredictions(model, documents, transactions, featureExtractor, modelTrainer);
            
            // Find new matches
            Console.WriteLine("\nSearching for new matches:");
            var newMatches = modelTrainer.FindBestMatches(model, documents, transactions, 0.8f);
            
            Console.WriteLine($"Found {newMatches.Count} high-confidence matches:");
            foreach (var match in newMatches.Take(10))
            {
                Console.WriteLine($"Document {match.Document.Id} ({match.Document.Name}) ↔ " +
                                $"Transaction {match.Transaction.Id} " +
                                $"(Confidence: {match.Prediction.Probability:F3})");
            }
            
            // Feature importance analysis
            modelTrainer.AnalyzeFeatureImportance(model, trainingData.Take(100).ToList());
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    
    static void DemonstratePredictions(
        Microsoft.ML.ITransformer model,
        List<DocumentModel> documents,
        List<TransactionModel> transactions,
        FeatureExtractor featureExtractor,
        ModelTrainer modelTrainer)
    {
        // Test some known matches
        var knownMatches = new[]
        {
            (DocId: 10, TransId: 12, Name: "Some Name Invoice 3356"),
            (DocId: 5, TransId: 13, Name: "DB Train Ticket"),
            (DocId: 17, TransId: 9, Name: "Some Name Consulting")
        };
        
        foreach (var match in knownMatches)
        {
            var document = documents.FirstOrDefault(d => d.Id == match.DocId);
            var transaction = transactions.FirstOrDefault(t => t.Id == match.TransId);
            
            if (document != null && transaction != null)
            {
                var features = featureExtractor.ExtractFeatures(document, transaction);
                var prediction = modelTrainer.PredictMatch(model, features);
                
                Console.WriteLine($"{match.Name}:");
                Console.WriteLine($"  Document: {document.Name} (€{document.Total})");
                Console.WriteLine($"  Transaction: {transaction.TransactionNote?.Substring(0, Math.Min(50, transaction.TransactionNote.Length))}... (€{transaction.GrossAmount})");
                Console.WriteLine($"  Prediction: {(prediction.IsMatch ? "MATCH" : "NO MATCH")} " +
                                $"(Confidence: {prediction.Probability:F3})");
                Console.WriteLine($"  Features: Amount={features.AmountSimilarity:F2}, Date={features.DateDiffDays:F2}, " +
                                $"Vendor={features.VendorSimilarity:F2}, Invoice={features.InvoiceNumberMatch:F2}");
                Console.WriteLine();
            }
        }
    }
    
    static async Task GenerateSampleData()
    {
        Console.WriteLine("Generating sample CSV data from markdown tables...");
        
        // Create Data directory if it doesn't exist
        Directory.CreateDirectory("Data");
        
        // This would parse the markdown tables and generate CSV files
        // For now, we'll create sample CSV structure
        var sampleDocumentsCsv = @"Id,Name,ExternalRef,Orphaned,Parsed,InvoiceNumber,InvoiceDate,SubTotal,Total,TaxRate,TaxAmount,Skonto
5,DB_Rechnung_375244107208.pdf,1yPUHm2XSk-dgWsm_-ZEkgI5WT9fCBkf5,true,true,2025-375244107208,2025-06-10,95.21,101.88,7,6.67,0
10,RE_3356.pdf,1f6XUndRorHMFxrp_2tfMQGyRlgSr1ie3,true,true,3356,2025-06-13,40,47.6,19,7.6,0
16,RE_3289.pdf,1wzezdmXpj_NCf8zQq1bkeoTCNay7wz8R,false,true,3289,2025-05-06,40,47.6,19,7.6,0";
        
        var sampleTransactionsCsv = @"Id,AccountId,NetAmount,GrossAmount,TaxAmount,TaxRate,Counterparty,TransactionReference,TransactionDateTime,TransactionNote,IsOutgoing,IsIncomeTaxRelevant,IsSalesTaxRelevant,TaxMonth,TaxYear,DocumentId,SenderReceiver
13,1,95.21,101.88,6.67,7,LU89751000135104200E,,2025-06-11,DB Vertrieb GmbH Ihr Einkauf bei DB Vertrieb GmbH,true,true,true,0,0,5,PayPal Europe S.a.r.l. et Cie S.C.A
12,1,40,47.6,7.6,19,DE06200505501238211229,,2025-06-13,Rng 3356 Mandant 52391,true,true,true,0,0,10,Some Name
25,1,40,47.6,7.6,0,DE06200505501238211229,,2025-05-07,Rng 3289 Mandant 52391,true,true,true,0,0,16,Some Name";
        
        await File.WriteAllTextAsync("Data/documents.csv", sampleDocumentsCsv);
        await File.WriteAllTextAsync("Data/transactions.csv", sampleTransactionsCsv);
        
        Console.WriteLine("Sample CSV files generated in Data/ directory");
    }
}
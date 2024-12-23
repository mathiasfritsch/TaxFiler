using System.ComponentModel.DataAnnotations;

namespace TaxFiler.DB.Model;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    
    [MaxLength(200)]
    public String Counterparty { get; set; }
    
    [MaxLength(200)]
    public String TransactionReference { get; set; }
    public DateTime TransactionDateTime { get; set; }
    
    [MaxLength(200)]
    public string TransactionNote { get; set; }
   
}
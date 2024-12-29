using System.ComponentModel.DataAnnotations;

namespace TaxFiler.DB.Model
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string ExternalRef { get; set; }
        public bool Orphaned { get; set; }
        public bool Parsed { get; set; }
        [MaxLength(100)]
        public string? InvoiceNumber { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? Total { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxAmount { get; set; }
        public int TaxMonth { get; set; }
        public int TaxYear { get; set; }
    }
}
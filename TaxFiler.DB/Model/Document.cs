using System.ComponentModel.DataAnnotations;

namespace TaxFiler.DB.Model
{
    public class Document
    {
        public Document(){
        }

        public Document(
            string name,
            string externalRef,
            bool orphaned,
            decimal? taxRate,
            decimal? taxAmount,
            decimal? total,
            decimal? subTotal,
            DateOnly? invoiceDate,
            string? invoiceNumber,
            bool parsed,
            decimal? skonto,
            string? vendorName = null,
            DateOnly? invoiceDateFromFolder = null){
            Name = name;
            ExternalRef = externalRef;
            Orphaned = orphaned;
            TaxRate = taxRate;
            TaxAmount = taxAmount;
            Total = total;
            SubTotal = subTotal;
            InvoiceDate = invoiceDate;
            InvoiceNumber = invoiceNumber;
            Parsed = parsed;
            Skonto = skonto;
            VendorName = vendorName;
            InvoiceDateFromFolder = invoiceDateFromFolder;
        }

        [Key] public int Id { get; set; }
        [MaxLength(200)] public string Name { get; set; }
        [MaxLength(50)] public string ExternalRef { get; set; }
        public bool Orphaned { get; set; }
        public bool Parsed { get; set; }
        [MaxLength(100)] public string? InvoiceNumber { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? Total { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? Skonto { get; set; }
        [MaxLength(200)] public string? VendorName { get; set; }
        public DateOnly? InvoiceDateFromFolder { get; set; }
        
        /// <summary>
        /// Collection of document attachments for this document.
        /// Supports this document being attached to multiple transactions.
        /// </summary>
        public ICollection<DocumentAttachment> DocumentAttachments { get; set; } = new List<DocumentAttachment>();
    }
}
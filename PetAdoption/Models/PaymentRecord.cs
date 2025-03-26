namespace PetAdoption.Models
{
    public class PaymentRecord
    {
        public int Id { get; set; }
        public string? UserEmail { get; set; }          // Nullable or required?
        public string? OrderId { get; set; }            // Nullable or required?
        public string? RazorpayPaymentId { get; set; }  // Nullable
        public decimal Amount { get; set; }
        public string? Status { get; set; }             // Nullable or required?
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }      // Nullable
    }
}
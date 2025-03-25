namespace PetAdoption.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string? UserEmail { get; set; }
        public string? ProductName { get; set; }
        public string? ProductPrice { get; set; }
        public string? ImageUrl { get; set; } // New property
        public int Quantity { get; set; } = 1;
        public DateTime AddedAt { get; set; }
    }
}
namespace PetAdoption.Models
{
    public class VetConsultation
    {
        public int Id { get; set; }
        public string PetName { get; set; }
        public string PetType { get; set; }
        public int PetAge { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public string OwnerPhone { get; set; }
        public string Concern { get; set; }
        public DateTime PreferredDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
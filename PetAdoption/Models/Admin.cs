namespace PetAdoption.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }  // Note: In production, you should hash passwords
    }
}
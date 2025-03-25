namespace PetAdoption.Models
{
    public class DogTrainingBooking
    {
        public int Id { get; set; }
        public bool PuppyTraining { get; set; }
        public bool BasicObedienceTraining { get; set; }
        public bool BehaviorCorrectionTraining { get; set; }
        public string Location { get; set; }
        public string DogBreed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
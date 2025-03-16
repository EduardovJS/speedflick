namespace SpeedFlick.Models
{
    public class UserScore
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string  Username { get; set; }
        public string AvatarUrl { get; set; }
        public int Score { get; set; }
    }
}

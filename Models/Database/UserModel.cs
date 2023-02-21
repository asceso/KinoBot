namespace Models.Database
{
    public class UserModel
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsSubscribedForNotifications { get; set; }
    }
}
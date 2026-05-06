namespace Gym
{
    public enum UserRole { Administrator, Trainer, Client }

    public class Member
    {
        public int    MemberId  { get; set; }
        public string FirstName { get; set; }
        public string LastName  { get; set; }
        public System.DateTime BirthDate { get; set; }
        public string Phone     { get; set; }
        public string Email     { get; set; }
        public string Notes     { get; set; }

        public string FullName { get { return LastName + " " + FirstName; } }
    }
}

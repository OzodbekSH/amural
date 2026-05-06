namespace Gym
{
    public class UserAccount
    {
        public int      UserId      { get; set; }
        public string   Login       { get; set; }
        public string   Password    { get; set; }
        public UserRole Role        { get; set; }
        public string   DisplayName { get; set; }
        public int?     MemberId    { get; set; }
    }
}

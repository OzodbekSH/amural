namespace Gym
{
    public static class AppState
    {
        static AppState()
        {
            Repository = new GymRepository();
        }

        public static GymRepository Repository  { get; private set; }
        public static UserAccount   CurrentUser { get; set; }
    }
}

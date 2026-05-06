namespace Gym
{
    public class Subscription
    {
        public int    SubscriptionId     { get; set; }
        public int    SubscriptionNumber { get; set; }
        public int    MemberId           { get; set; }
        public string PlanName           { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate   { get; set; }
        public decimal Price             { get; set; }
        public bool    IsActive          { get; set; }
    }
}

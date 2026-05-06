namespace Gym
{
    public class WeeklyVisitReportItem
    {
        public System.DateTime VisitDate        { get; set; }
        public string          MemberName       { get; set; }
        public string          SubscriptionPlan { get; set; }
    }

    public class TopVisitorReportItem
    {
        public string MemberName  { get; set; }
        public int    VisitsCount { get; set; }
    }
}

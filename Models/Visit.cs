namespace Gym
{
    public class Visit
    {
        public int    VisitId            { get; set; }
        public int    MemberId           { get; set; }
        public System.DateTime VisitDate { get; set; }
        public int    RegisteredByUserId { get; set; }
        public string Comment            { get; set; }
    }
}

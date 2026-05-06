using System.Data.Entity;
using System.Data.Entity.SqlServer;

namespace Gym
{
    public class GymDbConfiguration : DbConfiguration
    {
        public GymDbConfiguration()
        {
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
        }
    }

    [DbConfigurationType(typeof(GymDbConfiguration))]
    public class GymDbContext : DbContext
    {
        static GymDbContext()
        {
            // DB is managed by GymClub.sql — disable EF auto-migration
            Database.SetInitializer<GymDbContext>(null);
        }

        public GymDbContext() : base("name=GymDb") { }

        public DbSet<Member>       Members       { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Visit>        Visits        { get; set; }
        public DbSet<UserAccount>  Users         { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // UserAccount maps to "Users" table (EF would default to "UserAccounts")
            modelBuilder.Entity<UserAccount>().ToTable("Users");
        }
    }
}

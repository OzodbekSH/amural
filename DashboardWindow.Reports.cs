using System.Linq;
using System.Windows;

namespace Gym
{
    public partial class DashboardWindow : Window
    {
        private void LoadWeekReport_Click(object sender, RoutedEventArgs e)
        {
            ReportsGrid.ItemsSource = AppState.Repository.GetActiveVisitsForWeek()
                .Select(x => new
                {
                    Дата      = x.VisitDate.ToString("dd.MM.yyyy"),
                    Клиент    = x.MemberName,
                    Абонемент = x.SubscriptionPlan
                })
                .ToList();
        }

        private void LoadTopReport_Click(object sender, RoutedEventArgs e)
        {
            ReportsGrid.ItemsSource = AppState.Repository.GetTopVisitors()
                .Select(x => new
                {
                    Клиент               = x.MemberName,
                    КоличествоПосещений  = x.VisitsCount
                })
                .ToList();
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gym
{
    public partial class DashboardWindow : Window
    {
        private void SubscriptionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic row = SubscriptionsGrid.SelectedItem;
            if (row == null) return;

            var s = AppState.Repository.Subscriptions.First(x => x.SubscriptionId == (int)row.SubscriptionId);
            _selectedSubscriptionId                  = s.SubscriptionId;
            SubscriptionMemberComboBox.SelectedValue = s.MemberId;
            SubscriptionPlanComboBox.SelectedItem    = s.PlanName;
            SubscriptionStartDateInput.SelectedDate  = s.StartDate;
            SubscriptionEndDateInput.SelectedDate    = s.EndDate;
            SubscriptionPriceInput.Text              = s.Price.ToString("0.##");
            SubscriptionIsActiveInput.IsChecked      = s.IsActive;
        }

        private void AddSubscription_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                AppState.Repository.AddSubscription(
                    (int)SubscriptionMemberComboBox.SelectedValue,
                    SubscriptionPlanComboBox.SelectedItem as string,
                    SubscriptionStartDateInput.SelectedDate ?? DateTime.Today,
                    SubscriptionEndDateInput.SelectedDate ?? DateTime.Today.AddMonths(1),
                    ParseDecimal(SubscriptionPriceInput.Text, "Введите корректную стоимость."),
                    SubscriptionIsActiveInput.IsChecked == true);

                RefreshAllData();
                ClearSubscriptionInputs();
            });
        }

        private void UpdateSubscription_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedSubscriptionId == 0)
                    throw new InvalidOperationException("Выберите абонемент.");

                AppState.Repository.UpdateSubscription(
                    _selectedSubscriptionId,
                    (int)SubscriptionMemberComboBox.SelectedValue,
                    SubscriptionPlanComboBox.SelectedItem as string,
                    SubscriptionStartDateInput.SelectedDate ?? DateTime.Today,
                    SubscriptionEndDateInput.SelectedDate ?? DateTime.Today.AddMonths(1),
                    ParseDecimal(SubscriptionPriceInput.Text, "Введите корректную стоимость."),
                    SubscriptionIsActiveInput.IsChecked == true);

                RefreshAllData();
            });
        }

        private void DeleteSubscription_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedSubscriptionId == 0)
                    throw new InvalidOperationException("Выберите абонемент.");

                AppState.Repository.DeleteSubscription(_selectedSubscriptionId);
                RefreshAllData();
                ClearSubscriptionInputs();
            });
        }

        private void ClearSubscription_Click(object sender, RoutedEventArgs e)
        {
            ClearSubscriptionInputs();
        }

        private void ClearSubscriptionInputs()
        {
            _selectedSubscriptionId                 = 0;
            SubscriptionsGrid.SelectedItem          = null;
            SubscriptionStartDateInput.SelectedDate = DateTime.Today;
            SubscriptionEndDateInput.SelectedDate   = DateTime.Today.AddMonths(1);
            SubscriptionPriceInput.Text             = "3000";
            SubscriptionIsActiveInput.IsChecked     = true;
            if (SubscriptionPlanComboBox.Items.Count > 0)
                SubscriptionPlanComboBox.SelectedIndex = 0;
            if (SubscriptionMemberComboBox.Items.Count > 0)
                SubscriptionMemberComboBox.SelectedIndex = 0;
        }
    }
}

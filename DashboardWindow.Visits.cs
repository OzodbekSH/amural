using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gym
{
    public partial class DashboardWindow : Window
    {
        private void VisitsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic row = VisitsGrid.SelectedItem;
            if (row == null) return;

            var visit = AppState.Repository.Visits.First(v => v.VisitId == (int)row.VisitId);
            _selectedVisitId              = visit.VisitId;
            VisitMemberComboBox.SelectedValue = visit.MemberId;
            VisitDateInput.SelectedDate   = visit.VisitDate;
            VisitCommentInput.Text        = visit.Comment;
        }

        private void AddVisit_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                AppState.Repository.RegisterVisit(
                    (int)VisitMemberComboBox.SelectedValue,
                    VisitDateInput.SelectedDate ?? DateTime.Today,
                    AppState.CurrentUser.UserId,
                    VisitCommentInput.Text);

                RefreshAllData();
                ClearVisitInputs();
            });
        }

        private void UpdateVisit_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedVisitId == 0)
                    throw new InvalidOperationException("Выберите посещение.");

                AppState.Repository.UpdateVisit(
                    _selectedVisitId,
                    (int)VisitMemberComboBox.SelectedValue,
                    VisitDateInput.SelectedDate ?? DateTime.Today,
                    VisitCommentInput.Text);

                RefreshAllData();
            });
        }

        private void DeleteVisit_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedVisitId == 0)
                    throw new InvalidOperationException("Выберите посещение.");

                AppState.Repository.DeleteVisit(_selectedVisitId);
                RefreshAllData();
                ClearVisitInputs();
            });
        }

        private void ClearVisit_Click(object sender, RoutedEventArgs e)
        {
            ClearVisitInputs();
        }

        private void ClearVisitInputs()
        {
            _selectedVisitId              = 0;
            VisitsGrid.SelectedItem       = null;
            VisitDateInput.SelectedDate   = DateTime.Today;
            VisitCommentInput.Text        = string.Empty;
            if (VisitMemberComboBox.Items.Count > 0)
                VisitMemberComboBox.SelectedIndex = 0;
        }
    }
}

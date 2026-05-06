using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gym
{
    public partial class DashboardWindow : Window
    {
        private void MembersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic row = MembersGrid.SelectedItem;
            if (row == null) return;

            var member = AppState.Repository.Members.First(m => m.MemberId == (int)row.MemberId);
            _selectedMemberId = member.MemberId;
            MemberFirstNameInput.Text        = member.FirstName;
            MemberLastNameInput.Text         = member.LastName;
            MemberBirthDateInput.SelectedDate = member.BirthDate;
            MemberPhoneInput.Text            = member.Phone;
            MemberEmailInput.Text            = member.Email;
            MemberNotesInput.Text            = member.Notes;
        }

        private void AddMember_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                AppState.Repository.AddMember(
                    MemberFirstNameInput.Text,
                    MemberLastNameInput.Text,
                    MemberBirthDateInput.SelectedDate ?? DateTime.Today,
                    MemberPhoneInput.Text,
                    MemberEmailInput.Text,
                    MemberNotesInput.Text);

                RefreshSelectors();
                RefreshAllData();
                ClearMemberInputs();
            });
        }

        private void UpdateMember_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedMemberId == 0)
                    throw new InvalidOperationException("Выберите клиента.");

                AppState.Repository.UpdateMember(
                    _selectedMemberId,
                    MemberFirstNameInput.Text,
                    MemberLastNameInput.Text,
                    MemberBirthDateInput.SelectedDate ?? DateTime.Today,
                    MemberPhoneInput.Text,
                    MemberEmailInput.Text,
                    MemberNotesInput.Text);

                RefreshSelectors();
                RefreshAllData();
            });
        }

        private void DeleteMember_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedMemberId == 0)
                    throw new InvalidOperationException("Выберите клиента.");

                AppState.Repository.DeleteMember(_selectedMemberId);
                RefreshSelectors();
                RefreshAllData();
                ClearMemberInputs();
            });
        }

        private void ClearMember_Click(object sender, RoutedEventArgs e)
        {
            ClearMemberInputs();
        }

        private void ClearMemberInputs()
        {
            _selectedMemberId = 0;
            MembersGrid.SelectedItem         = null;
            MemberFirstNameInput.Text        = string.Empty;
            MemberLastNameInput.Text         = string.Empty;
            MemberBirthDateInput.SelectedDate = new DateTime(1998, 1, 1);
            MemberPhoneInput.Text            = string.Empty;
            MemberEmailInput.Text            = string.Empty;
            MemberNotesInput.Text            = string.Empty;
        }
    }
}

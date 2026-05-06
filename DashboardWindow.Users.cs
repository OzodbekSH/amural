using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gym
{
    public partial class DashboardWindow : Window
    {
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic row = UsersGrid.SelectedItem;
            if (row == null) return;

            var user = AppState.Repository.Users.First(u => u.UserId == (int)row.UserId);
            _selectedUserId                  = user.UserId;
            UserLoginInput.Text              = user.Login;
            UserPasswordInput.Text           = user.Password;
            UserDisplayNameInput.Text        = user.DisplayName;
            UserRoleComboBox.SelectedItem    = GetRoleText(user.Role);
            UserMemberComboBox.SelectedValue = user.MemberId;

            var isSelf = user.UserId == AppState.CurrentUser.UserId;
            DeleteUserButton.IsEnabled   = !isSelf;
            UserRoleComboBox.IsEnabled   = !isSelf;

            UpdateUserMemberState();
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                var role = _roleLabels[UserRoleComboBox.SelectedItem as string ?? "Клиент"];
                var memberId = role == UserRole.Client ? (int?)UserMemberComboBox.SelectedValue : null;

                AppState.Repository.AddUser(
                    UserLoginInput.Text,
                    UserPasswordInput.Text,
                    role,
                    UserDisplayNameInput.Text,
                    memberId);

                RefreshAllData();
                ClearUserInputs();
            });
        }

        private void UpdateUser_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedUserId == 0)
                    throw new System.InvalidOperationException("Выберите пользователя.");

                var role = _selectedUserId == AppState.CurrentUser.UserId
                    ? AppState.CurrentUser.Role
                    : _roleLabels[UserRoleComboBox.SelectedItem as string ?? "Клиент"];

                var memberId = role == UserRole.Client ? (int?)UserMemberComboBox.SelectedValue : null;

                AppState.Repository.UpdateUser(
                    _selectedUserId,
                    UserLoginInput.Text,
                    UserPasswordInput.Text,
                    role,
                    UserDisplayNameInput.Text,
                    memberId);

                if (_selectedUserId == AppState.CurrentUser.UserId)
                {
                    AppState.CurrentUser.DisplayName = UserDisplayNameInput.Text.Trim();
                    UserText.Text = AppState.CurrentUser.DisplayName + " (" + GetRoleText(AppState.CurrentUser.Role) + ")";
                }

                RefreshAllData();
            });
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAction(() =>
            {
                if (_selectedUserId == 0)
                    throw new System.InvalidOperationException("Выберите пользователя.");

                if (_selectedUserId == AppState.CurrentUser.UserId)
                    throw new System.InvalidOperationException("Нельзя удалить собственную учётную запись.");

                AppState.Repository.DeleteUser(_selectedUserId);
                RefreshAllData();
                ClearUserInputs();
            });
        }

        private void ClearUser_Click(object sender, RoutedEventArgs e)
        {
            ClearUserInputs();
        }

        private void UserRoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateUserMemberState();
        }

        private void ClearUserInputs()
        {
            _selectedUserId               = 0;
            UsersGrid.SelectedItem        = null;
            UserLoginInput.Text           = string.Empty;
            UserPasswordInput.Text        = string.Empty;
            UserDisplayNameInput.Text     = string.Empty;
            UserRoleComboBox.SelectedItem = "Клиент";
            var canManage = AppState.CurrentUser.Role == UserRole.Administrator;
            DeleteUserButton.IsEnabled = canManage;
            UserRoleComboBox.IsEnabled = canManage;
            if (UserMemberComboBox.Items.Count > 0)
                UserMemberComboBox.SelectedIndex = 0;
            UpdateUserMemberState();
        }

        private void UpdateUserMemberState()
        {
            var label = UserRoleComboBox.SelectedItem as string ?? "Клиент";
            var needMember = _roleLabels.TryGetValue(label, out var selectedRole) && selectedRole == UserRole.Client;
            UserMemberComboBox.IsEnabled = needMember && UserRoleComboBox.IsEnabled;

            if (!needMember)
                UserMemberComboBox.SelectedItem = null;
            else if (UserMemberComboBox.Items.Count > 0 && UserMemberComboBox.SelectedIndex < 0)
                UserMemberComboBox.SelectedIndex = 0;
        }
    }
}

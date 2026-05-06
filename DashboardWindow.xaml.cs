using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gym
{
    public partial class DashboardWindow : Window
    {
        private static readonly Dictionary<string, UserRole> _roleLabels = new Dictionary<string, UserRole>
        {
            { "Администратор", UserRole.Administrator },
            { "Тренер",        UserRole.Trainer },
            { "Клиент",        UserRole.Client }
        };

        private int _selectedMemberId;
        private int _selectedVisitId;
        private int _selectedSubscriptionId;
        private int _selectedUserId;

        public DashboardWindow()
        {
            InitializeComponent();
            InitializeScreen();
        }

        private void InitializeScreen()
        {
            UserText.Text = AppState.CurrentUser.DisplayName + " (" + GetRoleText(AppState.CurrentUser.Role) + ")";
            VisitDateInput.SelectedDate = DateTime.Today;
            MemberBirthDateInput.SelectedDate = new DateTime(1998, 1, 1);
            SubscriptionStartDateInput.SelectedDate = DateTime.Today;
            SubscriptionEndDateInput.SelectedDate = DateTime.Today.AddMonths(1);
            UserRoleComboBox.ItemsSource = _roleLabels.Keys.ToList();
            UserRoleComboBox.SelectedItem = "Клиент";
            SubscriptionPlanComboBox.ItemsSource = AppState.Repository.PlanCatalog.ToList();
            SubscriptionPlanComboBox.SelectedIndex = 0;

            RefreshSelectors();
            RefreshAllData();
            ApplyRights();
            ShowSection(MembersSection, MembersNavButton, "Работа с карточками клиентов.");
        }

        private void RefreshSelectors()
        {
            var visibleMembers = AppState.Repository.GetVisibleMembers(AppState.CurrentUser);
            var allMembers = AppState.Repository.Members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToList();

            VisitMemberComboBox.ItemsSource = visibleMembers;
            SubscriptionMemberComboBox.ItemsSource = visibleMembers;
            UserMemberComboBox.ItemsSource = allMembers;

            if (VisitMemberComboBox.Items.Count > 0 && VisitMemberComboBox.SelectedIndex < 0)
                VisitMemberComboBox.SelectedIndex = 0;

            if (SubscriptionMemberComboBox.Items.Count > 0 && SubscriptionMemberComboBox.SelectedIndex < 0)
                SubscriptionMemberComboBox.SelectedIndex = 0;

            if (UserMemberComboBox.Items.Count > 0 && UserMemberComboBox.SelectedIndex < 0)
                UserMemberComboBox.SelectedIndex = 0;
        }

        private void RefreshAllData()
        {
            MembersGrid.ItemsSource = AppState.Repository.GetVisibleMembers(AppState.CurrentUser)
                .Select(m => new
                {
                    m.MemberId,
                    m.FirstName,
                    m.LastName,
                    BirthDate = m.BirthDate.ToString("dd.MM.yyyy"),
                    m.Phone,
                    m.Email,
                    m.Notes
                })
                .ToList();

            VisitsGrid.ItemsSource = AppState.Repository.GetVisibleVisits(AppState.CurrentUser)
                .Select(v => new
                {
                    v.VisitId,
                    MemberName = AppState.Repository.GetMemberName(v.MemberId),
                    VisitDate = v.VisitDate.ToString("dd.MM.yyyy"),
                    v.Comment
                })
                .ToList();

            SubscriptionsGrid.ItemsSource = AppState.Repository.GetVisibleSubscriptions(AppState.CurrentUser)
                .Select(s => new
                {
                    s.SubscriptionId,
                    s.SubscriptionNumber,
                    MemberName = AppState.Repository.GetMemberName(s.MemberId),
                    s.PlanName,
                    StartDate = s.StartDate.ToString("dd.MM.yyyy"),
                    EndDate = s.EndDate.ToString("dd.MM.yyyy"),
                    Price = s.Price.ToString("0.##"),
                    IsActiveText = s.IsActive ? "Да" : "Нет"
                })
                .ToList();

            UsersGrid.ItemsSource = AppState.Repository.GetVisibleUsers(AppState.CurrentUser)
                .Select(u => new
                {
                    u.UserId,
                    u.Login,
                    RoleText = GetRoleText(u.Role),
                    u.DisplayName,
                    LinkedMember = u.MemberId.HasValue ? AppState.Repository.GetMemberName(u.MemberId.Value) : "-"
                })
                .ToList();
        }

        private void ApplyRights()
        {
            var role = AppState.CurrentUser.Role;
            var canEditMembers        = role == UserRole.Administrator || role == UserRole.Trainer;
            var canEditVisits         = role == UserRole.Administrator || role == UserRole.Trainer;
            var canManageSubscriptions = role == UserRole.Administrator;
            var canManageUsers        = role == UserRole.Administrator;
            var canViewReports        = role == UserRole.Administrator || role == UserRole.Trainer;

            AddMemberButton.IsEnabled    = canEditMembers;
            UpdateMemberButton.IsEnabled = canEditMembers;
            DeleteMemberButton.IsEnabled = canEditMembers;
            MemberFirstNameInput.IsEnabled  = canEditMembers;
            MemberLastNameInput.IsEnabled   = canEditMembers;
            MemberBirthDateInput.IsEnabled  = canEditMembers;
            MemberPhoneInput.IsEnabled      = canEditMembers;
            MemberEmailInput.IsEnabled      = canEditMembers;
            MemberNotesInput.IsEnabled      = canEditMembers;

            AddVisitButton.IsEnabled    = canEditVisits;
            UpdateVisitButton.IsEnabled = canEditVisits;
            DeleteVisitButton.IsEnabled = canEditVisits;
            VisitMemberComboBox.IsEnabled = canEditVisits;
            VisitDateInput.IsEnabled      = canEditVisits;
            VisitCommentInput.IsEnabled   = canEditVisits;

            AddSubscriptionButton.IsEnabled    = canManageSubscriptions;
            UpdateSubscriptionButton.IsEnabled = canManageSubscriptions;
            DeleteSubscriptionButton.IsEnabled = canManageSubscriptions;
            SubscriptionMemberComboBox.IsEnabled  = canManageSubscriptions;
            SubscriptionPlanComboBox.IsEnabled    = canManageSubscriptions;
            SubscriptionStartDateInput.IsEnabled  = canManageSubscriptions;
            SubscriptionEndDateInput.IsEnabled    = canManageSubscriptions;
            SubscriptionPriceInput.IsEnabled      = canManageSubscriptions;
            SubscriptionIsActiveInput.IsEnabled   = canManageSubscriptions;

            AddUserButton.IsEnabled    = canManageUsers;
            UpdateUserButton.IsEnabled = canManageUsers;
            DeleteUserButton.IsEnabled = canManageUsers;
            UserLoginInput.IsEnabled       = canManageUsers;
            UserPasswordInput.IsEnabled    = canManageUsers;
            UserDisplayNameInput.IsEnabled = canManageUsers;
            UserRoleComboBox.IsEnabled     = canManageUsers;
            UserMemberComboBox.IsEnabled   = canManageUsers;

            UsersNavButton.IsEnabled   = canManageUsers;
            ReportsNavButton.IsEnabled = canViewReports;
        }

        private void OpenMembers_Click(object sender, RoutedEventArgs e)
        {
            ShowSection(MembersSection, MembersNavButton, "Работа с карточками клиентов.");
        }

        private void OpenVisits_Click(object sender, RoutedEventArgs e)
        {
            ShowSection(VisitsSection, VisitsNavButton, "Регистрация и редактирование посещений.");
        }

        private void OpenSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            ShowSection(SubscriptionsSection, SubscriptionsNavButton, "Добавление, изменение и удаление абонементов.");
        }

        private void OpenUsers_Click(object sender, RoutedEventArgs e)
        {
            if (UsersNavButton.IsEnabled)
                ShowSection(UsersSection, UsersNavButton, "Компактный список пользователей и редактирование учетных записей.");
        }

        private void OpenReports_Click(object sender, RoutedEventArgs e)
        {
            if (ReportsNavButton.IsEnabled)
                ShowSection(ReportsSection, ReportsNavButton, "Отчеты по посещениям и рейтингу клиентов.");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            AppState.CurrentUser = null;
            Close();
        }

        private void ShowSection(UIElement section, Button activeButton, string hint)
        {
            MembersSection.Visibility       = Visibility.Collapsed;
            VisitsSection.Visibility        = Visibility.Collapsed;
            SubscriptionsSection.Visibility = Visibility.Collapsed;
            UsersSection.Visibility         = Visibility.Collapsed;
            ReportsSection.Visibility       = Visibility.Collapsed;

            MembersNavButton.Opacity       = 0.85;
            VisitsNavButton.Opacity        = 0.85;
            SubscriptionsNavButton.Opacity = 0.85;
            UsersNavButton.Opacity         = 0.85;
            ReportsNavButton.Opacity       = 0.85;

            section.Visibility    = Visibility.Visible;
            activeButton.Opacity  = 1.0;
            HeaderHintText.Text   = hint;
        }

        private void ExecuteAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static decimal ParseDecimal(string value, string errorMessage)
        {
            decimal result;
            if (!decimal.TryParse(value, out result))
                throw new InvalidOperationException(errorMessage);
            return result;
        }

        private static string GetRoleText(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator: return "Администратор";
                case UserRole.Trainer:       return "Тренер";
                default:                     return "Клиент";
            }
        }
    }
}

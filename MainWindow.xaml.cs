using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gym
{
    public partial class MainWindow : Window
    {
        private readonly Brush _activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F74C0"));
        private readonly Brush _inactiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCEBFF"));
        private readonly Brush _darkTextBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#164A8A"));

        public MainWindow()
        {
            InitializeComponent();
            RegisterBirthDateInput.SelectedDate = new DateTime(1998, 1, 1);
            RegisterPlanComboBox.ItemsSource = AppState.Repository.PlanCatalog.ToList();
            RegisterPlanComboBox.SelectedIndex = 0;
            UpdateRegistrationPrice();
            ShowLoginPanel();
            SetStatus("Введите логин и пароль или зарегистрируйте нового клиента.");
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var login = LoginInput.Text.Trim();
            var password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Введите логин и пароль.");
                MessageBox.Show("Введите логин и пароль.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var user = AppState.Repository.Authenticate(login, password);
            if (user == null)
            {
                SetStatus("Неверный логин или пароль.");
                MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppState.CurrentUser = user;
            SetStatus("Выполняется вход в систему.");

            var dashboard = new DashboardWindow();
            Hide();
            dashboard.ShowDialog();
            Show();

            PasswordInput.Password = string.Empty;
            if (AppState.CurrentUser == null)
            {
                SetStatus("Вы вышли из системы.");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var password = RegisterPasswordInput.Password;
                var confirmPassword = RegisterConfirmPasswordInput.Password;

                if (password != confirmPassword)
                {
                    throw new InvalidOperationException("Пароли не совпадают.");
                }

                var account = AppState.Repository.RegisterClientAccount(
                    RegisterFirstNameInput.Text,
                    RegisterLastNameInput.Text,
                    RegisterBirthDateInput.SelectedDate ?? DateTime.Today,
                    RegisterPhoneInput.Text,
                    RegisterEmailInput.Text,
                    RegisterNotesInput.Text,
                    RegisterLoginInput.Text.Trim(),
                    password,
                    RegisterPlanComboBox.SelectedItem as string,
                    ParseInt(RegisterMonthsInput.Text, "Введите корректный срок абонемента."),
                    ParseDecimal(RegisterPriceInput.Text, "Введите корректную стоимость."));

                LoginInput.Text = account.Login;
                PasswordInput.Password = string.Empty;
                ClearRegistrationForm();
                ShowLoginPanel();
                SetStatus("Регистрация завершена. Теперь войдите под новым аккаунтом.");
                MessageBox.Show("Регистрация прошла успешно. Теперь можно войти в систему.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SwitchToRegister_Click(object sender, RoutedEventArgs e)
        {
            ShowRegisterPanel();
            SetStatus("Заполните форму регистрации нового клиента.");
        }

        private void SwitchToLogin_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginPanel();
            SetStatus("Введите логин и пароль для входа.");
        }

        private void ShowLoginPanel()
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
            ShowLoginButton.Background = _activeBrush;
            ShowLoginButton.Foreground = Brushes.White;
            ShowRegisterButton.Background = _inactiveBrush;
            ShowRegisterButton.Foreground = _darkTextBrush;
        }

        private void ShowRegisterPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
            ShowRegisterButton.Background = _activeBrush;
            ShowRegisterButton.Foreground = Brushes.White;
            ShowLoginButton.Background = _inactiveBrush;
            ShowLoginButton.Foreground = _darkTextBrush;
        }

        private void ClearRegistrationForm()
        {
            RegisterFirstNameInput.Text = string.Empty;
            RegisterLastNameInput.Text = string.Empty;
            RegisterBirthDateInput.SelectedDate = new DateTime(1998, 1, 1);
            RegisterPhoneInput.Text = string.Empty;
            RegisterEmailInput.Text = string.Empty;
            RegisterLoginInput.Text = string.Empty;
            RegisterPasswordInput.Password = string.Empty;
            RegisterConfirmPasswordInput.Password = string.Empty;
            RegisterPlanComboBox.SelectedIndex = 0;
            RegisterMonthsInput.Text = "1";
            UpdateRegistrationPrice();
            RegisterNotesInput.Text = string.Empty;
        }

        private static int ParseInt(string value, string errorMessage)
        {
            int result;
            if (!int.TryParse(value, out result))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return result;
        }

        private static decimal ParseDecimal(string value, string errorMessage)
        {
            decimal result;
            if (!decimal.TryParse(value, out result))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return result;
        }

        private void RegisterPlan_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateRegistrationPrice();
        }

        private void RegisterMonths_Changed(object sender, TextChangedEventArgs e)
        {
            UpdateRegistrationPrice();
        }

        private void UpdateRegistrationPrice()
        {
            var plan = RegisterPlanComboBox.SelectedItem as string;
            if (plan == null) return;
            if (!int.TryParse(RegisterMonthsInput.Text, out var months) || months <= 0) return;
            decimal rate;
            if (!AppState.Repository.PlanPrices.TryGetValue(plan, out rate)) return;
            RegisterPriceInput.Text = (rate * months).ToString("0");
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}

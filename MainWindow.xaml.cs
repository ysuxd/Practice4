using Npgsql;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.Common;

namespace Practice
{

    public class DatabaseConnection
    {
        private string connectionString = "Server=localhost;Port=5432;Database=practice;User Id=postgres;Password=12345";
        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
    
    public partial class MainWindow : Window
    {
        private DatabaseConnection dbConnection;
        public MainWindow()
        {
            InitializeComponent();
            dbConnection = new DatabaseConnection();
            LoginTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Проверка ввода
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowErrorMessage("Введите логин и пароль");
                return;
            }

            try
            {
                // Проверка пользователя в базе данных
                using (var connection = dbConnection.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT u.userid, u.login, u.isblocked, r.rolename 
                        FROM users u
                        LEFT JOIN roles r ON u.roleid = r.roleid
                        WHERE u.login = @login AND u.password = @password";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Проверка блокировки
                                bool isBlocked = reader.GetBoolean(reader.GetOrdinal("isblocked"));
                                if (isBlocked)
                                {
                                    ShowErrorMessage("Пользователь заблокирован. Обратитесь к администратору.");
                                    return;
                                }

                                // Получаем роль пользователя
                                string roleName = reader.GetString(reader.GetOrdinal("rolename"));
                                int userId = reader.GetInt32(reader.GetOrdinal("userid"));

                                // Запускаем соответствующее окно
                                OpenUserWindow(userId, roleName, login);
                            }
                            else
                            {
                                ShowErrorMessage("Неверный логин или пароль");
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                ShowErrorMessage($"Ошибка базы данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка: {ex.Message}");
            }
        }
        private void OpenUserWindow(int userId, string roleName,  string login)
        {
            this.Hide();

            if (roleName == "Администратор")
            {
                // Создаем AdminWindow без параметров
                AdminWindow adminWindow = new AdminWindow();
                // Сохраняем данные пользователя в свойствах окна
                adminWindow.CurrentUserId = userId;
                adminWindow.CurrentUsername = login;
                adminWindow.CurrentRole = roleName;

                adminWindow.Closed += (s, args) =>
                {
                    this.Show();
                    ClearFields();
                };
                adminWindow.Show();
            }
            else if (roleName == "Клиент")
            {
                // Создаем UserWindow без параметров
                ClientIntefaceWindow clientIntefaceWindow = new ClientIntefaceWindow();
                // Сохраняем данные пользователя в свойствах
                clientIntefaceWindow.CurrentUserId = userId;
                clientIntefaceWindow.CurrentUsername = login;
                clientIntefaceWindow.CurrentRole = roleName;

                clientIntefaceWindow.Closed += (s, args) =>
                {
                    this.Show();
                    ClearFields();
                };
                clientIntefaceWindow.Show();
            }
            else if (roleName == "Сотрудник")
            {
                // Создаем UserWindow без параметров
                EmployeeInterfaceWindow employeeInterfaceWindow = new EmployeeInterfaceWindow();
                // Сохраняем данные пользователя в свойствах
                employeeInterfaceWindow.CurrentUserId = userId;
                employeeInterfaceWindow.CurrentUsername = login;
                employeeInterfaceWindow.CurrentRole = roleName;

                employeeInterfaceWindow.Closed += (s, args) =>
                {
                    this.Show();
                    ClearFields();
                };
                employeeInterfaceWindow.Show();
            }
        }
            private void ShowErrorMessage(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void ClearFields()
        {
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            LoginTextBox.Focus();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Owner = this;

            registrationWindow.Closed += (s, args) =>
            {
                this.Show();
                ClearFields();
            };

            this.Hide();
            registrationWindow.Show();
        }
        // Обработка нажатия Enter для удобства
        private void LoginTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }
    }
}


    
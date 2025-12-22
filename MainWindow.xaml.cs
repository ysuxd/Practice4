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

        // Альтернативный способ - по имени столбца
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            Console.WriteLine($"Попытка авторизации: login={login}, password={password}");

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowErrorMessage("Введите логин и пароль");
                return;
            }

            try
            {
                using (var connection = dbConnection.GetConnection())
                {
                    connection.Open();
                    Console.WriteLine("Подключение к БД установлено");

                    // УПРОЩЕННЫЙ запрос без JOIN сначала
                    string simpleQuery = @"
                SELECT userid, login, isblocked, roleid 
                FROM users 
                WHERE login = @login AND password = @password";

                    Console.WriteLine($"Выполняем запрос: {simpleQuery}");
                    Console.WriteLine($"Параметры: login={login}, password={password}");

                    using (var command = new NpgsqlCommand(simpleQuery, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.WriteLine("Пользователь найден, читаем данные...");

                                // Читаем по индексам
                                int userId = reader.GetInt32(0);          // userid
                                string dbLogin = reader.GetString(1);     // login
                                bool isBlocked = reader.GetBoolean(2);    // isblocked
                                int roleId = reader.GetInt32(3);          // roleid

                                Console.WriteLine($"Полученные данные:");
                                Console.WriteLine($"  userId: {userId}");
                                Console.WriteLine($"  login: {dbLogin}");
                                Console.WriteLine($"  isBlocked: {isBlocked}");
                                Console.WriteLine($"  roleId: {roleId}");

                                if (userId == 0)
                                {
                                    Console.WriteLine("ВНИМАНИЕ: userId = 0!");
                                    ShowErrorMessage("Ошибка: ID пользователя = 0");
                                    return;
                                }

                                if (isBlocked)
                                {
                                    ShowErrorMessage("Пользователь заблокирован");
                                    return;
                                }

                                // Теперь получаем название роли
                                reader.Close(); // Закрываем первый reader

                                string roleQuery = "SELECT rolename FROM roles WHERE roleid = @roleid";
                                using (var roleCmd = new NpgsqlCommand(roleQuery, connection))
                                {
                                    roleCmd.Parameters.AddWithValue("@roleid", roleId);
                                    string roleName = roleCmd.ExecuteScalar()?.ToString() ?? "Клиент";
                                    Console.WriteLine($"Роль: {roleName}");

                                    OpenUserWindow(userId, roleName, login);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Пользователь не найден");
                                ShowErrorMessage("Неверный логин или пароль");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                ShowErrorMessage($"Ошибка: {ex.Message}");
            }
        }
        private void OpenUserWindow(int userId, string roleName, string login)
        {
            Console.WriteLine($"=== OpenUserWindow ===");
            Console.WriteLine($"userId={userId}, role={roleName}, login={login}");

            this.Hide();

            if (roleName == "Администратор")
            {
                // Используйте конструктор с параметрами
                AdminWindow adminWindow = new AdminWindow(userId, login, roleName);
                adminWindow.Closed += (s, args) => this.Show();
                adminWindow.Show();
            }
            else if (roleName == "Клиент")
            {
                // Используйте конструктор с параметрами
                ClientIntefaceWindow clientWindow = new ClientIntefaceWindow(userId, login, roleName);
                clientWindow.Closed += (s, args) => this.Show();
                clientWindow.Show();
            }
            else if (roleName == "Сотрудник")
            {
                // Используйте конструктор с параметрами
                EmployeeInterfaceWindow employeeWindow = new EmployeeInterfaceWindow(userId, login, roleName);
                employeeWindow.Closed += (s, args) => this.Show();
                employeeWindow.Show();
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


    
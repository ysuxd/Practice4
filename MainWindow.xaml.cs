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
using System.Windows.Threading;

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
        private string currentCaptcha;

        public MainWindow()
        {
            InitializeComponent();
            dbConnection = new DatabaseConnection();
            LoginTextBox.Focus();
            GenerateCaptcha();
        }

        

        // Генерация капчи
        private void GenerateCaptcha()
        {
            // Символы для капчи (исключаем похожие символы: 0/O, 1/I/l, 2/Z, 5/S, 8/B)
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz234679";
            Random random = new Random();

            // Генерируем 5-6 случайных символов
            StringBuilder captcha = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                captcha.Append(chars[random.Next(chars.Length)]);
            }

            // Добавляем одну цифру
            captcha.Append(random.Next(2, 10));

            currentCaptcha = captcha.ToString();
            CaptchaTextBlock.Text = currentCaptcha;

            // Добавляем легкие искажения
            CaptchaTextBlock.LayoutTransform = new RotateTransform(random.Next(-5, 6));
            CaptchaTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(
                (byte)random.Next(100, 200),
                (byte)random.Next(100, 200),
                (byte)random.Next(100, 200)
            ));

            // Очищаем поле ввода капчи
            CaptchaTextBox.Text = "";
            CaptchaTextBox.Focus();
        }

        // Проверка капчи
        private bool ValidateCaptcha()
        {
            string userInput = CaptchaTextBox.Text.Trim();

            // Сравниваем без учета регистра и пробелов
            return string.Equals(userInput, currentCaptcha, StringComparison.OrdinalIgnoreCase);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Сначала скрываем предыдущую ошибку
            HideErrorMessage();

            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string captcha = CaptchaTextBox.Text.Trim();

            Console.WriteLine($"Попытка авторизации: login={login}");

            // Проверка ввода логина
            if (string.IsNullOrEmpty(login))
            {
                ShowErrorMessage("Введите логин");
                LoginTextBox.Focus();
                return;
            }

            // Проверка ввода пароля
            if (string.IsNullOrEmpty(password))
            {
                ShowErrorMessage("Введите пароль");
                PasswordBox.Focus();
                return;
            }

            // Проверка ввода капчи
            if (string.IsNullOrEmpty(captcha))
            {
                ShowErrorMessage("Введите код с картинки");
                CaptchaTextBox.Focus();
                return;
            }

            // Проверка капчи
            if (!ValidateCaptcha())
            {
                ShowErrorMessage("Неверный код с картинки");
                GenerateCaptcha();
                CaptchaTextBox.Focus();
                CaptchaTextBox.SelectAll();
                return;
            }

            try
            {
                using (var connection = dbConnection.GetConnection())
                {
                    connection.Open();
                    Console.WriteLine("Подключение к БД установлено");

                    // 1. Проверяем, существует ли пользователь и получаем текущий falllog
                    string checkUserQuery = "SELECT userid, isblocked, falllog FROM users WHERE login = @login";
                    int userId = 0;
                    bool isBlocked = false;
                    int falllog = 0;
                    bool userExists = false;

                    using (var checkCmd = new NpgsqlCommand(checkUserQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userExists = true;
                                userId = reader.GetInt32(0);
                                isBlocked = reader.GetBoolean(1);
                                falllog = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                                Console.WriteLine($"Найден пользователь: ID={userId}, isBlocked={isBlocked}, falllog={falllog}");
                            }
                            else
                            {
                                Console.WriteLine($"Пользователь с логином '{login}' не найден");
                                ShowErrorMessage("Неверный логин");
                                GenerateCaptcha();
                                return;
                            }
                        }
                    }

                    // Проверяем блокировку
                    if (isBlocked)
                    {
                        ShowErrorMessage("Пользователь заблокирован. Обратитесь к администратору.");
                        GenerateCaptcha();
                        return;
                    }

                    // 2. Проверяем логин и пароль
                    string authQuery = @"
                SELECT userid, login, isblocked, roleid 
                FROM users 
                WHERE login = @login AND password = @password";

                    Console.WriteLine($"Проверка пароля для пользователя {userId}");

                    using (var authCmd = new NpgsqlCommand(authQuery, connection))
                    {
                        authCmd.Parameters.AddWithValue("@login", login);
                        authCmd.Parameters.AddWithValue("@password", password);

                        using (var reader = authCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // УСПЕШНАЯ АВТОРИЗАЦИЯ
                                int authUserId = reader.GetInt32(0);
                                string dbLogin = reader.GetString(1);
                                bool authIsBlocked = reader.GetBoolean(2);
                                int roleId = reader.GetInt32(3);

                                Console.WriteLine($"Успешная авторизация: userid={authUserId}, roleid={roleId}");

                                reader.Close();

                                // Сбрасываем счетчик неудачных попыток
                                string resetFalllogQuery = "UPDATE users SET falllog = 0 WHERE userid = @userid";
                                using (var resetCmd = new NpgsqlCommand(resetFalllogQuery, connection))
                                {
                                    resetCmd.Parameters.AddWithValue("@userid", authUserId);
                                    int rowsAffected = resetCmd.ExecuteNonQuery();
                                    Console.WriteLine($"Сброшен falllog для пользователя {authUserId}. Затронуто строк: {rowsAffected}");
                                }

                                // Проверяем блокировку (на всякий случай)
                                if (authIsBlocked)
                                {
                                    ShowErrorMessage("Пользователь заблокирован");
                                    return;
                                }

                                // Получаем название роли
                                string roleQuery = "SELECT rolename FROM roles WHERE roleid = @roleid";
                                using (var roleCmd = new NpgsqlCommand(roleQuery, connection))
                                {
                                    roleCmd.Parameters.AddWithValue("@roleid", roleId);
                                    object result = roleCmd.ExecuteScalar();
                                    string roleName = result?.ToString() ?? "Клиент";
                                    Console.WriteLine($"Роль: {roleName}");

                                    // Скрываем ошибку перед открытием окна
                                    HideErrorMessage();
                                    OpenUserWindow(authUserId, roleName, dbLogin);
                                }
                            }
                            else
                            {
                                // НЕУДАЧНАЯ АВТОРИЗАЦИЯ - НЕВЕРНЫЙ ПАРОЛЬ
                                reader.Close();
                                Console.WriteLine($"Неверный пароль для пользователя {userId}");

                                // Увеличиваем счетчик неудачных попыток
                                int newFalllog = falllog + 1;
                                Console.WriteLine($"Неудачная попытка. falllog: {falllog} -> {newFalllog}");

                                string updateQuery;
                                string errorMessage;

                                if (newFalllog >= 3)
                                {
                                    // Блокируем пользователя
                                    updateQuery = "UPDATE users SET falllog = @falllog, isblocked = true WHERE userid = @userid";
                                    errorMessage = "Неверный пароль. Пользователь заблокирован после 3 неудачных попыток. Обратитесь к администратору.";
                                }
                                else
                                {
                                    updateQuery = "UPDATE users SET falllog = @falllog WHERE userid = @userid";
                                    errorMessage = $"Неверный пароль. Осталось попыток: {3 - newFalllog}";
                                }

                                // Выполняем обновление
                                using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@falllog", newFalllog);
                                    updateCmd.Parameters.AddWithValue("@userid", userId);
                                    int rowsAffected = updateCmd.ExecuteNonQuery();
                                    Console.WriteLine($"Обновлен falllog. Затронуто строк: {rowsAffected}");
                                }

                                ShowErrorMessage(errorMessage);
                                GenerateCaptcha();
                                CaptchaTextBox.Focus();
                                CaptchaTextBox.SelectAll();
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException npgEx)
            {
                // Обработка ошибок PostgreSQL
                Console.WriteLine($"Ошибка PostgreSQL: {npgEx.Message}");
                ShowErrorMessage("Ошибка подключения к базе данных. Проверьте подключение к серверу.");
                GenerateCaptcha();
            }
            catch (Exception ex)
            {
                // Обработка других ошибок
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // Проверяем, это ошибка подключения или что-то другое
                if (ex.Message.Contains("connection") || ex.Message.Contains("баз") || ex.Message.Contains("database"))
                {
                    ShowErrorMessage("Ошибка подключения к базе данных. Проверьте подключение к серверу.");
                }
                else
                {
                    ShowErrorMessage($"Ошибка: {ex.Message}");
                }

                GenerateCaptcha();
            }
        }

        private void OpenUserWindow(int userId, string roleName, string login)
        {
            Console.WriteLine($"=== OpenUserWindow ===");
            Console.WriteLine($"userId={userId}, role={roleName}, login={login}");

            this.Hide();

            if (roleName == "Администратор")
            {
                AdminWindow adminWindow = new AdminWindow(userId, login, roleName);
                adminWindow.Closed += (s, args) =>
                {
                    this.Show();
                    ClearFields();
                };
                adminWindow.Show();
            }
            else if (roleName == "Клиент")
            {
                ClientIntefaceWindow clientWindow = new ClientIntefaceWindow(userId, login, roleName);
                clientWindow.Closed += (s, args) =>
                {
                    this.Show();
                    ClearFields();
                };
                clientWindow.Show();
            }
            else if (roleName == "Сотрудник")
            {
                EmployeeInterfaceWindow employeeWindow = new EmployeeInterfaceWindow(userId, login, roleName);
                employeeWindow.Closed += (s, args) =>
                {
                    this.Show();
                    ClearFields();
                };
                employeeWindow.Show();
            }
        }

        private void ShowErrorMessage(string message)
        {
            Console.WriteLine($"ShowErrorMessage вызван с сообщением: '{message}'");

            // Устанавливаем текст
            ErrorMessageTextBlock.Text = message;

            // Делаем видимым
            ErrorMessageBorder.Visibility = Visibility.Visible;

            // Проверяем, что текст установлен
            Console.WriteLine($"Установлен текст: '{ErrorMessageTextBlock.Text}'");
            Console.WriteLine($"Видимость установлена: {ErrorMessageBorder.Visibility}");

            // Автоматически скрываем сообщение через 5 секунд
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, args) =>
            {
                Console.WriteLine("Таймер скрытия сработал");
                ErrorMessageBorder.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
        private void HideErrorMessage()
        {
            ErrorMessageBorder.Visibility = Visibility.Collapsed;
        }

        private void ClearFields()
        {
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
            CaptchaTextBox.Text = "";
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            GenerateCaptcha(); // Генерируем новую капчу
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

        // Кнопка обновления капчи
        private void RefreshCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
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
                CaptchaTextBox.Focus();
            }
        }

        private void CaptchaTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }
    }
}
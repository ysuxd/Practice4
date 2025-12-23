using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Practice
{
    public partial class RegistrationWindow : Window
    {
        private DatabaseConnection dbConnection;
        private const int CLIENT_ROLE_ID = 3; // Исправлено: роль "Клиент" имеет ID = 3

        public RegistrationWindow()
        {
            InitializeComponent();
            dbConnection = new DatabaseConnection();
            LastNameTextBox.Focus();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем предыдущие сообщения
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            

            // Получаем данные из полей
            string lastName = LastNameTextBox.Text.Trim();
            string firstName = FirstNameTextBox.Text.Trim();
            string surname = SurnameTextBox.Text.Trim();
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Валидация данных
            if (string.IsNullOrEmpty(lastName))
            {
                ShowErrorMessage("Введите фамилию");
                LastNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(firstName))
            {
                ShowErrorMessage("Введите имя");
                FirstNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(login))
            {
                ShowErrorMessage("Введите логин");
                LoginTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowErrorMessage("Введите пароль");
                PasswordBox.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                ShowErrorMessage("Пароли не совпадают");
                ConfirmPasswordBox.Focus();
                return;
            }

            if (password.Length < 6)
            {
                ShowErrorMessage("Пароль должен содержать минимум 6 символов");
                PasswordBox.Focus();
                return;
            }

            try
            {
                using (var connection = dbConnection.GetConnection())
                {
                    connection.Open();

                    // Начинаем транзакцию для обеспечения целостности данных
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Проверяем, не существует ли уже такой логин
                            string checkLoginQuery = "SELECT COUNT(*) FROM users WHERE login = @login";
                            using (var checkCommand = new NpgsqlCommand(checkLoginQuery, connection))
                            {
                                checkCommand.Transaction = transaction;
                                checkCommand.Parameters.AddWithValue("@login", login);
                                int loginCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                                if (loginCount > 0)
                                {
                                    ShowErrorMessage("Пользователь с таким логином уже существует");
                                    return;
                                }
                            }

                            // 2. Добавляем пользователя в таблицу users с ролью "Клиент" (roleid = 3)
                            int userId;
                            string insertUserQuery = @"
                                INSERT INTO users (login, password, roleid, isblocked) 
                                VALUES (@login, @password, @roleid, false) 
                                RETURNING userid";

                            using (var userCommand = new NpgsqlCommand(insertUserQuery, connection))
                            {
                                userCommand.Transaction = transaction;
                                userCommand.Parameters.AddWithValue("@login", login);
                                userCommand.Parameters.AddWithValue("@password", password);
                                userCommand.Parameters.AddWithValue("@roleid", CLIENT_ROLE_ID); // Исправлено: roleid = 3

                                userId = Convert.ToInt32(userCommand.ExecuteScalar());
                            }

                            // 3. Добавляем клиента в таблицу client
                            string insertClientQuery = @"
                                INSERT INTO client (lastname, firstname, surname, userid) 
                                VALUES (@lastname, @firstname, @surname, @userid)";

                            using (var clientCommand = new NpgsqlCommand(insertClientQuery, connection))
                            {
                                clientCommand.Transaction = transaction;
                                clientCommand.Parameters.AddWithValue("@lastname", lastName);
                                clientCommand.Parameters.AddWithValue("@firstname", firstName);
                                clientCommand.Parameters.AddWithValue("@surname",
                                    string.IsNullOrEmpty(surname) ? (object)DBNull.Value : surname);
                                clientCommand.Parameters.AddWithValue("@userid", userId);

                                clientCommand.ExecuteNonQuery();
                            }

                            // Подтверждаем транзакцию
                            transaction.Commit();

                           

                            // Очищаем поля через 3 секунды и закрываем окно
                            ClearFields();

                            // Автоматическое закрытие окна через 3 секунды
                            var timer = new System.Windows.Threading.DispatcherTimer();
                            timer.Interval = TimeSpan.FromSeconds(3);
                            timer.Tick += (s, args) =>
                            {
                                timer.Stop();
                                this.Close();
                            };
                            timer.Start();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Ошибка при регистрации: {ex.Message}");
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        

        private void ClearFields()
        {
            LastNameTextBox.Text = "";
            FirstNameTextBox.Text = "";
            SurnameTextBox.Text = "";
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }

        // Обработка нажатия Enter для удобства
        private void LastNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FirstNameTextBox.Focus();
            }
        }

        private void FirstNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SurnameTextBox.Focus();
            }
        }

        private void SurnameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginTextBox.Focus();
            }
        }

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
                ConfirmPasswordBox.Focus();
            }
        }

        private void ConfirmPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                RegisterButton_Click(sender, e);
            }
        }
    }
}
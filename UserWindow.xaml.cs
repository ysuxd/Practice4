using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Practice
{
    public partial class UserWindow : Window
    {
        private DatabaseConnection dbconnection;
        private DataTable rolesTable;

        public UserWindow()
        {
            InitializeComponent();
            dbconnection = new DatabaseConnection();
            LoadRoles(); // Загружаем роли в ComboBox
            LoadData();
        }

        // Загрузка ролей из таблицы Role в ComboBox
        private void LoadRoles()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT roleid, rolename FROM roles ORDER BY rolename";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            rolesTable = new DataTable();
                            adapter.Fill(rolesTable);
                            RoleComboBox.ItemsSource = rolesTable.DefaultView;

                            if (rolesTable.Rows.Count > 0)
                            {
                                RoleComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных пользователей с JOIN к таблице Role
        public void LoadData()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            u.userid, 
                            u.login, 
                            u.password, 
                            r.rolename,
                            u.isblocked,
                            u.roleid
                        FROM users u
                        LEFT JOIN roles r ON u.roleid = r.roleid
                        ORDER BY u.userid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            UserDataGrid.ItemsSource = dataTable.DefaultView;

                            // Обновляем счетчик пользователей
                            UpdateUsersCount(dataTable.Rows.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для обновления счетчика пользователей
        private void UpdateUsersCount(int count)
        {
            UsersCountText.Text = $"Всего пользователей: {count}";
        }

        // Добавление пользователя
        private void AddUser(string login, string password, int roleid, bool isblocked)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = @"
                    INSERT INTO users (login, password, roleid, isblocked) 
                    VALUES (@login, @password, @roleid, @isblocked)";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@roleid", roleid);
                    command.Parameters.AddWithValue("@isblocked", isblocked);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Обновление пользователя
        private void UpdateUser(int userid, string login, string password, int roleid, bool isblocked)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = @"
                    UPDATE users 
                    SET login = @login, 
                        password = @password, 
                        roleid = @roleid, 
                        isblocked = @isblocked 
                    WHERE userid = @userid";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@roleid", roleid);
                    command.Parameters.AddWithValue("@isblocked", isblocked);
                    command.Parameters.AddWithValue("@userid", userid);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Удаление пользователя
        private void DeleteUser(int userid)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить этого пользователя?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM users WHERE userid = @userid";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userid", userid);
                        command.ExecuteNonQuery();
                    }
                }
                LoadData();
                ClearInputFields();
            }
        }

        // Очистка полей ввода
        private void ClearInputFields()
        {
            LoginTextBox.Text = "";
            PasswordTextBox.Text = "";
            if (RoleComboBox.Items.Count > 0)
                RoleComboBox.SelectedIndex = 0;
            IsBlockedCheckBox.IsChecked = false;
        }

        // Обработчик выбора строки в DataGrid
        private void UserDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    LoginTextBox.Text = selectedRow["login"].ToString();
                    PasswordTextBox.Text = selectedRow["password"].ToString();

                    // Устанавливаем выбранную роль в ComboBox
                    if (selectedRow["roleid"] != DBNull.Value)
                    {
                        int roleid = Convert.ToInt32(selectedRow["roleid"]);
                        foreach (DataRowView item in RoleComboBox.Items)
                        {
                            if (Convert.ToInt32(item["roleid"]) == roleid)
                            {
                                RoleComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Устанавливаем состояние чекбокса блокировки
                    IsBlockedCheckBox.IsChecked = Convert.ToBoolean(selectedRow["isblocked"]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
                }
            }
            else
            {
                ClearInputFields();
            }
        }

        // Обработчики кнопок
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, заполните логин и пароль.");
                return;
            }

            if (RoleComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите роль.");
                return;
            }

            int roleid = Convert.ToInt32(RoleComboBox.SelectedValue);
            bool isblocked = IsBlockedCheckBox.IsChecked ?? false;

            try
            {
                AddUser(login, password, roleid, isblocked);
                LoadData();
                ClearInputFields();
                MessageBox.Show("Пользователь успешно добавлен.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserDataGrid.SelectedItem is DataRowView selectedRow)
            {
                int userid = Convert.ToInt32(selectedRow["userid"]);
                string login = LoginTextBox.Text.Trim();
                string password = PasswordTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Пожалуйста, заполните логин и пароль.");
                    return;
                }

                if (RoleComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Пожалуйста, выберите роль.");
                    return;
                }

                int roleid = Convert.ToInt32(RoleComboBox.SelectedValue);
                bool isblocked = IsBlockedCheckBox.IsChecked ?? false;

                try
                {
                    UpdateUser(userid, login, password, roleid, isblocked);
                    LoadData();
                    MessageBox.Show("Данные пользователя успешно обновлены.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении пользователя: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите пользователя для редактирования.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserDataGrid.SelectedItem is DataRowView selectedRow)
            {
                int userid = Convert.ToInt32(selectedRow["userid"]);
                DeleteUser(userid);
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите пользователя для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadRoles();
            ClearInputFields();
        }
    }
}
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
    /// <summary>
    /// Логика взаимодействия для RolesWindow.xaml
    /// </summary>
    public partial class RolesWindow : Window
    {
        private DatabaseConnection dbconnection;

        public RolesWindow()
        {
            InitializeComponent();
            RolesDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            dbconnection = new DatabaseConnection();
            LoadData();
        }

        public void LoadData()
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "SELECT roleid, rolename FROM roles ORDER BY roleid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // НЕ переименовываем столбцы, чтобы Binding работал правильно!
                        // Оставляем оригинальные имена: roleid и rolename
                        RolesDataGrid.ItemsSource = dataTable.DefaultView;

                        // Обновляем счетчик ролей
                        UpdateRolesCount(dataTable.Rows.Count);
                    }
                }
            }
        }

        // Метод для обновления счетчика ролей
        private void UpdateRolesCount(int count)
        {
            RolesCountText.Text = $"Всего ролей: {count}";
        }

        private void AddRole(string roleName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into roles (roleName) Values (@roleName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roleName", roleName);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void UpdateRole(int roleid, string roleName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update roles set rolename=@roleName WHERE roleid=@roleid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roleName", roleName);
                    command.Parameters.AddWithValue("@roleid", roleid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteRole(int roleid)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Delete from roles WHERE roleid=@roleid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roleid", roleid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string roleName = RolesNameTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                AddRole(roleName);
                LoadData();
                RolesNameTextBox.Text = string.Empty; // Очищаем поле после добавления
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите название роли.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = RolesDataGrid.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int roleid = Convert.ToInt32(selectedRow["roleid"]);
                string roleName = RolesNameTextBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(roleName))
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите обновить роль?",
                        "Подтверждение обновления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateRole(roleid, roleName);
                        LoadData();
                        RolesNameTextBox.Text = string.Empty; // Очищаем поле после обновления
                    }
                }
                else
                {
                    MessageBox.Show("Пожалуйста, введите корректные данные для обновления");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для обновления");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = RolesDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int roleid = Convert.ToInt32(selectedRow["roleid"]);
                string roleName = selectedRow["rolename"].ToString();

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы уверены, что хотите удалить роль '{roleName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteRole(roleid);
                    LoadData();
                    RolesNameTextBox.Text = string.Empty; // Очищаем поле после удаления
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }

        // Обработчик события выбора в DataGrid
        private void RolesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedRow = RolesDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                RolesNameTextBox.Text = selectedRow["rolename"].ToString();
            }
            else
            {
                RolesNameTextBox.Text = string.Empty;
            }
        }
    }
}
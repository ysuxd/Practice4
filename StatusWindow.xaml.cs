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
    public partial class StatusWindow : Window
    {
        private DatabaseConnection dbconnection;

        public StatusWindow()
        {
            InitializeComponent();
            StatusDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            dbconnection = new DatabaseConnection();
            LoadData();
        }

        public void LoadData()
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "SELECT statusid, statusname FROM status ORDER BY statusid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // НЕ переименовываем столбцы, чтобы Binding работал правильно!
                        // Оставляем оригинальные имена: statusid и statusname
                        StatusDataGrid.ItemsSource = dataTable.DefaultView;

                        // Обновляем счетчик статусов
                        UpdateStatusesCount(dataTable.Rows.Count);
                    }
                }
            }
        }

        // Метод для обновления счетчика статусов
        private void UpdateStatusesCount(int count)
        {
            StatusesCountText.Text = $"Всего статусов: {count}";
        }

        private void AddStatus(string statusName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into Status (statusName) Values (@statusName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@statusName", statusName);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void UpdateStatus(int statusid, string statusName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update Status set statusname=@statusName WHERE statusid=@statusid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@statusName", statusName);
                    command.Parameters.AddWithValue("@statusid", statusid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteStatus(int statusid)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Delete from Status WHERE statusid=@statusid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@statusid", statusid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string statusName = StatusNameTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(statusName))
            {
                AddStatus(statusName);
                LoadData();
                StatusNameTextBox.Text = string.Empty; // Очищаем поле после добавления
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите название статуса.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = StatusDataGrid.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int statusid = Convert.ToInt32(selectedRow["statusid"]);
                string statusName = StatusNameTextBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(statusName))
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите обновить статус?",
                        "Подтверждение обновления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateStatus(statusid, statusName);
                        LoadData();
                        StatusNameTextBox.Text = string.Empty; // Очищаем поле после обновления
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
            DataRowView selectedRow = StatusDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int statusid = Convert.ToInt32(selectedRow["statusid"]);
                string statusName = selectedRow["statusname"].ToString();

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы уверены, что хотите удалить статус '{statusName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteStatus(statusid);
                    LoadData();
                    StatusNameTextBox.Text = string.Empty; // Очищаем поле после удаления
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }

        // Обработчик события выбора в DataGrid
        private void StatusDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedRow = StatusDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                StatusNameTextBox.Text = selectedRow["statusname"].ToString();
            }
            else
            {
                StatusNameTextBox.Text = string.Empty;
            }
        }
    }
}
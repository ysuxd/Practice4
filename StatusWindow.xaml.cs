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

                        // Переименуем заголовки столбцов для красоты
                        if (dataTable.Columns.Contains("statusid"))
                            dataTable.Columns["statusid"].ColumnName = "Номер";
                        if (dataTable.Columns.Contains("Statusname"))
                            dataTable.Columns["statusname"].ColumnName = "Название";

                        StatusDataGrid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
        }

        private void AddClient(string statusName)
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


        private void UpdateClient(int statusid, string statusName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update Status set statusname=@statusName WHERE statusid=@statusid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@statusName", statusName);
                    command.Parameters.AddWithValue("@statusid", statusid); // Добавлен этот параметр
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteClient(int statusid)
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
            string statusName = StatusNameTextBox.Text;

            if (!string.IsNullOrWhiteSpace(statusName))
            {
                AddClient(statusName);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные данные.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = (DataRowView)StatusDataGrid.SelectedItem;
            if (selectedRow != null)
            {
                // Используйте новое имя столбца
                int statusid = Convert.ToInt32(selectedRow["Номер"]);
                string statusName = StatusNameTextBox.Text;
                // ... остальной код
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = (DataRowView)StatusDataGrid.SelectedItem;

            if (selectedRow != null)
            {
                // Используйте новое имя столбца
                int statusid = Convert.ToInt32(selectedRow["Номер"]);
                DeleteClient(statusid);
                LoadData();
            }
        }
    }
}

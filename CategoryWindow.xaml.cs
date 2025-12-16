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
    /// Логика взаимодействия для CategoryWindow.xaml
    /// </summary>
    public partial class CategoryWindow : Window
    {
        private DatabaseConnection dbconnection;



        public CategoryWindow()
        {
            InitializeComponent();
            CategoryDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            dbconnection = new DatabaseConnection();
            LoadData();
        }
        public void LoadData()
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "SELECT categoryid, categoryname FROM category ORDER BY categoryid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // Переименуем заголовки столбцов для красоты
                        if (dataTable.Columns.Contains("categoryid"))
                            dataTable.Columns["categoryid"].ColumnName = "Номер";
                        if (dataTable.Columns.Contains("Statusname"))
                            dataTable.Columns["categoryname"].ColumnName = "Название";

                        CategoryDataGrid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
        }

        private void AddClient(string categoryName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into category (categoryName) Values (@categoryName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@categoryName", categoryName);
                    command.ExecuteNonQuery();
                }
            }
        }


        private void UpdateClient(int categoryid, string categoryName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update category set categoryname=@categoryName WHERE categoryd=@categoryid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@categoryName", categoryName);
                    command.Parameters.AddWithValue("@categoryid", categoryid); // Добавлен этот параметр
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteClient(int categoryid)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Delete from category WHERE categoryid=@categoryid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@categoryid", categoryid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = CategoryNameTextBox.Text;

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                AddClient(categoryName);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные данные.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = (DataRowView)CategoryDataGrid.SelectedItem;
            if (selectedRow != null)
            {
                int categoryid = Convert.ToInt32(selectedRow["categoryid"]);
                string categoryName = CategoryNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    UpdateClient(categoryid, categoryName);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Пожалуйста,введите корректные данные для обновления");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста,выберите строку для обновления");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = (DataRowView)CategoryDataGrid.SelectedItem;

            if (selectedRow != null)
            {
                int categoryid = Convert.ToInt32(selectedRow["categoryid"]);
                DeleteClient(categoryid);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }
    }
}

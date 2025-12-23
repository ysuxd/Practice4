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

                        // НЕ переименовываем столбцы, чтобы Binding работал правильно!
                        // Оставляем оригинальные имена: categoryid и categoryname
                        CategoryDataGrid.ItemsSource = dataTable.DefaultView;

                        // Обновляем счетчик категорий
                        UpdateCategoriesCount(dataTable.Rows.Count);
                    }
                }
            }
        }

        // Метод для обновления счетчика категорий
        private void UpdateCategoriesCount(int count)
        {
            CategoriesCountText.Text = $"Всего категорий: {count}";
        }

        private void AddCategory(string categoryName)
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

        private void UpdateCategory(int categoryid, string categoryName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update category set categoryname=@categoryName WHERE categoryid=@categoryid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@categoryName", categoryName);
                    command.Parameters.AddWithValue("@categoryid", categoryid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteCategory(int categoryid)
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
            string categoryName = CategoryNameTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                AddCategory(categoryName);
                LoadData();
                CategoryNameTextBox.Text = string.Empty; // Очищаем поле после добавления
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите название категории.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = CategoryDataGrid.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int categoryid = Convert.ToInt32(selectedRow["categoryid"]);
                string categoryName = CategoryNameTextBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите обновить категорию?",
                        "Подтверждение обновления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateCategory(categoryid, categoryName);
                        LoadData();
                        CategoryNameTextBox.Text = string.Empty; // Очищаем поле после обновления
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
            DataRowView selectedRow = CategoryDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int categoryid = Convert.ToInt32(selectedRow["categoryid"]);
                string categoryName = selectedRow["categoryname"].ToString();

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы уверены, что хотите удалить категорию '{categoryName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteCategory(categoryid);
                    LoadData();
                    CategoryNameTextBox.Text = string.Empty; // Очищаем поле после удаления
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }

        private void CategoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedRow = CategoryDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                CategoryNameTextBox.Text = selectedRow["categoryname"].ToString();
            }
            else
            {
                CategoryNameTextBox.Text = string.Empty;
            }
        }
    }
}
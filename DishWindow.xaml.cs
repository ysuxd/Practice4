using Npgsql;
using System;
using System.Data;
using System.Windows;

namespace Practice
{
    public partial class DishWindow : Window
    {
        private DatabaseConnection dbconnection;
        private DataTable categoriesTable;

        public DishWindow()
        {
            InitializeComponent();
            dbconnection = new DatabaseConnection();
            LoadCategories(); // Загружаем категории в ComboBox
            LoadData();
        }

        // Загрузка категорий из таблицы category в ComboBox
        private void LoadCategories()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT categoryid, categoryname FROM category ORDER BY categoryname";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            categoriesTable = new DataTable();
                            adapter.Fill(categoriesTable);
                            CategoryComboBox.ItemsSource = categoriesTable.DefaultView;

                            if (categoriesTable.Rows.Count > 0)
                            {
                                CategoryComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных блюд с JOIN к таблице category
        public void LoadData()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            d.dishid, 
                            d.dishname, 
                            d.price, 
                            d.composition, 
                            d.quantity,
                            d.categoryid,
                            c.categoryname
                        FROM dish d
                        LEFT JOIN category c ON d.categoryid = c.categoryid
                        ORDER BY d.dishid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            DishDataGrid.ItemsSource = dataTable.DefaultView;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}",
                                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление блюда
        private void AddDish(string dishname, int categoryid, decimal price, string composition, int quantity)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO dish (dishname, categoryid, price, composition, quantity) 
                        VALUES (@dishname, @categoryid, @price, @composition, @quantity)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dishname", dishname);
                        command.Parameters.AddWithValue("@categoryid", categoryid);
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@composition",
                            string.IsNullOrEmpty(composition) ? (object)DBNull.Value : composition);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Обновление блюда
        private void UpdateDish(int dishid, string dishname, int categoryid, decimal price, string composition, int quantity)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        UPDATE dish 
                        SET dishname = @dishname, 
                            categoryid = @categoryid, 
                            price = @price, 
                            composition = @composition, 
                            quantity = @quantity
                        WHERE dishid = @dishid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dishname", dishname);
                        command.Parameters.AddWithValue("@categoryid", categoryid);
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@composition",
                            string.IsNullOrEmpty(composition) ? (object)DBNull.Value : composition);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.Parameters.AddWithValue("@dishid", dishid);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении блюда: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Удаление блюда
        private void DeleteDish(int dishid)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM dish WHERE dishid = @dishid";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dishid", dishid);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении блюда: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Очистка полей ввода
        private void ClearInputFields()
        {
            DishNameTextBox.Text = "";
            if (CategoryComboBox.Items.Count > 0)
                CategoryComboBox.SelectedIndex = 0;
            PriceTextBox.Text = "";
            CompositionTextBox.Text = "";
            QuantityTextBox.Text = "0";
        }

        // Обработчик выбора строки в DataGrid
        private void DishDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DishDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    DishNameTextBox.Text = selectedRow["dishname"].ToString();
                    PriceTextBox.Text = selectedRow["price"].ToString();
                    CompositionTextBox.Text = selectedRow["composition"]?.ToString() ?? "";
                    QuantityTextBox.Text = selectedRow["quantity"].ToString();

                    // Устанавливаем выбранную категорию в ComboBox
                    if (selectedRow["categoryid"] != DBNull.Value)
                    {
                        int categoryid = Convert.ToInt32(selectedRow["categoryid"]);
                        foreach (DataRowView item in CategoryComboBox.Items)
                        {
                            if (Convert.ToInt32(item["categoryid"]) == categoryid)
                            {
                                CategoryComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Обработчики кнопок
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string dishname = DishNameTextBox.Text.Trim();
            string priceText = PriceTextBox.Text.Trim();
            string quantityText = QuantityTextBox.Text.Trim();
            string composition = CompositionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(dishname))
            {
                MessageBox.Show("Пожалуйста, введите название блюда.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите категорию.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректную цену.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int categoryid = Convert.ToInt32(CategoryComboBox.SelectedValue);

            try
            {
                AddDish(dishname, categoryid, price, composition, quantity);
                LoadData();
                ClearInputFields();
                MessageBox.Show("Блюдо успешно добавлено.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении блюда: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (DishDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    int dishid = Convert.ToInt32(selectedRow["dishid"]);
                    string dishname = DishNameTextBox.Text.Trim();
                    string priceText = PriceTextBox.Text.Trim();
                    string quantityText = QuantityTextBox.Text.Trim();
                    string composition = CompositionTextBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(dishname))
                    {
                        MessageBox.Show("Пожалуйста, введите название блюда.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (CategoryComboBox.SelectedValue == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите категорию.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!decimal.TryParse(priceText, out decimal price) || price < 0)
                    {
                        MessageBox.Show("Пожалуйста, введите корректную цену.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
                    {
                        MessageBox.Show("Пожалуйста, введите корректное количество.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int categoryid = Convert.ToInt32(CategoryComboBox.SelectedValue);

                    try
                    {
                        UpdateDish(dishid, dishname, categoryid, price, composition, quantity);
                        LoadData();
                        MessageBox.Show("Блюдо успешно обновлено.",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении блюда: {ex.Message}",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите блюдо для редактирования.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DishDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    var result = MessageBox.Show("Вы уверены, что хотите удалить это блюдо?",
                                                "Подтверждение удаления",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        int dishid = Convert.ToInt32(selectedRow["dishid"]);
                        DeleteDish(dishid);
                        LoadData();
                        ClearInputFields();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите блюдо для удаления.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadCategories();
            ClearInputFields();
        }
    }
}
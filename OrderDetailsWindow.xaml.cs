using Npgsql;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Practice
{
    public partial class OrderDetailsWindow : Window
    {
        private DatabaseConnection dbconnection;
        private DataTable dishesTable;
        private DataTable ordersTable;
        private DataTable categoriesTable;
        private DataTable clientsTable;
        private DataTable employeesTable;
        private DataTable statusesTable;
        private int selectedOrderId = -1; // -1 означает "все заказы"
        private decimal totalAmount = 0;
        private int totalItems = 0;

        // Конструктор для просмотра всех деталей заказов
        public OrderDetailsWindow()
        {
            InitializeComponent();
            dbconnection = new DatabaseConnection();
            InitializeWindow();
        }

        // Конструктор для просмотра деталей конкретного заказа
        public OrderDetailsWindow(int orderId)
        {
            InitializeComponent();
            dbconnection = new DatabaseConnection();
            selectedOrderId = orderId;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            LoadDishes();
            LoadOrders();
            LoadCategories();
            LoadClients();
            LoadEmployees();
            LoadStatuses();
            LoadData();

            // Если открыто для конкретного заказа
            if (selectedOrderId != -1)
            {
                ConfigureForSingleOrder();
            }
            else
            {
                ConfigureForAllOrders();
            }

            // Обработчик изменения выбора блюда
            DishComboBox.SelectionChanged += DishComboBox_SelectionChanged;
        }

        private void ConfigureForSingleOrder()
        {
            // Настраиваем окно для просмотра одного заказа
            WindowTitleTextBlock.Text = $"Детали заказа #{selectedOrderId}";
            WindowSubtitleTextBlock.Text = "Позиции заказа";

            // Показываем информацию о заказе
            OrderInfoBorder.Visibility = Visibility.Visible;
            OrderIdTextBlock.Text = $"Заказ #{selectedOrderId}";

            // Обновляем информацию о статусе заказа
            UpdateOrderStatusInfo();

            // Показываем итоговую сумму
            UpdateTotalAmount();
            TotalAmountBorder.Visibility = Visibility.Visible;

            // Скрываем или отключаем выбор заказа
            OrderPanel.Visibility = Visibility.Collapsed;

            // Показываем кнопку "Назад к заказам"
            BackToOrdersButton.Visibility = Visibility.Visible;

            // Обновляем текст информационного блока
            InfoTextBlock.Text = $"Позиции заказа #{selectedOrderId}. Для редактирования выберите запись в таблице";
        }

        private void ConfigureForAllOrders()
        {
            // Настраиваем окно для просмотра всех заказов
            WindowTitleTextBlock.Text = "Детали всех заказов";
            WindowSubtitleTextBlock.Text = "Управление позициями заказов";

            // Скрываем ненужные элементы
            OrderInfoBorder.Visibility = Visibility.Collapsed;
            BackToOrdersButton.Visibility = Visibility.Collapsed;
            TotalAmountBorder.Visibility = Visibility.Collapsed;

            // Показываем панель выбора заказа
            OrderPanel.Visibility = Visibility.Visible;

            // Обновляем текст информационного блока
            InfoTextBlock.Text = "Для редактирования выберите запись в таблице";

            // Устанавливаем первый заказ, если есть
            if (OrderComboBox.Items.Count > 0)
            {
                OrderComboBox.SelectedIndex = 0;
                OrderComboBox.IsEnabled = true;
            }
        }

        private void UpdateOrderStatusInfo()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT s.statusname, o.orderdate, c.lastname || ' ' || c.firstname as clientname
                        FROM orders o
                        LEFT JOIN status s ON o.statusid = s.statusid
                        LEFT JOIN client c ON o.clientid = c.clientid
                        WHERE o.orderid = @orderid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderid", selectedOrderId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string statusName = reader["statusname"]?.ToString() ?? "Неизвестно";
                                DateTime orderDate = reader["orderdate"] != DBNull.Value ?
                                    Convert.ToDateTime(reader["orderdate"]) : DateTime.MinValue;
                                string clientName = reader["clientname"]?.ToString() ?? "Неизвестный клиент";

                                OrderStatusTextBlock.Text = $"Статус: {statusName}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке статуса заказа: {ex.Message}");
            }
        }

        private void UpdateTotalAmount()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            COUNT(*) as items_count,
                            SUM(price * quantity) as total_amount
                        FROM ordersdetails 
                        WHERE orderid = @orderid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderid", selectedOrderId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                totalItems = reader["items_count"] != DBNull.Value ?
                                    Convert.ToInt32(reader["items_count"]) : 0;
                                totalAmount = reader["total_amount"] != DBNull.Value ?
                                    Convert.ToDecimal(reader["total_amount"]) : 0;

                                TotalAmountTextBlock.Text = $"Итого: {totalAmount:0.00} ₽";
                                ItemsCountSummaryTextBlock.Text = $"Позиций: {totalItems}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при расчете суммы: {ex.Message}");
            }
        }

        // Загрузка блюд из таблицы dish
        private void LoadDishes()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT dishid, dishname, price FROM dish ORDER BY dishname";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            dishesTable = new DataTable();
                            adapter.Fill(dishesTable);
                            DishComboBox.ItemsSource = dishesTable.DefaultView;

                            if (dishesTable.Rows.Count > 0)
                            {
                                DishComboBox.SelectedIndex = 0;
                                // Автоматически устанавливаем цену из выбранного блюда
                                UpdatePriceFromSelectedDish();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка заказов из таблицы orders с информацией о клиентах
        private void LoadOrders()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            o.orderid, 
                            CONCAT('Заказ #', o.orderid, ' от ', o.orderdate, ' (', c.lastname, ')') as orderinfo,
                            o.clientid
                        FROM orders o
                        LEFT JOIN client c ON o.clientid = c.clientid
                        ORDER BY o.orderdate DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            ordersTable = new DataTable();
                            adapter.Fill(ordersTable);
                            OrderComboBox.ItemsSource = ordersTable.DefaultView;

                            if (ordersTable.Rows.Count > 0)
                            {
                                OrderComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка категорий из таблицы category
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

        // Загрузка клиентов из таблицы client
        private void LoadClients()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            clientid, 
                            CONCAT(lastname, ' ', firstname, ' ', COALESCE(surname, '')) as clientname 
                        FROM client 
                        ORDER BY lastname, firstname";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            clientsTable = new DataTable();
                            adapter.Fill(clientsTable);
                            ClientComboBox.ItemsSource = clientsTable.DefaultView;

                            if (clientsTable.Rows.Count > 0)
                            {
                                ClientComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке клиентов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка сотрудников из таблицы employee
        private void LoadEmployees()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            employeeid, 
                            CONCAT(lastname, ' ', firstname, ' ', COALESCE(surname, '')) as employeename 
                        FROM employee 
                        ORDER BY lastname, firstname";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            employeesTable = new DataTable();
                            adapter.Fill(employeesTable);
                            EmployeeComboBox.ItemsSource = employeesTable.DefaultView;

                            if (employeesTable.Rows.Count > 0)
                            {
                                EmployeeComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сотрудников: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка статусов из таблицы status
        private void LoadStatuses()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT statusid, statusname FROM status ORDER BY statusname";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            statusesTable = new DataTable();
                            adapter.Fill(statusesTable);
                            StatusComboBox.ItemsSource = statusesTable.DefaultView;

                            if (statusesTable.Rows.Count > 0)
                            {
                                StatusComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статусов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Автоматическое обновление цены при выборе блюда
        private void UpdatePriceFromSelectedDish()
        {
            if (DishComboBox.SelectedItem is DataRowView selectedDish)
            {
                try
                {
                    decimal price = Convert.ToDecimal(selectedDish["price"]);
                    PriceTextBox.Text = price.ToString("0.00");
                }
                catch { }
            }
        }

        // Загрузка данных деталей заказа с JOIN ко всем связанным таблицам
        public void LoadData()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            od.orderdetailsid,
                            od.dishid,
                            d.dishname,
                            od.orderid,
                            od.categoryid,
                            c.categoryname,
                            od.clientid,
                            CONCAT(cl.lastname, ' ', cl.firstname, ' ', COALESCE(cl.surname, '')) as clientname,
                            od.employeeid,
                            CONCAT(e.lastname, ' ', e.firstname, ' ', COALESCE(e.surname, '')) as employeename,
                            od.statusid,
                            s.statusname,
                            od.price,
                            od.quantity
                        FROM ordersdetails od
                        LEFT JOIN dish d ON od.dishid = d.dishid
                        LEFT JOIN orders ord ON od.orderid = ord.orderid
                        LEFT JOIN category c ON od.categoryid = c.categoryid
                        LEFT JOIN client cl ON od.clientid = cl.clientid
                        LEFT JOIN employee e ON od.employeeid = e.employeeid
                        LEFT JOIN status s ON od.statusid = s.statusid";

                    // Добавляем фильтр, если выбран конкретный заказ
                    if (selectedOrderId != -1)
                    {
                        query += " WHERE od.orderid = @orderid";
                    }

                    query += " ORDER BY od.orderdetailsid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Добавляем параметр, если фильтруем по заказу
                        if (selectedOrderId != -1)
                        {
                            command.Parameters.AddWithValue("@orderid", selectedOrderId);
                        }

                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            OrderDetailsDataGrid.ItemsSource = dataTable.DefaultView;

                            // Обновляем счетчик
                            ItemsCountTextBlock.Text = $"Всего позиций: {dataTable.Rows.Count}";

                            // Обновляем итоговую сумму для конкретного заказа
                            if (selectedOrderId != -1)
                            {
                                UpdateTotalAmount();
                            }
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

        // Добавление детали заказа
        private void AddOrderDetail(int dishid, int orderid, int categoryid, int clientid, int employeeid, int statusid, decimal price, int quantity)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO ordersdetails (dishid, orderid, categoryid, clientid, employeeid, statusid, price, quantity) 
                        VALUES (@dishid, @orderid, @categoryid, @clientid, @employeeid, @statusid, @price, @quantity)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dishid", dishid);
                        command.Parameters.AddWithValue("@orderid", orderid);
                        command.Parameters.AddWithValue("@categoryid", categoryid);
                        command.Parameters.AddWithValue("@clientid", clientid);
                        command.Parameters.AddWithValue("@employeeid", employeeid);
                        command.Parameters.AddWithValue("@statusid", statusid);
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении детали заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Обновление детали заказа
        private void UpdateOrderDetail(int orderdetailsid, int dishid, int orderid, int categoryid, int clientid, int employeeid, int statusid, decimal price, int quantity)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        UPDATE ordersdetails 
                        SET dishid = @dishid, 
                            orderid = @orderid, 
                            categoryid = @categoryid, 
                            clientid = @clientid, 
                            employeeid = @employeeid, 
                            statusid = @statusid, 
                            price = @price, 
                            quantity = @quantity
                        WHERE orderdetailsid = @orderdetailsid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dishid", dishid);
                        command.Parameters.AddWithValue("@orderid", orderid);
                        command.Parameters.AddWithValue("@categoryid", categoryid);
                        command.Parameters.AddWithValue("@clientid", clientid);
                        command.Parameters.AddWithValue("@employeeid", employeeid);
                        command.Parameters.AddWithValue("@statusid", statusid);
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@quantity", quantity);
                        command.Parameters.AddWithValue("@orderdetailsid", orderdetailsid);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении детали заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Удаление детали заказа
        private void DeleteOrderDetail(int orderdetailsid)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM ordersdetails WHERE orderdetailsid = @orderdetailsid";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderdetailsid", orderdetailsid);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении детали заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Очистка полей ввода
        private void ClearInputFields()
        {
            if (DishComboBox.Items.Count > 0)
                DishComboBox.SelectedIndex = 0;
            if (OrderComboBox.Items.Count > 0 && OrderComboBox.IsEnabled)
                OrderComboBox.SelectedIndex = 0;
            if (CategoryComboBox.Items.Count > 0)
                CategoryComboBox.SelectedIndex = 0;
            if (ClientComboBox.Items.Count > 0)
                ClientComboBox.SelectedIndex = 0;
            if (EmployeeComboBox.Items.Count > 0)
                EmployeeComboBox.SelectedIndex = 0;
            if (StatusComboBox.Items.Count > 0)
                StatusComboBox.SelectedIndex = 0;
            PriceTextBox.Text = "";
            QuantityTextBox.Text = "1";
            UpdatePriceFromSelectedDish();
        }

        // Обработчик выбора строки в DataGrid
        private void OrderDetailsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderDetailsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    // Устанавливаем выбранное блюдо
                    if (selectedRow["dishid"] != DBNull.Value)
                    {
                        int dishid = Convert.ToInt32(selectedRow["dishid"]);
                        foreach (DataRowView item in DishComboBox.Items)
                        {
                            if (Convert.ToInt32(item["dishid"]) == dishid)
                            {
                                DishComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Устанавливаем выбранный заказ (только если не в режиме просмотра одного заказа)
                    if (selectedRow["orderid"] != DBNull.Value && OrderComboBox.IsEnabled)
                    {
                        int orderid = Convert.ToInt32(selectedRow["orderid"]);
                        foreach (DataRowView item in OrderComboBox.Items)
                        {
                            if (Convert.ToInt32(item["orderid"]) == orderid)
                            {
                                OrderComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Устанавливаем выбранную категорию
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

                    // Устанавливаем выбранного клиента
                    if (selectedRow["clientid"] != DBNull.Value)
                    {
                        int clientid = Convert.ToInt32(selectedRow["clientid"]);
                        foreach (DataRowView item in ClientComboBox.Items)
                        {
                            if (Convert.ToInt32(item["clientid"]) == clientid)
                            {
                                ClientComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Устанавливаем выбранного сотрудника
                    if (selectedRow["employeeid"] != DBNull.Value)
                    {
                        int employeeid = Convert.ToInt32(selectedRow["employeeid"]);
                        foreach (DataRowView item in EmployeeComboBox.Items)
                        {
                            if (Convert.ToInt32(item["employeeid"]) == employeeid)
                            {
                                EmployeeComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Устанавливаем выбранный статус
                    if (selectedRow["statusid"] != DBNull.Value)
                    {
                        int statusid = Convert.ToInt32(selectedRow["statusid"]);
                        foreach (DataRowView item in StatusComboBox.Items)
                        {
                            if (Convert.ToInt32(item["statusid"]) == statusid)
                            {
                                StatusComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Устанавливаем цену и количество
                    PriceTextBox.Text = selectedRow["price"].ToString();
                    QuantityTextBox.Text = selectedRow["quantity"].ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Обработчик изменения выбора блюда (для автоматического обновления цены)
        private void DishComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePriceFromSelectedDish();
        }

        // Обработчики кнопок
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (DishComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите блюдо.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Если в режиме просмотра одного заказа, используем selectedOrderId
            int orderid = selectedOrderId != -1 ? selectedOrderId :
                (OrderComboBox.SelectedValue != null ? Convert.ToInt32(OrderComboBox.SelectedValue) : -1);

            if (orderid == -1)
            {
                MessageBox.Show("Пожалуйста, выберите заказ.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите категорию.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ClientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите клиента.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EmployeeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите сотрудника.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StatusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите статус.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректную цену.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество (больше 0).",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int dishid = Convert.ToInt32(DishComboBox.SelectedValue);
            int categoryid = Convert.ToInt32(CategoryComboBox.SelectedValue);
            int clientid = Convert.ToInt32(ClientComboBox.SelectedValue);
            int employeeid = Convert.ToInt32(EmployeeComboBox.SelectedValue);
            int statusid = Convert.ToInt32(StatusComboBox.SelectedValue);

            try
            {
                AddOrderDetail(dishid, orderid, categoryid, clientid, employeeid, statusid, price, quantity);
                LoadData();
                ClearInputFields();
                MessageBox.Show("Деталь заказа успешно добавлена.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении детали заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderDetailsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    int orderdetailsid = Convert.ToInt32(selectedRow["orderdetailsid"]);

                    if (DishComboBox.SelectedValue == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите блюдо.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Если в режиме просмотра одного заказа, используем selectedOrderId
                    int orderid = selectedOrderId != -1 ? selectedOrderId :
                        (OrderComboBox.SelectedValue != null ? Convert.ToInt32(OrderComboBox.SelectedValue) : -1);

                    if (orderid == -1)
                    {
                        MessageBox.Show("Пожалуйста, выберите заказ.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (CategoryComboBox.SelectedValue == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите категорию.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (ClientComboBox.SelectedValue == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите клиента.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (EmployeeComboBox.SelectedValue == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите сотрудника.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (StatusComboBox.SelectedValue == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите статус.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
                    {
                        MessageBox.Show("Пожалуйста, введите корректную цену.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
                    {
                        MessageBox.Show("Пожалуйста, введите корректное количество (больше 0).",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int dishid = Convert.ToInt32(DishComboBox.SelectedValue);
                    int categoryid = Convert.ToInt32(CategoryComboBox.SelectedValue);
                    int clientid = Convert.ToInt32(ClientComboBox.SelectedValue);
                    int employeeid = Convert.ToInt32(EmployeeComboBox.SelectedValue);
                    int statusid = Convert.ToInt32(StatusComboBox.SelectedValue);

                    try
                    {
                        UpdateOrderDetail(orderdetailsid, dishid, orderid, categoryid, clientid, employeeid, statusid, price, quantity);
                        LoadData();
                        MessageBox.Show("Деталь заказа успешно обновлена.",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении детали заказа: {ex.Message}",
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
                MessageBox.Show("Пожалуйста, выберите деталь заказа для редактирования.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderDetailsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    var result = MessageBox.Show("Вы уверены, что хотите удалить эту деталь заказа?",
                                                "Подтверждение удаления",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        int orderdetailsid = Convert.ToInt32(selectedRow["orderdetailsid"]);
                        DeleteOrderDetail(orderdetailsid);
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
                MessageBox.Show("Пожалуйста, выберите деталь заказа для удаления.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadDishes();
            if (selectedOrderId == -1) // Только если не в режиме просмотра одного заказа
            {
                LoadOrders();
            }
            LoadCategories();
            LoadClients();
            LoadEmployees();
            LoadStatuses();
            ClearInputFields();
        }

        // Обработчик кнопки "Назад к заказам"
        private void BackToOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
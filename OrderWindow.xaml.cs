using Npgsql;
using System;
using System.Data;
using System.Windows;

namespace Practice
{
    public partial class OrderWindow : Window
    {
        private DatabaseConnection dbconnection;
        private DataTable clientsTable;
        private DataTable employeesTable;
        private DataTable statusesTable;

        public OrderWindow()
        {
            InitializeComponent();
            dbconnection = new DatabaseConnection();
            LoadClients();      // Загружаем клиентов в ComboBox
            LoadEmployees();    // Загружаем сотрудников в ComboBox
            LoadStatuses();     // Загружаем статусы в ComboBox
            LoadData();

            // Устанавливаем текущую дату по умолчанию
            OrderDatePicker.SelectedDate = DateTime.Today;
        }

        // Загрузка клиентов из таблицы client
        private void LoadClients()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    // Объединяем имя, фамилию и отчество в одно поле
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
                    // Объединяем имя, фамилию и отчество в одно поле
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

        // Загрузка данных заказов с JOIN к таблицам client, employee и status
        public void LoadData()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    o.orderid, 
                    o.clientid,
                    CONCAT(c.lastname, ' ', c.firstname, ' ', COALESCE(c.surname, '')) as clientname,
                    o.employeeid,
                    CONCAT(e.lastname, ' ', e.firstname, ' ', COALESCE(e.surname, '')) as employeename,
                    o.statusid,
                    s.statusname,
                    o.orderdate,
                    o.ordertime
                FROM orders o
                LEFT JOIN client c ON o.clientid = c.clientid
                LEFT JOIN employee e ON o.employeeid = e.employeeid
                LEFT JOIN status s ON o.statusid = s.statusid
                ORDER BY o.orderdate DESC, o.ordertime DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            OrderDataGrid.ItemsSource = dataTable.DefaultView;

                            // Обновляем счетчик заказов
                            UpdateOrdersCount(dataTable.Rows.Count);
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

        // Метод для обновления счетчика заказов
        private void UpdateOrdersCount(int count)
        {
            OrdersCountText.Text = $"Всего заказов: {count}";
        }

        // Добавление заказа
        private void AddOrder(int clientid, int employeeid, int statusid, DateTime orderdate, TimeSpan ordertime)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO orders (clientid, employeeid, statusid, orderdate, ordertime) 
                        VALUES (@clientid, @employeeid, @statusid, @orderdate, @ordertime)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientid", clientid);
                        command.Parameters.AddWithValue("@employeeid", employeeid);
                        command.Parameters.AddWithValue("@statusid", statusid);
                        command.Parameters.AddWithValue("@orderdate", orderdate.Date);
                        command.Parameters.AddWithValue("@ordertime", ordertime);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Обновление заказа
        private void UpdateOrder(int orderid, int clientid, int employeeid, int statusid, DateTime orderdate, TimeSpan ordertime)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        UPDATE orders 
                        SET clientid = @clientid, 
                            employeeid = @employeeid, 
                            statusid = @statusid, 
                            orderdate = @orderdate, 
                            ordertime = @ordertime
                        WHERE orderid = @orderid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientid", clientid);
                        command.Parameters.AddWithValue("@employeeid", employeeid);
                        command.Parameters.AddWithValue("@statusid", statusid);
                        command.Parameters.AddWithValue("@orderdate", orderdate.Date);
                        command.Parameters.AddWithValue("@ordertime", ordertime);
                        command.Parameters.AddWithValue("@orderid", orderid);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Удаление заказа
        private void DeleteOrder(int orderid)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM orders WHERE orderid = @orderid";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderid", orderid);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Очистка полей ввода
        private void ClearInputFields()
        {
            if (ClientComboBox.Items.Count > 0)
                ClientComboBox.SelectedIndex = 0;
            if (EmployeeComboBox.Items.Count > 0)
                EmployeeComboBox.SelectedIndex = 0;
            if (StatusComboBox.Items.Count > 0)
                StatusComboBox.SelectedIndex = 0;
            OrderDatePicker.SelectedDate = DateTime.Today;
            TimeTextBox.Text = "12:00";
        }

        // Обработчик выбора строки в DataGrid
        private void OrderDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (OrderDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
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

                    // Устанавливаем дату и время
                    if (selectedRow["orderdate"] != DBNull.Value)
                    {
                        OrderDatePicker.SelectedDate = Convert.ToDateTime(selectedRow["orderdate"]);
                    }

                    if (selectedRow["ordertime"] != DBNull.Value)
                    {
                        TimeSpan time = (TimeSpan)selectedRow["ordertime"];
                        TimeTextBox.Text = time.ToString(@"hh\:mm");
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

            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату заказа.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan ordertime))
            {
                MessageBox.Show("Пожалуйста, введите корректное время в формате ЧЧ:ММ.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int clientid = Convert.ToInt32(ClientComboBox.SelectedValue);
            int employeeid = Convert.ToInt32(EmployeeComboBox.SelectedValue);
            int statusid = Convert.ToInt32(StatusComboBox.SelectedValue);
            DateTime orderdate = OrderDatePicker.SelectedDate.Value;

            try
            {
                AddOrder(clientid, employeeid, statusid, orderdate, ordertime);
                LoadData();
                ClearInputFields();
                MessageBox.Show("Заказ успешно добавлен.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    int orderid = Convert.ToInt32(selectedRow["orderid"]);

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

                    if (OrderDatePicker.SelectedDate == null)
                    {
                        MessageBox.Show("Пожалуйста, выберите дату заказа.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan ordertime))
                    {
                        MessageBox.Show("Пожалуйста, введите корректное время в формате ЧЧ:ММ.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int clientid = Convert.ToInt32(ClientComboBox.SelectedValue);
                    int employeeid = Convert.ToInt32(EmployeeComboBox.SelectedValue);
                    int statusid = Convert.ToInt32(StatusComboBox.SelectedValue);
                    DateTime orderdate = OrderDatePicker.SelectedDate.Value;

                    try
                    {
                        UpdateOrder(orderid, clientid, employeeid, statusid, orderdate, ordertime);
                        LoadData();
                        MessageBox.Show("Заказ успешно обновлен.",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении заказа: {ex.Message}",
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
                MessageBox.Show("Пожалуйста, выберите заказ для редактирования.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    var result = MessageBox.Show("Вы уверены, что хотите удалить этот заказ?",
                                                "Подтверждение удаления",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        int orderid = Convert.ToInt32(selectedRow["orderid"]);
                        DeleteOrder(orderid);
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
                MessageBox.Show("Пожалуйста, выберите заказ для удаления.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadClients();
            LoadEmployees();
            LoadStatuses();
            ClearInputFields();
        }
    }
}
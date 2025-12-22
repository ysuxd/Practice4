using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace Practice
{
    public partial class ClientIntefaceWindow : Window
    {
        public int CurrentUserId { get; set; }
        public string CurrentUsername { get; set; }
        public string CurrentRole { get; set; }

        private DatabaseConnection dbconnection;
        private ObservableCollection<Dish> dishes;
        private ObservableCollection<CartItem> cartItems;
        private int clientId;
        private string clientName;

        public ClientIntefaceWindow(int userId, string username, string role)
        {
            InitializeComponent();
            Console.WriteLine("Вызван конструктор С параметрами");

            // Устанавливаем значения
            CurrentUserId = userId;
            CurrentUsername = username;
            CurrentRole = role;

            Console.WriteLine($"Установлено:");
            Console.WriteLine($"  CurrentUserId = {CurrentUserId}");
            Console.WriteLine($"  CurrentUsername = {CurrentUsername}");
            Console.WriteLine($"  CurrentRole = {CurrentRole}");

            dbconnection = new DatabaseConnection();
            dishes = new ObservableCollection<Dish>();
            cartItems = new ObservableCollection<CartItem>();

            LoadUserInfo();
            LoadDishes();
            UpdateCartDisplay();
        }

        private void LoadUserInfo()
        {
            try
            {
                Console.WriteLine($"=== НАЧАЛО LoadUserInfo() ===");
                Console.WriteLine($"Текущий UserId: {CurrentUserId}");
                Console.WriteLine($"Текущий Username: {CurrentUsername}");
                Console.WriteLine($"Текущая Role: {CurrentRole}");

                // Устанавливаем временные значения
                WelcomeText.Text = $"Здравствуйте, {CurrentUsername}!";
                UserInfoText.Text = $"Касимовский нефтегазовый колледж • {CurrentRole}";

                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();

                    // ШАГ 1: Проверим, что пользователь действительно существует
                    Console.WriteLine($"ШАГ 1: Проверяем пользователя с userid={CurrentUserId}");
                    string checkUserQuery = "SELECT userid, login FROM users WHERE userid = @userid";

                    using (var checkCmd = new NpgsqlCommand(checkUserQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@userid", CurrentUserId);
                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int dbUserId = reader.GetInt32(0);
                                string dbLogin = reader.GetString(1);
                                Console.WriteLine($"✓ Пользователь найден в БД: ID={dbUserId}, Login={dbLogin}");

                                if (dbUserId != CurrentUserId)
                                {
                                    Console.WriteLine($"⚠ ВНИМАНИЕ: CurrentUserId ({CurrentUserId}) не совпадает с ID из БД ({dbUserId})!");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"✗ ОШИБКА: Пользователь с ID={CurrentUserId} не найден в таблице users!");
                                MessageBox.Show($"Пользователь с ID={CurrentUserId} не найден в системе. \nПопробуйте выйти и зайти снова.",
                                              "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                    }

                    // ШАГ 2: Ищем клиента
                    Console.WriteLine($"ШАГ 2: Ищем клиента с userid={CurrentUserId}");
                    string findClientQuery = @"
                SELECT 
                    clientid,
                    firstname,
                    lastname,
                    surname
                FROM client 
                WHERE userid = @userid";

                    using (var clientCmd = new NpgsqlCommand(findClientQuery, connection))
                    {
                        clientCmd.Parameters.AddWithValue("@userid", CurrentUserId);
                        using (var reader = clientCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                clientId = reader.GetInt32(0);
                                Console.WriteLine($"✓ Клиент найден: clientid={clientId}");

                                // Собираем ФИО
                                string firstName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                                string lastName = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
                                string surname = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();

                                Console.WriteLine($"ФИО из БД: LastName='{lastName}', FirstName='{firstName}', Surname='{surname}'");

                                // Формируем полное имя
                                List<string> nameParts = new List<string>();
                                if (!string.IsNullOrEmpty(lastName)) nameParts.Add(lastName);
                                if (!string.IsNullOrEmpty(firstName)) nameParts.Add(firstName);
                                if (!string.IsNullOrEmpty(surname)) nameParts.Add(surname);

                                if (nameParts.Count > 0)
                                {
                                    clientName = string.Join(" ", nameParts);
                                    WelcomeText.Text = $"Здравствуйте, {clientName}!";
                                    UserInfoText.Text = $"{clientName} • {CurrentRole}";
                                    Console.WriteLine($"✓ Имя клиента установлено: {clientName}");
                                }
                                else
                                {
                                    clientName = CurrentUsername;
                                    Console.WriteLine($"⚠ ФИО пустые, используем логин: {clientName}");
                                }
                            }
                            else
                            {
                                // Клиент не найден
                                Console.WriteLine($"✗ Клиент не найден для userid={CurrentUserId}");

                                // Проверяем, есть ли вообще запись в client с таким userid
                                string checkClientExists = "SELECT COUNT(*) FROM client WHERE userid = @userid";
                                using (var countCmd = new NpgsqlCommand(checkClientExists, connection))
                                {
                                    countCmd.Parameters.AddWithValue("@userid", CurrentUserId);
                                    int count = Convert.ToInt32(countCmd.ExecuteScalar());
                                    Console.WriteLine($"Всего записей в client с userid={CurrentUserId}: {count}");
                                }

                                // Если пользователь есть, но клиента нет - создаем запись
                                Console.WriteLine($"Создаем запись клиента для userid={CurrentUserId}");
                                string createClientQuery = @"
                            INSERT INTO client (firstname, lastname, surname, userid) 
                            VALUES (@firstname, @lastname, @surname, @userid)
                            RETURNING clientid";

                                using (var createCmd = new NpgsqlCommand(createClientQuery, connection))
                                {
                                    createCmd.Parameters.AddWithValue("@firstname", CurrentUsername);
                                    createCmd.Parameters.AddWithValue("@lastname", "");
                                    createCmd.Parameters.AddWithValue("@surname", "");
                                    createCmd.Parameters.AddWithValue("@userid", CurrentUserId);

                                    clientId = Convert.ToInt32(createCmd.ExecuteScalar());
                                    clientName = CurrentUsername;

                                    Console.WriteLine($"✓ Создана запись клиента: clientid={clientId}");
                                    MessageBox.Show($"Создана новая запись клиента с ID {clientId}",
                                                  "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"=== КОНЕЦ LoadUserInfo() ===");
                Console.WriteLine($"Итог: clientId={clientId}, clientName={clientName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== КРИТИЧЕСКАЯ ОШИБКА ===");
                Console.WriteLine($"Тип: {ex.GetType().Name}");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"Стек вызова: {ex.StackTrace}");

                if (ex is NpgsqlException npgEx)
                {
                    Console.WriteLine($"Код SQL ошибки: {npgEx.SqlState}");
                    Console.WriteLine($"Позиция ошибки: {npgEx.Data["Position"]}");
                }

                MessageBox.Show($"Критическая ошибка: {ex.Message}\n\n" +
                              $"Проверьте подключение к базе данных и правильность SQL запросов.",
                              "Системная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Устанавливаем безопасные значения
                clientId = CurrentUserId; // Используем userid как clientid
                clientName = CurrentUsername;
                WelcomeText.Text = $"Здравствуйте, {CurrentUsername}!";
                UserInfoText.Text = $"Касимовский нефтегазовый колледж • {CurrentRole}";
            }
        }



        private void LoadDishes()
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();

                    // Исправленный запрос - убрали d.description, так как в таблице dish нет такого поля
                    // Вместо этого используем composition
                    string query = @"
                        SELECT 
                            d.dishid, 
                            d.dishname, 
                            d.composition, 
                            d.price, 
                            d.quantity as available_quantity,
                            c.categoryname
                        FROM dish d
                        LEFT JOIN category c ON d.categoryid = c.categoryid
                        WHERE d.quantity > 0  -- Показываем только блюда, которые есть в наличии
                        ORDER BY c.categoryname, d.dishname";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            dishes.Clear();
                            while (reader.Read())
                            {
                                var dish = new Dish
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? "Без описания" : reader.GetString(2),
                                    Price = reader.GetDecimal(3),
                                    AvailableQuantity = reader.GetInt32(4),
                                    Category = reader.IsDBNull(5) ? "Без категории" : reader.GetString(5)
                                };
                                dishes.Add(dish);
                            }
                        }
                    }
                }

                DishesItemsControl.ItemsSource = dishes;

                // Загрузка категорий в фильтр
                var categories = dishes.Select(d => d.Category).Distinct().ToList();
                CategoryFilterComboBox.Items.Clear();
                CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все категории", IsSelected = true });
                foreach (var category in categories)
                {
                    CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = category });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке меню: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление блюда в корзину
        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int dishId = Convert.ToInt32(button.Tag);
                var dish = dishes.FirstOrDefault(d => d.Id == dishId);

                if (dish != null)
                {
                    // Проверяем, есть ли уже это блюдо в корзине
                    var existingItem = cartItems.FirstOrDefault(item => item.Id == dishId);

                    if (existingItem != null)
                    {
                        // Проверяем, не превышает ли новое количество доступное количество
                        if (existingItem.Quantity + 1 <= dish.AvailableQuantity)
                        {
                            existingItem.Quantity++;
                        }
                        else
                        {
                            MessageBox.Show($"Нельзя добавить больше {dish.AvailableQuantity} шт. этого блюда.\n" +
                                          $"В корзине уже: {existingItem.Quantity} шт.\n" +
                                          $"Доступно: {dish.AvailableQuantity} шт.",
                                          "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        // Проверяем, что есть хотя бы 1 штука доступна
                        if (dish.AvailableQuantity > 0)
                        {
                            cartItems.Add(new CartItem
                            {
                                Id = dishId,
                                Name = dish.Name,
                                Price = dish.Price,
                                AvailableQuantity = dish.AvailableQuantity,
                                Quantity = 1
                            });
                        }
                        else
                        {
                            MessageBox.Show("Это блюдо закончилось",
                                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    UpdateCartDisplay();
                }
            }
        }

        // Удаление блюда из корзины
        private void RemoveFromCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int dishId = Convert.ToInt32(button.Tag);
                var itemToRemove = cartItems.FirstOrDefault(item => item.Id == dishId);

                if (itemToRemove != null)
                {
                    cartItems.Remove(itemToRemove);
                    UpdateCartDisplay();
                }
            }
        }

        // Очистка корзины
        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count > 0)
            {
                var result = MessageBox.Show("Вы уверены, что хотите очистить корзину?",
                                            "Подтверждение",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    cartItems.Clear();
                    UpdateCartDisplay();
                }
            }
        }

        // Обновление отображения корзины
        private void UpdateCartDisplay()
        {
            CartItemsControl.ItemsSource = null;
            CartItemsControl.ItemsSource = cartItems;

            // Обновляем общую сумму
            decimal total = cartItems.Sum(item => item.Price * item.Quantity);
            TotalAmountText.Text = $"{total:C}";

            // Обновляем статус корзины
            if (cartItems.Count > 0)
            {
                CartStatusText.Text = $"{cartItems.Count} товар(ов) • {total:C}";
                EmptyCartText.Visibility = Visibility.Collapsed;
                PlaceOrderButton.IsEnabled = true;
            }
            else
            {
                CartStatusText.Text = "Корзина пуста";
                EmptyCartText.Visibility = Visibility.Visible;
                PlaceOrderButton.IsEnabled = false;
            }
        }

        // Оформление заказа
        // Оформление заказа
        // Оформление заказа
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Добавьте блюда для оформления заказа.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (clientId == 0)
            {
                MessageBox.Show("Не удалось определить информацию о клиенте. Обратитесь к администратору.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, что все блюда еще есть в нужном количестве
            foreach (var cartItem in cartItems)
            {
                var dish = dishes.FirstOrDefault(d => d.Id == cartItem.Id);
                if (dish == null || dish.AvailableQuantity < cartItem.Quantity)
                {
                    MessageBox.Show($"Блюдо '{cartItem.Name}' больше не доступно в количестве {cartItem.Quantity} шт.\n" +
                                   $"Доступно: {dish?.AvailableQuantity ?? 0} шт.\n" +
                                   "Пожалуйста, обновите корзину.",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadDishes();
                    UpdateCartDisplay();
                    return;
                }
            }

            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();

                    // СНАЧАЛА получим employeeid
                    int employeeId = 1; // значение по умолчанию

                    try
                    {
                        // Проверяем, есть ли сотрудник с ID=1
                        string checkEmpQuery = "SELECT COUNT(*) FROM employee WHERE employeeid = 1";
                        using (var checkCmd = new NpgsqlCommand(checkEmpQuery, connection))
                        {
                            int empCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (empCount == 0)
                            {
                                // Ищем любого сотрудника
                                string findEmpQuery = "SELECT employeeid FROM employee LIMIT 1";
                                using (var findCmd = new NpgsqlCommand(findEmpQuery, connection))
                                {
                                    var result = findCmd.ExecuteScalar();
                                    if (result != null && result != DBNull.Value)
                                    {
                                        employeeId = Convert.ToInt32(result);
                                    }
                                    else
                                    {
                                        // Если вообще нет сотрудников, создаем одного
                                        string createEmpQuery = @"
                                    INSERT INTO employee (firstname, lastname) 
                                    VALUES ('Системный', 'Оператор') 
                                    RETURNING employeeid";
                                        using (var createCmd = new NpgsqlCommand(createEmpQuery, connection))
                                        {
                                            employeeId = Convert.ToInt32(createCmd.ExecuteScalar());
                                            Console.WriteLine($"Создан системный оператор с ID={employeeId}");
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine($"Будет использован employeeid: {employeeId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при получении employeeid: {ex.Message}");
                        employeeId = 1; // используем значение по умолчанию
                    }

                    // Начинаем транзакцию
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Создаем заказ - ВАЖНО: добавляем statusid = 4
                            int orderId;
                            string orderQuery = @"
                        INSERT INTO orders (clientid, employeeid, statusid, orderdate, ordertime) 
                        VALUES (@clientid, @employeeid, @statusid, @orderdate, @ordertime) 
                        RETURNING orderid";

                            using (var command = new NpgsqlCommand(orderQuery, connection))
                            {
                                command.Transaction = transaction;
                                command.Parameters.AddWithValue("@clientid", clientId);
                                command.Parameters.AddWithValue("@employeeid", employeeId);
                                command.Parameters.AddWithValue("@statusid", 4); // статус "В процессе"
                                command.Parameters.AddWithValue("@orderdate", DateTime.Today);
                                command.Parameters.AddWithValue("@ordertime", DateTime.Now.TimeOfDay);

                                orderId = Convert.ToInt32(command.ExecuteScalar());
                                Console.WriteLine($"Заказ создан: ID={orderId}, clientId={clientId}, employeeId={employeeId}, statusId=4");
                            }

                            // 2. Добавляем детали заказа
                            foreach (var item in cartItems)
                            {
                                // Получаем categoryid для блюда
                                int categoryId = 1;
                                try
                                {
                                    using (var command = new NpgsqlCommand(
                                        "SELECT categoryid FROM dish WHERE dishid = @dishid", connection))
                                    {
                                        command.Transaction = transaction;
                                        command.Parameters.AddWithValue("@dishid", item.Id);
                                        var result = command.ExecuteScalar();
                                        if (result != null && result != DBNull.Value)
                                        {
                                            categoryId = Convert.ToInt32(result);
                                        }
                                    }
                                }
                                catch
                                {
                                    categoryId = 1;
                                }

                                // Добавляем детали заказа
                                string detailsQuery = @"
                            INSERT INTO ordersdetails 
                            (dishid, orderid, categoryid, clientid, employeeid, statusid, price, quantity) 
                            VALUES (@dishid, @orderid, @categoryid, @clientid, @employeeid,@statusid, @price, @quantity)";


                                using (var command = new NpgsqlCommand(detailsQuery, connection))
                                {
                                    command.Transaction = transaction;
                                    command.Parameters.AddWithValue("@dishid", item.Id);
                                    command.Parameters.AddWithValue("@orderid", orderId);
                                    command.Parameters.AddWithValue("@categoryid", categoryId);
                                    command.Parameters.AddWithValue("@clientid", clientId);
                                    command.Parameters.AddWithValue("@employeeid", employeeId);
                                    command.Parameters.AddWithValue("@statusid", 4); 
                                    command.Parameters.AddWithValue("@price", item.Price);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);

                                    command.ExecuteNonQuery();
                                    Console.WriteLine($"Добавлено блюдо: {item.Name}");
                                }

                                // Уменьшаем количество блюд
                                using (var command = new NpgsqlCommand(
                                    @"UPDATE dish 
                              SET quantity = quantity - @quantity 
                              WHERE dishid = @dishid",
                                    connection))
                                {
                                    command.Transaction = transaction;
                                    command.Parameters.AddWithValue("@dishid", item.Id);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);
                                    command.ExecuteNonQuery();
                                }
                            }

                            // Подтверждаем транзакцию
                            transaction.Commit();
                            Console.WriteLine($"Транзакция успешно завершена для заказа #{orderId}");

                            // Очищаем корзину
                            cartItems.Clear();

                            // Обновляем список блюд
                            LoadDishes();
                            UpdateCartDisplay();

                            MessageBox.Show($"✅ Заказ №{orderId} успешно оформлен!\n" +
                                          $"💰 Сумма: {TotalAmountText.Text}\n" +
                                          $"📊 Статус: В процессе\n" +
                                          $"🙏 Спасибо за ваш заказ, {clientName}!",
                                          "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                transaction.Rollback();
                                Console.WriteLine($"Транзакция откатана: {ex.Message}");
                            }
                            catch (Exception rollbackEx)
                            {
                                Console.WriteLine($"Ошибка при откате транзакции: {rollbackEx.Message}");
                            }

                            // Проверяем, если это ошибка о NULL в statutapi
                            if (ex.Message.Contains("statutapi") && ex.Message.Contains("NOT NULL"))
                            {
                                throw new Exception($"Ошибка при создании заказа: Не указан статус заказа. Проверьте наличие статуса с ID=4 в таблице status.");
                            }
                            else
                            {
                                throw new Exception($"Ошибка при создании заказа: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка оформления: {ex.Message}");
                MessageBox.Show($"❌ Ошибка при оформлении заказа: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Просмотр истории заказов
        private void ViewOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            o.orderid, 
                            o.orderdate, 
                            s.statusname,
                            COUNT(od.orderdetailsid) as items_count,
                            SUM(od.price * od.quantity) as total_amount
                        FROM orders o
                        LEFT JOIN ordersdetails od ON o.orderid = od.orderid
                        LEFT JOIN status s ON o.statusid = s.statusid
                        WHERE o.clientid = @clientid
                        GROUP BY o.orderid, o.orderdate, s.statusname
                        ORDER BY o.orderdate DESC, o.orderid DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientid", clientId);

                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);

                            if (dataTable.Rows.Count > 0)
                            {
                                string ordersInfo = $"Заказы клиента: {clientName}\n\n";
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    ordersInfo += $"Заказ #{row["orderid"]} от {Convert.ToDateTime(row["orderdate"]):dd.MM.yyyy}\n";
                                    ordersInfo += $"Статус: {row["statusname"]}\n";
                                    ordersInfo += $"Позиций: {row["items_count"]}\n";
                                    ordersInfo += $"Сумма: {Convert.ToDecimal(row["total_amount"]):C}\n";
                                    ordersInfo += "────────────────────\n";
                                }

                                MessageBox.Show(ordersInfo, "История заказов",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("У вас пока нет заказов.",
                                                "История заказов",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории заказов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Валидация ввода количества
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }

            // Если текстбокс связан с элементом корзины
            if (sender is TextBox textBox && textBox.DataContext is CartItem cartItem)
            {
                // Пытаемся получить новое значение
                string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
                if (int.TryParse(newText, out int newQuantity))
                {
                    // Проверяем, не превышает ли новое количество доступное количество
                    if (newQuantity > cartItem.AvailableQuantity)
                    {
                        MessageBox.Show($"Нельзя установить количество больше {cartItem.AvailableQuantity} шт.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Handled = true;
                        return;
                    }

                    if (newQuantity <= 0)
                    {
                        MessageBox.Show("Количество должно быть больше 0",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Handled = true;
                    }
                }
            }
        }

        // Обработчик изменения текста в TextBox количества
        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is CartItem cartItem)
            {
                if (int.TryParse(textBox.Text, out int newQuantity))
                {
                    if (newQuantity > 0 && newQuantity <= cartItem.AvailableQuantity)
                    {
                        cartItem.Quantity = newQuantity;
                        UpdateCartDisplay();
                    }
                    else if (newQuantity > cartItem.AvailableQuantity)
                    {
                        textBox.Text = cartItem.AvailableQuantity.ToString();
                        cartItem.Quantity = cartItem.AvailableQuantity;
                        UpdateCartDisplay();
                        MessageBox.Show($"Максимальное количество: {cartItem.AvailableQuantity} шт.",
                                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        // Модели данных
        public class Dish
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int AvailableQuantity { get; set; }
            public string Category { get; set; }
        }

        public class CartItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int AvailableQuantity { get; set; }
            public int Quantity { get; set; }
        }
    }
}
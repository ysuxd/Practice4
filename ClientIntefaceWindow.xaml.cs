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

        public ClientIntefaceWindow()
        {
            InitializeComponent();
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
                Console.WriteLine($"DEBUG: Загрузка информации для userid={CurrentUserId}, login={CurrentUsername}");

                // Временное приветствие
                WelcomeText.Text = $"Здравствуйте, {CurrentUsername}!";
                UserInfoText.Text = $"Касимовский нефтегазовый колледж • {CurrentRole}";

                // Сначала пытаемся получить информацию о клиенте
                clientId = 0;
                clientName = "";

                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();

                    // Запрос 1: Проверяем, есть ли клиент с таким userid
                    string query = @"
                SELECT 
                    c.clientid,
                    c.firstname,
                    c.lastname,
                    c.surname,
                    u.login
                FROM users u
                LEFT JOIN client c ON u.userid = c.userid
                WHERE u.userid = @userid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userid", CurrentUserId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Проверяем, есть ли clientid
                                if (!reader.IsDBNull(0))
                                {
                                    clientId = reader.GetInt32(0);

                                    // Собираем ФИО
                                    string lastName = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
                                    string firstName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                                    string surname = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();

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
                                    }
                                    else
                                    {
                                        // Если ФИО пустые, используем логин
                                        clientName = reader.GetString(4); // login
                                        WelcomeText.Text = $"Здравствуйте, {clientName}!";
                                    }

                                    Console.WriteLine($"DEBUG: Клиент найден - ID={clientId}, Name={clientName}");
                                }
                                else
                                {
                                    // Клиент не найден, но пользователь есть
                                    Console.WriteLine($"DEBUG: Клиент не найден для userid={CurrentUserId}");

                                    // Показываем сообщение об ошибке
                                    MessageBox.Show("Не удалось определить информацию о клиенте.\nОбратитесь к администратору.",
                                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                                    // Используем логин как имя
                                    clientName = CurrentUsername;
                                    WelcomeText.Text = $"Здравствуйте, {CurrentUsername}!";
                                }
                            }
                            else
                            {
                                // Пользователь не найден - это странно
                                Console.WriteLine($"DEBUG: Пользователь не найден в базе - userid={CurrentUserId}");
                                clientName = CurrentUsername;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации пользователя: {ex.Message}\n\nПроверьте соединение с базой данных и правильность запросов.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Устанавливаем значения по умолчанию
                clientId = 0;
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
                    LoadDishes(); // Обновляем информацию о доступности
                    UpdateCartDisplay();
                    return;
                }
            }

            try
            {
                using (var connection = dbconnection.GetConnection())
                {
                    connection.Open();

                    // Начинаем транзакцию
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Создаем новый заказ
                            int orderId;
                            using (var command = new NpgsqlCommand(
                                @"INSERT INTO orders (clientid, employeeid, statusid, orderdate, ordertime) 
                                  VALUES (@clientid, 1, 1, @orderdate, @ordertime) 
                                  RETURNING orderid", connection))
                            {
                                command.Transaction = transaction;
                                command.Parameters.AddWithValue("@clientid", clientId);
                                command.Parameters.AddWithValue("@orderdate", DateTime.Today);
                                command.Parameters.AddWithValue("@ordertime", DateTime.Now.TimeOfDay);

                                orderId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            // 2. Добавляем детали заказа для каждого товара в корзине
                            // и одновременно уменьшаем количество доступных блюд
                            foreach (var item in cartItems)
                            {
                                // Получаем categoryid для блюда
                                int categoryId;
                                using (var command = new NpgsqlCommand(
                                    "SELECT categoryid FROM dish WHERE dishid = @dishid", connection))
                                {
                                    command.Transaction = transaction;
                                    command.Parameters.AddWithValue("@dishid", item.Id);
                                    categoryId = Convert.ToInt32(command.ExecuteScalar());
                                }

                                // Добавляем детали заказа
                                using (var command = new NpgsqlCommand(
                                    @"INSERT INTO ordersdetails 
                                      (dishid, orderid, categoryid, clientid, employeeid, statusid, price, quantity) 
                                      VALUES (@dishid, @orderid, @categoryid, @clientid, 1, 1, @price, @quantity)",
                                    connection))
                                {
                                    command.Transaction = transaction;
                                    command.Parameters.AddWithValue("@dishid", item.Id);
                                    command.Parameters.AddWithValue("@orderid", orderId);
                                    command.Parameters.AddWithValue("@categoryid", categoryId);
                                    command.Parameters.AddWithValue("@clientid", clientId);
                                    command.Parameters.AddWithValue("@price", item.Price);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);

                                    command.ExecuteNonQuery();
                                }

                                // Уменьшаем количество блюд на складе
                                using (var command = new NpgsqlCommand(
                                    @"UPDATE dish 
                                      SET quantity = quantity - @quantity 
                                      WHERE dishid = @dishid AND quantity >= @quantity",
                                    connection))
                                {
                                    command.Transaction = transaction;
                                    command.Parameters.AddWithValue("@dishid", item.Id);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);

                                    int rowsAffected = command.ExecuteNonQuery();
                                    if (rowsAffected == 0)
                                    {
                                        throw new Exception($"Не удалось обновить количество для блюда '{item.Name}'. " +
                                                          $"Возможно, количество изменилось.");
                                    }
                                }
                            }

                            // Подтверждаем транзакцию
                            transaction.Commit();

                            // Очищаем корзину
                            cartItems.Clear();

                            // Обновляем список блюд (количества изменились)
                            LoadDishes();
                            UpdateCartDisplay();

                            MessageBox.Show($"Заказ №{orderId} успешно оформлен!\n" +
                                          $"Сумма: {TotalAmountText.Text}\n" +
                                          $"Спасибо за ваш заказ, {clientName}!",
                                          "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Ошибка при создании заказа: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}",
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
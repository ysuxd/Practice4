using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для EmployeeInterfaceWindow.xaml
    /// </summary>
    public partial class EmployeeInterfaceWindow : Window
    {
        public int CurrentUserId { get; set; }
        public string CurrentUsername { get; set; }
        public string CurrentRole { get; set; }

        public EmployeeInterfaceWindow(int userId, string username, string role)
        {
            InitializeComponent();
            CurrentUserId = userId;
            CurrentUsername = username;
            CurrentRole = role;

            // Устанавливаем данные в интерфейсе
            this.DataContext = this;

            // Устанавливаем заголовок окна с именем сотрудника
            this.Title = $"Панель сотрудника - {username}";
        }

        // Кнопка меню блюд
        private void DishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DishWindow dishWindow = new DishWindow();
                dishWindow.Owner = this;
                dishWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии меню: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка заказов
        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OrderWindow orderWindow = new OrderWindow();
                orderWindow.Owner = this;
                orderWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка деталей заказов
        private void OrderDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OrderDetailsWindow orderDetailsWindow = new OrderDetailsWindow();
                orderDetailsWindow.Owner = this;
                orderDetailsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии деталей заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка клиентов
        private void ClientButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientWindow clientWindow = new ClientWindow();
                clientWindow.Owner = this;
                clientWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка обновления
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Здесь можно добавить логику обновления данных
                MessageBox.Show("Информация обновлена", "Обновление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка выхода
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Закрываем текущее окно
                    this.Close();

                    // Показываем окно авторизации
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выходе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработка закрытия окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Если главное окно авторизации еще открыто, показываем его
            Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.Show();
        }
    }
}
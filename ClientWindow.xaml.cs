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
using Npgsql;

namespace Practice
{
    public partial class ClientWindow : Window
    {
        private DatabaseConnection dbconnection;

        public ClientWindow()
        {
            InitializeComponent();
            ClientDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            dbconnection = new DatabaseConnection();
            LoadData();
        }

        public void LoadData()
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                // Добавляем вычисление полного имени
                string query = @"SELECT 
                        clientid, 
                        firstname, 
                        surname, 
                        lastname,
                        COALESCE(lastname || ' ' || firstname || ' ' || surname, lastname || ' ' || firstname) as fullname
                        FROM Client 
                        ORDER BY clientid ASC";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // НЕ переименовываем столбцы, чтобы Binding работал правильно
                        // Оставляем оригинальные имена, которые указаны в Binding в XAML
                        ClientDataGrid.ItemsSource = dataTable.DefaultView;

                        // Обновляем счетчик клиентов
                        UpdateClientsCount(dataTable.Rows.Count);
                    }
                }
            }
        }

        // Метод для обновления счетчика клиентов
        private void UpdateClientsCount(int count)
        {
            ClientsCountText.Text = $"Всего клиентов: {count}";
        }

        private void AddClient(string firstName, string surname, string lastName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into Client (firstName,surname,lastName) Values (@firstName, @surname, @lastName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstName", firstName);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@lastName", lastName);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void UpdateClient(int clientid, string firstName, string surname, string lastName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update Client set firstname=@firstName, surname=@surname, lastname=@lastName WHERE clientid=@clientid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstName", firstName);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@lastName", lastName);
                    command.Parameters.AddWithValue("@clientid", clientid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteClient(int clientid)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Delete from Client WHERE clientid=@clientid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@clientid", clientid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameTextBox.Text.Trim();
            string surname = SurnameTextBox.Text.Trim();
            string lastName = LastNameTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                AddClient(firstName, surname, lastName);
                LoadData();
                ClearInputFields();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные данные (имя и фамилия обязательны).");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = ClientDataGrid.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                int clientid = Convert.ToInt32(selectedRow["Номер"]);
                string firstName = FirstNameTextBox.Text.Trim();
                string surname = SurnameTextBox.Text.Trim();
                string lastName = LastNameTextBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите обновить данные клиента?",
                        "Подтверждение обновления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateClient(clientid, firstName, surname, lastName);
                        LoadData();
                        ClearInputFields();
                    }
                }
                else
                {
                    MessageBox.Show("Пожалуйста, введите корректные данные для обновления (имя и фамилия обязательны)");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для обновления");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = ClientDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                int clientid = Convert.ToInt32(selectedRow["Номер"]);
                string firstName = selectedRow["Имя"].ToString();
                string lastName = selectedRow["Фамилия"].ToString();

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы уверены, что хотите удалить клиента {lastName} {firstName}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteClient(clientid);
                    LoadData();
                    ClearInputFields();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }

        // Обработчик события выбора в DataGrid
        private void ClientDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedRow = ClientDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Заполняем поля ввода данными выбранного клиента
                LastNameTextBox.Text = selectedRow["Фамилия"].ToString();
                FirstNameTextBox.Text = selectedRow["Имя"].ToString();
                SurnameTextBox.Text = selectedRow["Отчество"].ToString();
            }
            else
            {
                // Очищаем поля ввода, если ничего не выбрано
                ClearInputFields();
            }
        }

        // Метод для очистки полей ввода
        private void ClearInputFields()
        {
            LastNameTextBox.Text = string.Empty;
            FirstNameTextBox.Text = string.Empty;
            SurnameTextBox.Text = string.Empty;
        }
    }
}
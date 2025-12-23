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
    public partial class EmployeeWindow : Window
    {
        private DatabaseConnection dbconnection;

        public EmployeeWindow()
        {
            InitializeComponent();
            EmployeeDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
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
                                employeeid, 
                                firstname, 
                                surname, 
                                lastname,
                                COALESCE(lastname || ' ' || firstname || ' ' || surname, lastname || ' ' || firstname) as fullname
                                FROM employee 
                                ORDER BY employeeid";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // НЕ переименовываем столбцы, чтобы Binding работал правильно!
                        // Оставляем оригинальные имена: employeeid, firstname, surname, lastname, fullname
                        EmployeeDataGrid.ItemsSource = dataTable.DefaultView;

                        // Обновляем счетчик сотрудников
                        UpdateEmployeesCount(dataTable.Rows.Count);
                    }
                }
            }
        }

        // Метод для обновления счетчика сотрудников
        private void UpdateEmployeesCount(int count)
        {
            EmployeesCountText.Text = $"Всего сотрудников: {count}";
        }

        private void AddEmployee(string firstName, string surname, string lastName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into Employee (firstName, surname, lastName) Values (@firstName, @surname, @lastName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstName", firstName);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@lastName", lastName);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void UpdateEmployee(int employeeid, string firstName, string surname, string lastName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update Employee set firstname=@firstName, surname=@surname, lastname=@lastName WHERE employeeid=@employeeid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstName", firstName);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@lastName", lastName);
                    command.Parameters.AddWithValue("@employeeid", employeeid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteEmployee(int employeeid)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Delete from Employee WHERE employeeid=@employeeid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@employeeid", employeeid);
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
                AddEmployee(firstName, surname, lastName);
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
            DataRowView selectedRow = EmployeeDataGrid.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int employeeid = Convert.ToInt32(selectedRow["employeeid"]);
                string firstName = FirstNameTextBox.Text.Trim();
                string surname = SurnameTextBox.Text.Trim();
                string lastName = LastNameTextBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите обновить данные сотрудника?",
                        "Подтверждение обновления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateEmployee(employeeid, firstName, surname, lastName);
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
            DataRowView selectedRow = EmployeeDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальное имя столбца
                int employeeid = Convert.ToInt32(selectedRow["employeeid"]);
                string firstName = selectedRow["firstname"].ToString();
                string lastName = selectedRow["lastname"].ToString();

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы уверены, что хотите удалить сотрудника {lastName} {firstName}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteEmployee(employeeid);
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
        private void EmployeeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedRow = EmployeeDataGrid.SelectedItem as DataRowView;

            if (selectedRow != null)
            {
                // Используем оригинальные имена столбцов
                LastNameTextBox.Text = selectedRow["lastname"].ToString();
                FirstNameTextBox.Text = selectedRow["firstname"].ToString();
                SurnameTextBox.Text = selectedRow["surname"].ToString();
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
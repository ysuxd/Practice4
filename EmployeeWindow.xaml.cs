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
using Npgsql;

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
                string query = "SELECT employeeid, firstname, surname, lastname FROM employee ORDER BY employeeid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // Переименуем заголовки столбцов для красоты
                        if (dataTable.Columns.Contains("employeeid"))
                            dataTable.Columns["employeeid"].ColumnName = "Номер";
                        if (dataTable.Columns.Contains("firstname"))
                            dataTable.Columns["firstname"].ColumnName = "Имя";
                        if (dataTable.Columns.Contains("surname"))
                            dataTable.Columns["surname"].ColumnName = "Отчество";
                        if (dataTable.Columns.Contains("lastname"))
                            dataTable.Columns["lastname"].ColumnName = "Фамилия";

                        EmployeeDataGrid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
        }

        private void AddClient(string firstName, string surname, string lastName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into Employee (firstName,surname,lastName) Values (@firstName, @surname, @lastName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstName", firstName);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@lastName", lastName);
                    command.ExecuteNonQuery();
                }
            }
        }


        private void UpdateClient(int employeeid, string firstName, string surname, string lastName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Update Client set firstname=@firstName, surname=@surname, lastname=@lastName WHERE employeeid=@employeeid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstName", firstName);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@lastName", lastName);
                    command.Parameters.AddWithValue("@employeeid", employeeid); // Добавлен этот параметр
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteClient(int employeeid)
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
            string firstName = FirstNameTextBox.Text;
            string surname = SurnameNameTextBox.Text;
            string lastName = LastNameTextBox.Text;

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                AddClient(firstName, surname, lastName);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные данные.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = (DataRowView)EmployeeDataGrid.SelectedItem;
            if (selectedRow != null)
            {
                int employeeid = Convert.ToInt32(selectedRow["employeeid"]);
                string firstName = FirstNameTextBox.Text;
                string surname = SurnameNameTextBox.Text;
                string lastName = LastNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    UpdateClient(employeeid, firstName, surname, lastName);
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
            DataRowView selectedRow = (DataRowView)EmployeeDataGrid.SelectedItem;

            if (selectedRow != null)
            {
                int employeeid = Convert.ToInt32(selectedRow["employeeid"]);
                DeleteClient(employeeid);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }
    }
}

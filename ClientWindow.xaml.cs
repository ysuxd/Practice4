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
                string query = "SELECT clientid, firstname, surname, lastname FROM Client ORDER BY clientid ASC";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // Переименуем заголовки столбцов для красоты
                        if (dataTable.Columns.Contains("clientid"))
                            dataTable.Columns["clientid"].ColumnName = "Номер";
                        if (dataTable.Columns.Contains("firstname"))
                            dataTable.Columns["firstname"].ColumnName = "Имя";
                        if (dataTable.Columns.Contains("surname"))
                            dataTable.Columns["surname"].ColumnName = "Отчество";
                        if (dataTable.Columns.Contains("lastname"))
                            dataTable.Columns["lastname"].ColumnName = "Фамилия";

                        ClientDataGrid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
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
            DataRowView selectedRow = (DataRowView)ClientDataGrid.SelectedItem;
            if (selectedRow != null)
            {
                // ИСПРАВЛЕНО: используем новое имя столбца "Номер" вместо "clientid"
                int clientid = Convert.ToInt32(selectedRow["Номер"]);
                string firstName = FirstNameTextBox.Text;
                string surname = SurnameNameTextBox.Text;
                string lastName = LastNameTextBox.Text;

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    UpdateClient(clientid, firstName, surname, lastName);
                    LoadData();
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
            DataRowView selectedRow = (DataRowView)ClientDataGrid.SelectedItem;

            if (selectedRow != null)
            {
                // ИСПРАВЛЕНО: используем новое имя столбца "Номер" вместо "clientid"
                int clientid = Convert.ToInt32(selectedRow["Номер"]);
                DeleteClient(clientid);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }
    }
}
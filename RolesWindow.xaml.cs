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
    /// <summary>
    /// Логика взаимодействия для RolesWindow.xaml
    /// </summary>
    public partial class RolesWindow : Window
    {
        private DatabaseConnection dbconnection;

        public RolesWindow()
        {
            InitializeComponent();
            RolesDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            dbconnection = new DatabaseConnection();
            LoadData();
        }

        public void LoadData()
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "SELECT roleid, rolename FROM roles ORDER BY roleid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // Переименуем заголовки столбцов для красоты
                        if (dataTable.Columns.Contains("roleid"))
                            dataTable.Columns["roleid"].ColumnName = "Номер";
                        if (dataTable.Columns.Contains("rolename"))
                            dataTable.Columns["rolename"].ColumnName = "Название";

                        RolesDataGrid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
        }

        private void AddClient(string roleName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Insert Into roles (roleName) Values (@roleName)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roleName", roleName);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void UpdateClient(int roleid, string roleName)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                // ИСПРАВЛЕНО: было "roleyd", должно быть "roleid"
                string query = "Update roles set rolename=@roleName WHERE roleid=@roleid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roleName", roleName);
                    command.Parameters.AddWithValue("@roleid", roleid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteClient(int roleid)
        {
            using (var connection = dbconnection.GetConnection())
            {
                connection.Open();
                string query = "Delete from roles WHERE roleid=@roleid";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roleid", roleid);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string roleName = RolesNameTextBox.Text;

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                AddClient(roleName);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные данные.");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = (DataRowView)RolesDataGrid.SelectedItem;
            if (selectedRow != null)
            {
                // ИСПРАВЛЕНО: используем новое имя столбца "Номер" вместо "roleid"
                int roleid = Convert.ToInt32(selectedRow["Номер"]);
                string roleName = RolesNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(roleName))
                {
                    UpdateClient(roleid, roleName);
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
            DataRowView selectedRow = (DataRowView)RolesDataGrid.SelectedItem;

            if (selectedRow != null)
            {
                // ИСПРАВЛЕНО: используем новое имя столбца "Номер" вместо "roleid"
                int roleid = Convert.ToInt32(selectedRow["Номер"]);
                DeleteClient(roleid);
                LoadData();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления");
            }
        }
    }
}
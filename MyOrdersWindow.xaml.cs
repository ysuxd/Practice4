using System;
using System.Data;
using System.Windows;
using Npgsql;

namespace Practice
{
    public partial class MyOrdersWindow : Window
    {
        private DatabaseConnection dbconnection;
        private int currentClientId;
        private string currentClientName;

        public MyOrdersWindow(int clientId, string clientName)
        {
            InitializeComponent();
            dbconnection = new DatabaseConnection();
            currentClientId = clientId;
            currentClientName = clientName;
            Title = $"Мои заказы - {clientName}";
            LoadData();
        }

        private void LoadData()
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
                            o.ordertime,
                            s.statusname,
                            COUNT(od.orderdetailsid) as items_count,
                            SUM(od.price * od.quantity) as total_amount
                        FROM orders o
                        LEFT JOIN ordersdetails od ON o.orderid = od.orderid
                        LEFT JOIN status s ON o.statusid = s.statusid
                        WHERE o.clientid = @clientid
                        GROUP BY o.orderid, o.orderdate, o.ordertime, s.statusname
                        ORDER BY o.orderdate DESC, o.ordertime DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientid", currentClientId);

                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);

                            if (dataTable.Rows.Count > 0)
                            {
                                MyOrdersDataGrid.ItemsSource = dataTable.DefaultView;
                                OrdersCountText.Text = $"Всего заказов: {dataTable.Rows.Count}";
                            }
                            else
                            {
                                MessageBox.Show("У вас пока нет заказов.",
                                                "Мои заказы",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                                Close();
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

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyOrdersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    int orderId = Convert.ToInt32(selectedRow["orderid"]);
                    
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для просмотра деталей.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
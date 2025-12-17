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
    /// Логика взаимодействия для ClientIntefaceWindow.xaml
    /// </summary>
    public partial class ClientIntefaceWindow : Window
    {

        public int CurrentUserId { get; set; }
        public string CurrentUsername { get; set; }
        public string CurrentRole { get; set; }

        public ClientIntefaceWindow()
        {
            InitializeComponent();
        }
    }
}

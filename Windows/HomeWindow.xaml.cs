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

namespace RW_Explorer.Windows
{
    public partial class HomeWindow : Window
    {
        public HomeWindow()
        {
            InitializeComponent();
        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var newWindow = new HomeWindow();
            newWindow.Left = this.Left;
            newWindow.Top = this.Top;
            newWindow.Width = this.Width;
            newWindow.Height = this.Height;
            newWindow.WindowState = this.WindowState;
            newWindow.Show();
            this.Close();
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }
    }
}

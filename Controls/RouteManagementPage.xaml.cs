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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RW_Explorer.Controls
{
    /// <summary>
    /// RouteManagementPage.xaml 的交互逻辑
    /// </summary>
    public partial class RouteManagementPage : Page
    {
        public RouteManagementPage()
        {
            InitializeComponent();
            Loaded += RouteManagementPage_Loaded;
        }

        private void RouteManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowRouteList();
        }

        private void ShowRouteList_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void AddRoute_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SearchRoutes_Click(object sender, RoutedEventArgs e)
        {
            if (mainFrame.Content is RouteListPage routeListPage)
            {
                routeListPage.SearchRoutes(txtSearch.Text);
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch.Text.Length > 2 || string.IsNullOrEmpty(txtSearch.Text))
            {
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            string searchText = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                ShowRouteList();
                return;
            }


            bool hasResults = true;

            if (hasResults)
            {
                txtNoResults.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtNoResults.Visibility = Visibility.Visible;
                mainFrame.Content = null;
            }
        }

        private void AdvancedFilter_Click(object sender, RoutedEventArgs e)
        {
        }
        private void ShowRouteList()
        {
            mainFrame.Navigate(new RouteListPage());
        }
    }
}

using RW_Explorer.Class;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RW_Explorer.Controls
{
    public partial class RouteListPage : Page
    {
        private ObservableCollection<RouteItemViewModel> _routes = new ObservableCollection<RouteItemViewModel>();

        public RouteListPage()
        {
            InitializeComponent();
            Loaded += RouteListPage_Loaded; // 保留事件订阅
        }

        // 添加缺失的Loaded事件处理方法
        private void RouteListPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoutes();
            listRoutes.ItemsSource = _routes;
        }

        public void SearchRoutes(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                listRoutes.ItemsSource = _routes;
            }
            else
            {
                var filtered = _routes.Where(r =>
                    r.DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Providers.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();

                listRoutes.ItemsSource = filtered;
            }
        }

        private void LoadRoutes()
        {
            _routes.Clear();

            var config = AppConfig.Load("config.json");
            if (string.IsNullOrEmpty(config?.GameDirectory)) return;

            string routesPath = Path.Combine(config.GameDirectory, "Content", "Routes");
            if (!Directory.Exists(routesPath)) return;

            foreach (var routeDir in Directory.GetDirectories(routesPath))
            {
                string xmlPath = Path.Combine(routeDir, "RouteProperties.xml");
                if (File.Exists(xmlPath))
                {
                    try
                    {
                        string xmlContent = File.ReadAllText(xmlPath);
                        var props = RouteProperties.ParseFromXml(xmlContent);
                        props.FolderPath = routeDir;

                        // 查找线路图片
                        string imagePath = Path.Combine(routeDir, "RouteInformation", "Image.png");
                        if (File.Exists(imagePath))
                        {
                            props.ImagePath = imagePath;
                        }

                        _routes.Add(new RouteItemViewModel(props));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"加载线路失败: {Path.GetFileName(routeDir)}, 错误: {ex.Message}");
                    }
                }
            }
        }

        private void OpenRouteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (listRoutes.SelectedItem is RouteItemViewModel selectedRoute)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{selectedRoute.FolderPath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个线路", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditRouteProperties_Click(object sender, RoutedEventArgs e)
        {
            if (listRoutes.SelectedItem is RouteItemViewModel selectedRoute)
            {
                string xmlPath = Path.Combine(selectedRoute.FolderPath, "RouteProperties.xml");
                if (File.Exists(xmlPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "notepad.exe",
                            Arguments = $"\"{xmlPath}\"",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"打开失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择一个线路", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
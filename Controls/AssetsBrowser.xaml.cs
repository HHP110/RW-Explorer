using RW_Explorer.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static RW_Explorer.Controls.IconExtensions;
using MediaBrushes = System.Windows.Media.Brushes;

namespace RW_Explorer.Controls
{
    public static class IconExtensions
    {
        public static ImageSource ToImageSource(this Icon icon)
        {
            if (icon == null) return null;

            using (icon)
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
        }
        public static class Win32
        {
            public const uint SHGFI_ICON = 0x000000100;
            public const uint SHGFI_LARGEICON = 0x000000000;
            public const uint SHGFI_SMALLICON = 0x000000001;
            public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(
                string pszPath,
                uint dwFileAttributes,
                out SHFILEINFO psfi,
                uint cbSizeFileInfo,
                uint uFlags);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        public static ImageSource ToImageSource(this Bitmap bitmap)
        {
            if (bitmap == null) return null;

            using (bitmap)
            {
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }

    public partial class AssetsBrowser : System.Windows.Controls.UserControl
    {
        private FileSystemItem _rightClickedItem;
        private const int PageSize = 100;
        private int _currentPage = 1;
        private string _currentDirectory;
        private string _assetsPath;
        private List<FileSystemItem> _allItems = new List<FileSystemItem>();
        private CancellationTokenSource _cancellationTokenSource;
        private RouteProperties _selectedRouteProperties;
        private Dictionary<string, RouteProperties> _routesCache = new Dictionary<string, RouteProperties>();


        public AssetsBrowser()
        {
            InitializeComponent();
            Loaded += AssetsBrowser_Loaded;

        }

        private void LoadAllRoutesProperties()
        {
            var config = AppConfig.Load("config.json");
            if (string.IsNullOrEmpty(config?.GameDirectory))
            {
                Debug.WriteLine("配置文件中没有找到GameDirectory");
                return;
            }

            string routesPath = Path.Combine(config.GameDirectory, "Content", "Routes");
            Debug.WriteLine($"Routes路径: {routesPath}");

            if (!Directory.Exists(routesPath))
            {
                Debug.WriteLine($"Routes目录不存在: {routesPath}");
                return;
            }
            var routeDirectories = Directory.GetDirectories(routesPath);
            Debug.WriteLine($"找到 {routeDirectories.Length} 个Route目录");

            foreach (var routeDir in routeDirectories)
            {
                string xmlPath = Path.Combine(routeDir, "RouteProperties.xml");
                Debug.WriteLine($"检查文件: {xmlPath}");

                if (File.Exists(xmlPath))
                {
                    try
                    {
                        string xmlContent = File.ReadAllText(xmlPath);
                        var props = RouteProperties.ParseFromXml(xmlContent);
                        props.CreationTime = Directory.GetCreationTime(routeDir);

                        string routeName = Path.GetFileName(routeDir);
                        _routesCache[routeName] = props;

                        Debug.WriteLine($"已加载线路配置: {routeName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"解析线路配置失败: {Path.GetFileName(routeDir)}, 错误: {ex.Message}");
                    }
                }
            }

            Debug.WriteLine($"共加载 {_routesCache.Count} 条线路配置");

            Debug.WriteLine("已加载的路线名称列表:");
            foreach (var routeName in _routesCache.Keys)
            {
                Debug.WriteLine($"  - {routeName}");
            }
        }
        private void UpdateDetailPanel(FileSystemItem item)
        {
            Dispatcher.Invoke(() =>
            {
                detailPanel.Children.Clear();

                if (item == null || !item.IsDirectory)
                {
                    detailPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "未选择文件夹",
                            Foreground = MediaBrushes.Gray,
                            FontStyle = FontStyles.Italic
                        });
                    return;
                }

                detailPanel.Children.Add(
                    new TextBlock
                    {
                        Text = "基本信息",
                        FontWeight = FontWeights.Bold,
                        Foreground = MediaBrushes.White
                    });

                detailPanel.Children.Add(CreateDetailRow("名称:", item.Name));
                detailPanel.Children.Add(CreateDetailRow("路径:", item.Path));
                detailPanel.Children.Add(CreateDetailRow("大小:", item.Size));

                var config = AppConfig.Load("config.json");
                string comment = config.GetFolderComment(item.Path);

                detailPanel.Children.Add(
                    new TextBlock
                    {
                        Text = "我的备注",
                        FontWeight = FontWeights.Bold,
                        Foreground = MediaBrushes.White,
                        Margin = new Thickness(0, 10, 0, 0)
                    });

                if (!string.IsNullOrEmpty(comment))
                {
                    detailPanel.Children.Add(
                        new TextBlock
                        {
                            Text = comment,
                            Foreground = MediaBrushes.White,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 5, 0, 0)
                        });
                }
                else
                {
                    detailPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "暂无备注",
                            Foreground = MediaBrushes.Gray,
                            FontStyle = FontStyles.Italic,
                            Margin = new Thickness(0, 5, 0, 0)
                        });
                }

                string folderName = Path.GetFileName(item.Path);
                var matchedRoutes = _routesCache
                    .Where(r => r.Key.Equals(folderName, StringComparison.OrdinalIgnoreCase) ||
                               r.Value.Providers.Any(p => p.Equals(folderName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (matchedRoutes.Any())
                {
                    detailPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "线路信息",
                            FontWeight = FontWeights.Bold,
                            Foreground = MediaBrushes.White,
                            Margin = new Thickness(0, 10, 0, 0)
                        });

                    foreach (var route in matchedRoutes)
                    {
                        detailPanel.Children.Add(CreateDetailRow("线路名称:", route.Value.DisplayName));
                        detailPanel.Children.Add(CreateDetailRow("路线ID:", route.Key));

                        if (route.Value.Providers.Any())
                        {
                            detailPanel.Children.Add(
                                new TextBlock
                                {
                                    Text = "提供者:",
                                    FontWeight = FontWeights.Bold,
                                    Foreground = MediaBrushes.White
                                });

                            foreach (var provider in route.Value.Providers)
                            {
                                detailPanel.Children.Add(
                                    new TextBlock
                                    {
                                        Text = $"  • {provider}",
                                        Foreground = MediaBrushes.White,
                                        Margin = new Thickness(15, 0, 0, 0)
                                    });
                            }
                        }
                    }
                }
                else
                {
                    detailPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "未找到匹配的线路信息",
                            Foreground = MediaBrushes.Gray,
                            FontStyle = FontStyles.Italic,
                            Margin = new Thickness(0, 10, 0, 0)
                        });

                    var suggestions = _routesCache
                        .Where(r => r.Value.DisplayName.IndexOf(folderName, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Take(3)
                        .ToList();

                    if (suggestions.Any())
                    {
                        detailPanel.Children.Add(
                            new TextBlock
                            {
                                Text = "可能相关的线路:",
                                FontWeight = FontWeights.Bold,
                                Foreground = MediaBrushes.White,
                                Margin = new Thickness(0, 10, 0, 0)
                            });

                        foreach (var suggestion in suggestions)
                        {
                            detailPanel.Children.Add(
                                new TextBlock
                                {
                                    Text = $"  • {suggestion.Value.DisplayName} (ID: {suggestion.Key})",
                                    Foreground = MediaBrushes.White,
                                    Margin = new Thickness(15, 0, 0, 0)
                                });
                        }
                    }
                }
            });
        }
        private void AddComment_Click(object sender, RoutedEventArgs e)
        {
            if (_rightClickedItem == null || !_rightClickedItem.IsDirectory)
            {
                MessageBox.Show("请先选择文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var config = AppConfig.Load("config.json");
            string currentComment = config.GetFolderComment(_rightClickedItem.Path);
            var dialog = new CommentInputDialog
            {
                Owner = Window.GetWindow(this),
                CommentText = currentComment ?? ""
            };

            if (dialog.ShowDialog() == true)
            {
                config.SetFolderComment(_rightClickedItem.Path, dialog.CommentText);
                config.Save("config.json");
                UpdateDetailPanel(_rightClickedItem);

                MessageBox.Show("注释已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem is FileSystemItem selectedItem)
            {
                UpdateDetailPanel(selectedItem);
            }
            else
            {
                UpdateDetailPanel(null);
            }
        }

        private UIElement CreateDetailRow(string label, string value)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0),
                Children =
        {
            new TextBlock {
                Text = label,
                Foreground = MediaBrushes.LightGray,
                Width = 80
            },
            new TextBlock {
                Text = value,
                Foreground = MediaBrushes.White,
                TextWrapping = TextWrapping.Wrap
            }
        }
            };
        }
        private void AssetsBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            var config = AppConfig.Load("config.json");
            if (!string.IsNullOrEmpty(config?.GameDirectory))
            {
                _assetsPath = Path.Combine(config.GameDirectory, "Assets");
                if (Directory.Exists(_assetsPath))
                {
                    Task.Run(() =>
                    {
                        LoadAllRoutesProperties();
                        Dispatcher.Invoke(() => NavigateTo(_assetsPath));
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show($"Assets目录不存在: {_assetsPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private static System.Windows.Controls.ListViewItem FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is System.Windows.Controls.ListViewItem parent) return parent;
            return FindParent<System.Windows.Controls.ListViewItem>(parentObject);
        }

        private void ListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hitTestResult = VisualTreeHelper.HitTest(listView, e.GetPosition(listView));
            if (hitTestResult == null) return;
            var listViewItem = FindParent<ListViewItem>(hitTestResult.VisualHit);
            if (listViewItem == null) return;

            _rightClickedItem = listViewItem.DataContext as FileSystemItem;
            if (_rightClickedItem == null) return;

            if (_rightClickedItem.IsDirectory)
            {
                listViewItem.IsSelected = true;

                menuAddComment.Visibility = Visibility.Visible;

                e.Handled = true;

                Debug.WriteLine($"右键选中: {_rightClickedItem.Name}");
                Debug.WriteLine($"路径: {_rightClickedItem.Path}");
            }
            else
            {
                menuAddComment.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }
        private void CopyFolderPath_Click(object sender, RoutedEventArgs e)
        {
            if (_rightClickedItem == null || !_rightClickedItem.IsDirectory)
            {
                MessageBox.Show("请先选择一个文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Clipboard.SetText(_rightClickedItem.Path);
                MessageBox.Show($"已复制路径: {_rightClickedItem.Path}",
                               "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (_rightClickedItem == null || !_rightClickedItem.IsDirectory)
            {
                MessageBox.Show("请先选择一个文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!Directory.Exists(_rightClickedItem.Path))
            {
                MessageBox.Show("目录不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{_rightClickedItem.Path}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(_currentDirectory);
        }
        private void NavigateTo(string path)
        {
            if (!Directory.Exists(path) && path != "..")
            {
                System.Windows.MessageBox.Show($"目录不存在: {path}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _currentDirectory = path;
            _currentPage = 1;
            _allItems.Clear();
            listView.ItemsSource = null;
            txtPageInfo.Text = "加载中...";

            Task.Run(async () =>
            {
                try
                {
                    await LoadDirectoryContentsAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        System.Windows.MessageBox.Show($"加载目录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }
        private async Task LoadDirectoryContentsAsync(CancellationToken cancellationToken)
        {
            var tempItems = new List<FileSystemItem>();

            await Dispatcher.InvokeAsync(() =>
            {
                if (_currentDirectory != _assetsPath && Directory.GetParent(_currentDirectory) != null)
                {
                    tempItems.Add(new FileSystemItem
                    {
                        Name = "..",
                        Path = Directory.GetParent(_currentDirectory).FullName,
                        IsDirectory = true,
                        Size = "",
                        Icon = GetFolderIcon()
                    });
                }
            });

            var dirs = await Task.Run(() =>
            {
                try { return Directory.GetDirectories(_currentDirectory); }
                catch { return Array.Empty<string>(); }
            });

            var files = await Task.Run(() =>
            {
                try { return Directory.GetFiles(_currentDirectory); }
                catch { return Array.Empty<string>(); }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var dir in dirs)
                {
                    tempItems.Add(new FileSystemItem
                    {
                        Name = Path.GetFileName(dir),
                        Path = dir,
                        IsDirectory = true,
                        Size = "计算中...",
                        Icon = GetFolderIcon()
                    });
                }

                foreach (var file in files)
                {
                    tempItems.Add(new FileSystemItem
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        IsDirectory = false,
                        Size = "计算中...",
                        Icon = GetFileIcon(file)
                    });
                }

                _allItems = tempItems;
                UpdateListView();
            });

            var updateTasks = new List<Task>();

            foreach (var dir in dirs)
            {
                updateTasks.Add(UpdateDirectorySizeAsync(dir, tempItems, cancellationToken));
            }

            foreach (var file in files)
            {
                updateTasks.Add(UpdateFileSizeAsync(file, tempItems, cancellationToken));
            }

            await Task.WhenAll(updateTasks);
        }
        private async Task UpdateDirectorySizeAsync(string dirPath, List<FileSystemItem> items, CancellationToken cancellationToken)
        {
            long size = await Task.Run(() => CalculateFolderSize(dirPath, cancellationToken));

            if (!cancellationToken.IsCancellationRequested)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var item = items.FirstOrDefault(i => i.Path == dirPath);
                    if (item != null)
                    {
                        item.Size = FormatSize(size);
                        UpdateSingleItem(item);
                    }
                });
            }
        }

        private async Task UpdateFileSizeAsync(string filePath, List<FileSystemItem> items, CancellationToken cancellationToken)
        {
            long size = await Task.Run(() =>
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length;
                }
                catch
                {
                    return 0;
                }
            });

            if (!cancellationToken.IsCancellationRequested)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var item = items.FirstOrDefault(i => i.Path == filePath);
                    if (item != null)
                    {
                        item.Size = FormatSize(size);
                        UpdateSingleItem(item);
                    }
                });
            }
        }

        private void UpdateSingleItem(FileSystemItem item)
        {
            if (listView.ItemsSource is ICollectionView view)
            {
                view.Refresh();
            }
        }
        private ImageSource GetFolderIcon()
        {
            try
            {
                var shinfo = new SHFILEINFO();
                IntPtr hImg = Win32.SHGetFileInfo(
                    "dummy",
                    Win32.FILE_ATTRIBUTE_DIRECTORY,
                    out shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON | Win32.SHGFI_USEFILEATTRIBUTES);

                if (hImg != IntPtr.Zero)
                {
                    using (var icon = Icon.FromHandle(shinfo.hIcon))
                    {
                        return icon.ToImageSource();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        private ImageSource GetFileIcon(string filePath)
        {
            try
            {
                var icon = Icon.ExtractAssociatedIcon(filePath);
                return icon.ToImageSource();
            }
            catch
            {
                return GetFolderIcon();
            }
        }
        private void UpdateListView()
        {
            listView.ItemsSource = null;
            listView.Items.Clear();

            var filteredItems = _allItems
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            listView.ItemsSource = filteredItems;
            UpdatePageInfo();
        }

        private void UpdatePageInfo()
        {
            int totalPages = (int)Math.Ceiling((double)_allItems.Count / PageSize);
            txtPageInfo.Text = $"第 {_currentPage} 页 / 共 {totalPages} 页";
        }

        private string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
            {
                return $"{(bytes / (double)GB):0.##} GB";
            }
            else if (bytes >= MB)
            {
                return $"{(bytes / (double)MB):0.##} MB";
            }
            else if (bytes >= KB)
            {
                return $"{(bytes / (double)KB):0.##} KB";
            }
            else
            {
                return $"{bytes} B";
            }
        }

        private void btnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateListView();
            }
        }

        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_allItems.Count / PageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdateListView();
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (listView.SelectedItem is FileSystemItem selectedItem && selectedItem.IsDirectory)
            {
                NavigateTo(selectedItem.Path);
            }
        }

        private long CalculateFolderSize(string folderPath, CancellationToken cancellationToken)
        {
            long size = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return 0;

                    try
                    {
                        var fileInfo = new FileInfo(file);
                        size += fileInfo.Length;
                    }
                    catch {}
                }
            }
            catch {}
            return size;
        }
    }

    public class FileSystemItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public string Size { get; set; }
        public ImageSource Icon { get; set; }
    }
}
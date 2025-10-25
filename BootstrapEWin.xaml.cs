using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using Path = System.IO.Path;

namespace RW_Explorer
{
    public partial class MainWindow : Window
    {
        private const string ConfigFileName = "config.json";
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            this.MinHeight = 310;
            this.MinWidth = 320;
            LoadConfig();
            UpdateRunButtonState();
        }

        private void LoadConfig()
        {
            _config = AppConfig.Load(ConfigFileName);
            txtGameDirectory.Text = _config.GameDirectory ?? "未选择目录";
        }

        private void UpdateRunButtonState()
        {
            RunButton.IsEnabled = !string.IsNullOrEmpty(_config?.GameDirectory) &&
                                File.Exists(Path.Combine(_config.GameDirectory, "RailWorks.exe"));
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "选择模拟列车游戏运行路径",
                IsFolderPicker = true,
                EnsurePathExists = true,
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dialog.FileName;
                string exePath = Path.Combine(selectedPath, "RailWorks.exe");

                if (File.Exists(exePath))
                {
                    _config.GameDirectory = selectedPath;
                    _config.Save(ConfigFileName);
                    txtGameDirectory.Text = selectedPath;
                    MessageBox.Show("目录设置成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无效的路径。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                UpdateRunButtonState();
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var homeWindow = new Windows.HomeWindow();
            this.Hide();
            homeWindow.Closed += (s, args) => this.Show();
            homeWindow.Show();
        }
    }
}
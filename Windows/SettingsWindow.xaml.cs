using RW_Explorer.Class;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace RW_Explorer.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            this.Opacity = 0;
            this.Loaded += Window_Loaded;

        }
        private void LoadVersionInfo()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            txtVersion.Text = $"{version.Major}.{version.Minor}.{version.Build}";
            txtUpdateStatus.Text = $"当前版本: {version.Major}.{version.Minor}.{version.Build}";
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var openAnimation = (Storyboard)Resources["WindowOpenAnimation"];
            openAnimation.Begin(this);
        }

        private async void btnClose_Click(object sender, RoutedEventArgs e)
        {
            await CloseWithAnimation();
        }

        private async void btnOK_Click(object sender, RoutedEventArgs e)
        {
            await CloseWithAnimation();
        }

        private async Task CloseWithAnimation()
        {
            var closeAnimation = (Storyboard)Resources["WindowCloseAnimation"];
            closeAnimation.Begin(this);

            await Task.Delay(150);
            this.Close();
        }
    }
}
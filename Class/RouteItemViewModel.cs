using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RW_Explorer.Class
{
    public class RouteItemViewModel
    {
        public string DisplayName { get; set; }
        public string FolderPath { get; set; }
        public string FolderName => Path.GetFileName(FolderPath);
        public string Providers { get; set; }
        public ImageSource ImageSource { get; set; }

        public RouteItemViewModel(RouteProperties props)
        {
            DisplayName = props.DisplayName;
            FolderPath = props.FolderPath;
            Providers = string.Join(", ", props.Providers);

            if (!string.IsNullOrEmpty(props.ImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(props.ImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImageSource = bitmap;
                }
                catch
                {
                    ImageSource = null;
                }
            }
        }
    }
}
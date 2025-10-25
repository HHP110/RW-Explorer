using System.Windows;

namespace RW_Explorer.Windows
{
    public partial class CommentInputDialog : Window
    {
        public static readonly DependencyProperty CommentTextProperty =
            DependencyProperty.Register("CommentText", typeof(string), typeof(CommentInputDialog),
                new PropertyMetadata(string.Empty));

        public string CommentText
        {
            get { return (string)GetValue(CommentTextProperty); }
            set { SetValue(CommentTextProperty, value); }
        }

        public CommentInputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
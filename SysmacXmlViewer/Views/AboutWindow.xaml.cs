using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;
using SysmacXmlViewer.ViewModels;

namespace SysmacXmlViewer.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            try
            {
                InitializeComponent();
                DataContext = new AboutViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize About window: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // デフォルトブラウザでURLを開く
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

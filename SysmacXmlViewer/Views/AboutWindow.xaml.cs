using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using SysmacXmlViewer.ViewModels;

namespace SysmacXmlViewer.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            DataContext = new AboutViewModel();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

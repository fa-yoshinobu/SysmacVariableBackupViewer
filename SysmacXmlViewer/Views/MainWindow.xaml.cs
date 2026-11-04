using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SysmacXmlViewer.ViewModels;
using System.Windows.Media;

namespace SysmacXmlViewer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel();
        }

        private void VariableGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm && sender is DataGrid grid
                && grid.CurrentCell.Item is Models.VariableItem item)
                vm.SelectedVariable = item;
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow();
                aboutWindow.Owner = this;
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open About window: {SysmacXmlViewer.Services.ErrorMessages.Describe(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
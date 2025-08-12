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

        private void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+C で選択されたセルの内容をクリップボードにコピー
                if (sender is DataGrid dataGrid)
                {
                    // 選択されたセルがある場合
                    if (dataGrid.CurrentCell.Item != null)
                    {
                        var cellContent = dataGrid.CurrentCell.Column.GetCellContent(dataGrid.CurrentCell.Item);
                        if (cellContent is TextBlock textBlock)
                        {
                            Clipboard.SetText(textBlock.Text);
                        }
                        else if (cellContent is TextBox textBox)
                        {
                            Clipboard.SetText(textBox.Text);
                        }
                        else if (cellContent is ContentPresenter contentPresenter)
                        {
                            // ContentPresenterの場合は子要素を探す
                            var childTextBlock = FindVisualChild<TextBlock>(contentPresenter);
                            if (childTextBlock != null)
                            {
                                Clipboard.SetText(childTextBlock.Text);
                            }
                        }
                    }
                    // 選択された行がある場合
                    else if (dataGrid.SelectedItem != null)
                    {
                        var selectedItem = dataGrid.SelectedItem as Models.VariableItem;
                        if (selectedItem != null)
                        {
                            // 選択された行の全データをコピー
                            string rowData = $"{selectedItem.DisplayName}\t{selectedItem.DataTypeDisplayName}\t{selectedItem.Value}\t{selectedItem.ConvertedValueDisplay}\t{selectedItem.Offset}\t{selectedItem.GroupName}\t{selectedItem.ArrayIndex}";
                            Clipboard.SetText(rowData);
                        }
                    }
                    e.Handled = true;
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
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
                MessageBox.Show($"Failed to open About window: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 
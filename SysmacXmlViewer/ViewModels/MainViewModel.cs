using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Win32;
using SysmacXmlViewer.Commands;
using SysmacXmlViewer.Models;
using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly XmlParser _xmlParser;
        private readonly CsvExporter _csvExporter;
        private readonly XmlWriter _xmlWriter;
        
        private ObservableCollection<VariableItem> _filteredVariables = new();
        private string _filterText = string.Empty;
        private string _selectedDataType = "All";
        private string _currentFilePath = string.Empty;
        private ProjectInfo _projectInfo = new();
        private List<VariableItem> _allVariables = new();
        
        // 高速化のためのキャッシュ
        private Dictionary<string, string> _dataTypeCache = new();
        private List<VariableItem> _cachedFilteredList = new();
        private readonly object _filterLock = new();
        private bool _pendingFilterRequest = false;
        private bool _isFiltering = false;

        public MainViewModel()
        {
            _xmlParser = new XmlParser();
            _csvExporter = new CsvExporter();
            _xmlWriter = new XmlWriter();
            
            LoadFileCommand = new RelayCommand(LoadFile);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            
            AvailableDataTypes = new ObservableCollection<string>();
            AvailableDataTypes.Add("All");
        }

        public ObservableCollection<VariableItem> FilteredVariables
        {
            get => _filteredVariables;
            set => SetProperty(ref _filteredVariables, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    // 非同期でフィルタリングを実行
                    _ = ApplyFiltersAsync();
                }
            }
        }

        public string SelectedDataType
        {
            get => _selectedDataType;
            set
            {
                if (SetProperty(ref _selectedDataType, value))
                {
                    // 非同期でフィルタリングを実行
                    _ = ApplyFiltersAsync();
                }
            }
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        public ProjectInfo ProjectInfo
        {
            get => _projectInfo;
            set => SetProperty(ref _projectInfo, value);
        }

        public ObservableCollection<string> AvailableDataTypes { get; }

        public int TotalVariables => _allVariables.Count;
        public int FilteredVariablesCount => FilteredVariables.Count;

        public ICommand LoadFileCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        private async void LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                Title = "Select Sysmac Studio XML File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // メモリクリア
                    ClearMemory();
                    
                    // 非同期でファイル読み込み
                    var data = await Task.Run(() => _xmlParser.ParseXmlFile(openFileDialog.FileName));
                    
                    _allVariables = data.Variables;
                    ProjectInfo = data.ProjectInfo;
                    CurrentFilePath = openFileDialog.FileName;

                    // データ型リストを更新（STRING[]型を統一）
                    await UpdateAvailableDataTypesAsync(data);

                    await ApplyFiltersAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load file: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ClearMemory()
        {
            // キャッシュをクリア
            VariableItem.ClearCache();
            Services.DataTypeConverter.ClearCache();
            
            // コレクションをクリア
            _allVariables.Clear();
            FilteredVariables.Clear();
            AvailableDataTypes.Clear();
            _dataTypeCache.Clear();
            _cachedFilteredList.Clear();
            
            lock (_filterLock)
            {
                _pendingFilterRequest = false;
                _isFiltering = false;
            }
            
            // ガベージコレクションを強制実行
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private async Task UpdateAvailableDataTypesAsync(XmlBackupData data)
        {
            var dataTypes = await Task.Run(() =>
                data.VariablesByType.Keys
                    .Select(NormalizeDataType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            );

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableDataTypes.Clear();
                AvailableDataTypes.Add("All");
                foreach (var dt in dataTypes)
                {
                    AvailableDataTypes.Add(dt);
                }
            });
        }

        private string NormalizeDataType(string dataType)
        {
            // キャッシュを使用して高速化
            if (_dataTypeCache.TryGetValue(dataType, out string? cachedResult))
            {
                return cachedResult;
            }

            string result;
            // STRING[]型をすべてSTRINGに統一
            if (dataType.StartsWith("STRING[") && dataType.EndsWith("]"))
            {
                result = "STRING";
            }
            else
            {
                result = dataType;
            }

            _dataTypeCache[dataType] = result;
            return result;
        }

        private void ExportCsv()
        {
            if (_allVariables.Count == 0)
            {
                System.Windows.MessageBox.Show("No data to export.", "Warning", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select CSV File Save Location",
                FileName = $"SysmacVariables_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _csvExporter.ExportToCsv(FilteredVariables.ToList(), saveFileDialog.FileName);
                    System.Windows.MessageBox.Show("CSV file export completed.", "Complete", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to export CSV file: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ClearFilters()
        {
            FilterText = string.Empty;
            SelectedDataType = "All";
        }

        private async Task ApplyFiltersAsync()
        {
            lock (_filterLock)
            {
                if (_isFiltering)
                {
                    _pendingFilterRequest = true;
                    return;
                }

                _isFiltering = true;
            }

            while (true)
            {
                List<VariableItem> filtered;

                try
                {
                    filtered = await Task.Run(() => ApplyFiltersInternal());
                }
                catch
                {
                    lock (_filterLock)
                    {
                        _isFiltering = false;
                        _pendingFilterRequest = false;
                    }
                    throw;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FilteredVariables.Clear();
                    foreach (var variable in filtered)
                    {
                        FilteredVariables.Add(variable);
                    }

                    OnPropertyChanged(nameof(TotalVariables));
                    OnPropertyChanged(nameof(FilteredVariablesCount));
                });

                bool shouldRepeat;
                lock (_filterLock)
                {
                    if (_pendingFilterRequest)
                    {
                        _pendingFilterRequest = false;
                        shouldRepeat = true;
                    }
                    else
                    {
                        _isFiltering = false;
                        shouldRepeat = false;
                    }
                }

                if (!shouldRepeat)
                {
                    break;
                }
            }
        }

        private List<VariableItem> ApplyFiltersInternal()
        {
            var filtered = _allVariables.AsEnumerable();

            // 検索テキストでフィルタ（大文字小文字を区別しない比較を最適化）
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var filterLower = FilterText.ToLowerInvariant();
                filtered = filtered.Where(v => 
                    v.Name.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ||
                    v.DataType.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ||
                    v.Value.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ||
                    v.DisplayName.Contains(filterLower, StringComparison.OrdinalIgnoreCase));
            }

            // データ型でフィルタ（STRING[]型を統一）
            if (!string.IsNullOrWhiteSpace(SelectedDataType) && SelectedDataType != "All")
            {
                filtered = filtered.Where(v => NormalizeDataType(v.DataType) == SelectedDataType);
            }

            var result = filtered.ToList();
            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 

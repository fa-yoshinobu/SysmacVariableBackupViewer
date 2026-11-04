using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
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
        private bool _isLoading;
        private string _metadataStatus = "Load a backup, then load its SMC2 project for POU names and comments.";
        public string MetadataStatus
        {
            get => _metadataStatus;
            private set => SetProperty(ref _metadataStatus, value);
        }

        public sealed record PouFilter(string Key, string Label);
        public ObservableCollection<PouFilter> AvailablePous { get; } = new() { new("", "All") };
        private PouFilter? _selectedPou;
        public PouFilter? SelectedPou
        {
            get => _selectedPou;
            set { if (SetProperty(ref _selectedPou, value)) _ = ApplyFiltersAsync(); }
        }
        private VariableItem? _selectedVariable;
        public VariableItem? SelectedVariable
        {
            get => _selectedVariable;
            set => SetProperty(ref _selectedVariable, value);
        }
        private string _currentProjectPath = "";
        public string CurrentProjectPath
        {
            get => _currentProjectPath;
            private set => SetProperty(ref _currentProjectPath, value);
        }
        private bool _isFiltering;
        private string _operation = "Ready";
        public bool IsBusy => _isLoading || _isFiltering;
        public string ActivityStatus => _isLoading ? _operation : _isFiltering ? "Filtering variables..." : "Ready";
        private void NotifyActivity()
        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(ActivityStatus));
            CommandManager.InvalidateRequerySuggested();
        }
        private void UpdatePous()
        {
            var key = SelectedPou?.Key;
            AvailablePous.Clear();
            AvailablePous.Add(new("", "All"));
            foreach (var group in _allVariables.GroupBy(v => v.ScopeKey).OrderBy(g => g.First().ScopeDisplay))
                AvailablePous.Add(new(group.Key, group.Key == "Global" ? "Global"
                    : group.First().PouName.Length > 0 ? group.First().PouName : group.Key));
            SelectedPou = AvailablePous.FirstOrDefault(p => p.Key == key) ?? AvailablePous[0];
        }

        // 高速化のためのキャッシュ
        private readonly ConcurrentDictionary<string, string> _dataTypeCache = new();
        private CancellationTokenSource? _filterCancellation;
        private int _filterVersion;

        public MainViewModel()
        {
            _xmlParser = new XmlParser();
            _csvExporter = new CsvExporter();
            _xmlWriter = new XmlWriter();

            LoadFileCommand = new RelayCommand(LoadFile, () => !_isLoading);
            LoadProjectCommand = new RelayCommand(LoadProject, () => !_isLoading && _allVariables.Count > 0);
            ExportCsvCommand = new RelayCommand(ExportCsv, () => !IsBusy && FilteredVariables.Count > 0);
            _selectedPou = AvailablePous[0];
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
        public ICommand LoadProjectCommand { get; }
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
                    _isLoading = true;
                    _operation = "Loading backup XML...";
                    NotifyActivity();

                    // 非同期でファイル読み込み
                    var data = await Task.Run(() => _xmlParser.ParseXmlFile(openFileDialog.FileName));

                    ClearMemory();
                    MetadataStatus = "Load the matching SMC2 project for POU names and comments.";
                    _allVariables = data.Variables;
                    ProjectInfo = data.ProjectInfo;
                    CurrentFilePath = openFileDialog.FileName;
                    CurrentProjectPath = "";
                    UpdatePous();

                    // データ型リストを更新（STRING[]型を統一）
                    await UpdateAvailableDataTypesAsync(data);

                    await ApplyFiltersAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load file: {SysmacXmlViewer.Services.ErrorMessages.Describe(ex)}", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    _isLoading = false;
                    NotifyActivity();
                }
            }
        }

        private async void LoadProject()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Sysmac Studio projects (*.smc2)|*.smc2",
                Title = "Select matching Sysmac Studio project"
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                _isLoading = true;
                _operation = "Loading project and matching names & comments...";
                NotifyActivity();
                var original = _allVariables;
                var tracking = ProjectInfo.Tracking;
                var mapped = await Task.Run(() =>
                {
                    var metadata = SmcProjectMetadata.Load(dialog.FileName, tracking);
                    var copy = original.Select(v => new VariableItem
                    {
                        Name = v.Name,
                        DataType = v.DataType,
                        Offset = v.Offset,
                        Value = v.Value
                    }).ToList();
                    metadata.Apply(copy);
                    return copy;
                });
                _allVariables = mapped;
                CurrentProjectPath = dialog.FileName;
                UpdatePous();
                var local = _allVariables.Count(v => v.IsLocalVariable);
                var resolved = _allVariables.Count(v => v.PouName.Length > 0);
                var comments = _allVariables.Count(v => v.Comment.Length > 0);
                MetadataStatus = $"{Path.GetFileName(dialog.FileName)} | POU resolved: {resolved}/{local} | Comments: {comments}/{_allVariables.Count}";
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load project: {SysmacXmlViewer.Services.ErrorMessages.Describe(ex)}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
                NotifyActivity();
            }
        }

        private void ClearMemory()
        {
            // キャッシュをクリア
            VariableItem.ClearCache();
            Services.DataTypeConverter.ClearCache();

            // コレクションをクリア
            _filterCancellation?.Cancel();
            _filterVersion++;
            _allVariables = new();
            FilteredVariables.Clear();
            SelectedVariable = null;
            AvailableDataTypes.Clear();
            _dataTypeCache.Clear();

        }

        private Task UpdateAvailableDataTypesAsync(XmlBackupData data)
        {
            var selected = SelectedDataType;
            AvailableDataTypes.Clear();
            AvailableDataTypes.Add("All");
            foreach (var type in data.VariablesByType.Keys.Select(NormalizeDataType).Distinct().OrderBy(x => x))
                AvailableDataTypes.Add(type);
            SelectedDataType = AvailableDataTypes.Contains(selected) ? selected : "All";
            return Task.CompletedTask;
        }

        private string NormalizeDataType(string dataType)
        {
            return _dataTypeCache.GetOrAdd(
                dataType,
                static key => key.StartsWith("STRING[") && key.EndsWith(']') ? "STRING" : key);
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
                    System.Windows.MessageBox.Show($"Failed to export CSV file: {SysmacXmlViewer.Services.ErrorMessages.Describe(ex)}", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ClearFilters()
        {
            FilterText = string.Empty;
            SelectedDataType = "All";
            SelectedPou = AvailablePous[0];
        }

        private async Task ApplyFiltersAsync()
        {
            _filterCancellation?.Cancel();
            using var cancellation = new CancellationTokenSource();
            _filterCancellation = cancellation;
            var version = ++_filterVersion;
            // Lists are replaced on load, never cleared while a worker is reading them.
            var variables = _allVariables;
            var text = FilterText;
            var dataType = SelectedDataType;
            var pou = SelectedPou?.Key ?? "";
            _isFiltering = true;
            NotifyActivity();
            try
            {
                var filtered = await Task.Run(() => ApplyFiltersInternal(variables, text, dataType, pou, cancellation.Token));
                if (version != _filterVersion || cancellation.IsCancellationRequested) return;
                var selectedName = SelectedVariable?.Name;
                FilteredVariables = new ObservableCollection<VariableItem>(filtered);
                SelectedVariable = filtered.FirstOrDefault(v => v.Name == selectedName);
                OnPropertyChanged(nameof(TotalVariables));
                OnPropertyChanged(nameof(FilteredVariablesCount));
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // A newer filter or backup owns the displayed results.
            }
            finally
            {
                if (ReferenceEquals(_filterCancellation, cancellation))
                {
                    _filterCancellation = null;
                    _isFiltering = false;
                    NotifyActivity();
                }
            }
        }

        private List<VariableItem> ApplyFiltersInternal(List<VariableItem> variables, string text,
            string dataType, string pou, CancellationToken cancellation)
        {
            var result = new List<VariableItem>();
            foreach (var v in variables)
            {
                cancellation.ThrowIfCancellationRequested();
                if (pou.Length > 0 && v.ScopeKey != pou) continue;
                if (!string.IsNullOrWhiteSpace(text) &&
                    !v.Name.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                    !v.DataType.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                    !v.Value.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                    !v.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                    !v.Comment.Contains(text, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(dataType) && dataType != "All" && NormalizeDataType(v.DataType) != dataType) continue;
                result.Add(v);
            }
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

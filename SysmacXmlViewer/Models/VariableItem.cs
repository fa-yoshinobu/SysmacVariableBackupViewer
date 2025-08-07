using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace SysmacXmlViewer.Models
{
    public class VariableItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _dataType = string.Empty;
        private string _offset = string.Empty;
        private string _value = string.Empty;
        private string _displayName = string.Empty;
        private string _groupName = string.Empty;

        // 高速化のためのキャッシュ
        private static readonly ConcurrentDictionary<string, string> _convertedValueCache = new();
        private static readonly ConcurrentDictionary<string, bool> _isStringArrayCache = new();
        private static readonly ConcurrentDictionary<string, string> _arrayIndexCache = new();
        private static readonly ConcurrentDictionary<string, string> _hierarchicalPathCache = new();
        private static readonly ConcurrentDictionary<string, int?> _stringArrayCharLimitCache = new();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        public string Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        // 配列型変数かどうか
        public bool IsArray => Name.Contains("[") || IsStringArray;

        // 階層型変数かどうか
        public bool IsHierarchical => Name.Contains("/");

        // STRING[]型の配列かどうか
        public bool IsStringArray
        {
            get
            {
                // キャッシュから取得を試行
                if (_isStringArrayCache.TryGetValue(DataType, out bool cachedResult))
                {
                    return cachedResult;
                }

                bool result = DataType.StartsWith("STRING[") && DataType.EndsWith("]");
                _isStringArrayCache.TryAdd(DataType, result);
                return result;
            }
        }

        // 配列インデックスを取得
        public string ArrayIndex
        {
            get
            {
                // キャッシュから取得を試行
                string cacheKey = $"{DataType}_{Name}";
                if (_arrayIndexCache.TryGetValue(cacheKey, out string? cachedResult))
                {
                    return cachedResult;
                }

                string result;
                if (IsStringArray)
                {
                    var match = Regex.Match(DataType, @"STRING\[(\d+)\]");
                    result = match.Success ? match.Groups[1].Value : string.Empty;
                }
                else
                {
                    var arrayMatch = Regex.Match(Name, @"\[(\d+)\]");
                    result = arrayMatch.Success ? arrayMatch.Groups[1].Value : string.Empty;
                }

                _arrayIndexCache.TryAdd(cacheKey, result);
                return result;
            }
        }

        // 階層パスを取得
        public string HierarchicalPath
        {
            get
            {
                // キャッシュから取得を試行
                if (_hierarchicalPathCache.TryGetValue(Name, out string? cachedResult))
                {
                    return cachedResult;
                }

                var parts = Name.Split('/');
                string result = parts.Length > 1 ? string.Join("/", parts.Skip(1)) : Name;
                
                _hierarchicalPathCache.TryAdd(Name, result);
                return result;
            }
        }

        // STRING[]型の文字数制限を取得
        public int? StringArrayCharLimit
        {
            get
            {
                if (!IsStringArray) return null;
                
                // キャッシュから取得を試行
                if (_stringArrayCharLimitCache.TryGetValue(DataType, out int? cachedResult))
                {
                    return cachedResult;
                }

                var match = Regex.Match(DataType, @"STRING\[(\d+)\]");
                int? result = match.Success && int.TryParse(match.Groups[1].Value, out int charCount) ? charCount : null;
                
                _stringArrayCharLimitCache.TryAdd(DataType, result);
                return result;
            }
        }

        // データ型の表示名を取得
        public string DataTypeDisplayName
        {
            get
            {
                // STRING[]型をすべてSTRINGに統一
                if (IsStringArray)
                {
                    return "STRING";
                }
                return DataType;
            }
        }

        // 値の表示形式を取得（XMLから読み込んだ元の値）
        public string ValueDisplay
        {
            get
            {
                // 値の欄にはXMLから読み込んだ元の値をそのまま表示
                return Value;
            }
        }

        // 変換値の表示形式を取得（変換後の値）
        public string ConvertedValueDisplay
        {
            get
            {
                // キャッシュキーを作成
                string cacheKey = $"{DataType}_{Value}";
                
                // キャッシュから取得を試行
                if (_convertedValueCache.TryGetValue(cacheKey, out string? cachedResult))
                {
                    return cachedResult;
                }

                string result;
                if (IsStringArray)
                {
                    // STRING[]型の場合、16進数データを文字列に変換
                    if (IsHexString(Value))
                    {
                        // 16進数データの場合、変換後の文字列を表示
                        string convertedString = ConvertHexToString(Value);
                        result = convertedString;
                    }
                    else
                    {
                        // 16進数でない場合は元の値を表示
                        result = Value;
                    }
                }
                else if (DataType.ToUpperInvariant() == "DATE_AND_TIME")
                {
                    // DATE_AND_TIME型の場合、日時形式に変換
                    result = Services.DataTypeConverter.ConvertDateAndTimeToString(Value);
                }
                else if (DataType.ToUpperInvariant() == "TIME")
                {
                    // TIME型の場合、ミリ秒形式に変換
                    result = Services.DataTypeConverter.ConvertTimeToString(Value);
                }
                else if (DataType.ToUpperInvariant() == "DATE")
                {
                    // DATE型の場合、日付形式に変換
                    result = Services.DataTypeConverter.ConvertDateToString(Value);
                }
                else if (DataType.ToUpperInvariant() == "TIME_OF_DAY")
                {
                    // TIME_OF_DAY型の場合、時刻形式に変換
                    result = Services.DataTypeConverter.ConvertTimeOfDayToString(Value);
                }
                else if (DataType.ToUpperInvariant() == "REAL")
                {
                    // REAL型の場合、10進表記に変換
                    result = Services.DataTypeConverter.ConvertRealToDecimal(Value);
                }
                else if (DataType.ToUpperInvariant() == "LREAL")
                {
                    // LREAL型の場合、10進表記に変換
                    result = Services.DataTypeConverter.ConvertLRealToDecimal(Value);
                }
                else if (DataType.ToUpperInvariant() == "STRING")
                {
                    // STRING型の場合、16進数文字列をUTF-8文字列に変換
                    result = Services.DataTypeConverter.ConvertStringArrayToString(DataType, Value);
                }
                else
                {
                    // その他の型は元の値を表示
                    result = Value;
                }

                // キャッシュに保存
                _convertedValueCache.TryAdd(cacheKey, result);
                return result;
            }
        }

        // 16進数文字列かどうかを判定
        private bool IsHexString(string value)
        {
            return !string.IsNullOrEmpty(value) && 
                   value.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
        }

        // 16進数文字列を文字列に変換
        private string ConvertHexToString(string hexString)
        {
            try
            {
                if (hexString.Length % 2 != 0)
                {
                    hexString = "0" + hexString;
                }

                byte[] bytes = new byte[hexString.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return hexString;
            }
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

        // キャッシュをクリアするメソッド（メモリ管理用）
        public static void ClearCache()
        {
            _convertedValueCache.Clear();
            _isStringArrayCache.Clear();
            _arrayIndexCache.Clear();
            _hierarchicalPathCache.Clear();
            _stringArrayCharLimitCache.Clear();
        }
    }
} 
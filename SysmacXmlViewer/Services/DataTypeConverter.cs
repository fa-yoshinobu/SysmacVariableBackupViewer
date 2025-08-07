using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SysmacXmlViewer.Services
{
    public static class DataTypeConverter
    {
        // 高速化のためのキャッシュ
        private static readonly ConcurrentDictionary<string, string> _conversionCache = new();
        private static readonly ConcurrentDictionary<string, bool> _hexStringCache = new();

        public static string ConvertValueToString(string dataType, string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
                return string.Empty;

            // キャッシュキーを作成
            string cacheKey = $"{dataType}_{rawValue}";
            
            // キャッシュから取得を試行
            if (_conversionCache.TryGetValue(cacheKey, out string? cachedResult))
            {
                return cachedResult;
            }

            try
            {
                string result;

                // STRING[]型の場合は元の値をそのまま返す
                if (dataType.StartsWith("STRING[") && dataType.EndsWith("]"))
                {
                    result = rawValue;
                }
                else
                {
                    switch (dataType.ToUpperInvariant())
                    {
                        case "BOOL":
                            result = bool.TryParse(rawValue, out bool boolValue) ? boolValue.ToString() : rawValue;
                            break;
                        
                        case "WORD":
                        case "UINT":
                            result = ushort.TryParse(rawValue, out ushort wordValue) ? wordValue.ToString() : rawValue;
                            break;
                        
                        case "INT":
                            result = short.TryParse(rawValue, out short intValue) ? intValue.ToString() : rawValue;
                            break;
                        
                        case "REAL":
                            result = ConvertRealToDecimal(rawValue);
                            break;
                        
                        case "LREAL":
                            result = ConvertLRealToDecimal(rawValue);
                            break;
                        
                        case "STRING":
                            // string型の場合は16進数文字列をUTF-8文字列に変換
                            result = ConvertStringArrayToString(dataType, rawValue);
                            break;
                        
                        case "DATE_AND_TIME":
                            result = ConvertDateAndTimeToString(rawValue);
                            break;
                        
                        case "TIME":
                            result = ConvertTimeToString(rawValue);
                            break;
                        
                        case "DATE":
                            result = ConvertDateToString(rawValue);
                            break;
                        
                        case "TIME_OF_DAY":
                            result = ConvertTimeOfDayToString(rawValue);
                            break;
                        
                        default:
                            // 不明なデータ型の場合はそのまま返す
                            result = rawValue;
                            break;
                    }
                }

                // キャッシュに保存
                _conversionCache.TryAdd(cacheKey, result);
                return result;
            }
            catch
            {
                // 変換に失敗した場合は元の値を返す
                _conversionCache.TryAdd(cacheKey, rawValue);
                return rawValue;
            }
        }

        public static string ConvertStringArrayToString(string dataType, string rawValue)
        {
            try
            {
                // 16進数データを文字列に変換
                string convertedString = ConvertHexToString(rawValue);
                
                // 変換後の文字列のみを返す
                return convertedString;
            }
            catch
            {
                return rawValue;
            }
        }

        public static string ConvertDateAndTimeToString(string rawValue)
        {
            try
            {
                // DATE_AND_TIME型はナノ秒単位の値として処理
                if (long.TryParse(rawValue, out long nanoseconds))
                {
                    // ナノ秒を秒に変換
                    long seconds = nanoseconds / 1_000_000_000;
                    
                    // Unix時間（1970年1月1日からの秒数）として解釈
                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
                    
                    return dateTime.ToString("yyyy-MM-dd-HH:mm:ss.ff");
                }
                
                // 変換できない場合は元の値を返す
                return rawValue;
            }
            catch
            {
                return rawValue;
            }
        }

        public static string ConvertTimeToString(string rawValue)
        {
            try
            {
                if (long.TryParse(rawValue, out long nanoseconds))
                {
                    // ナノ秒を秒に変換
                    double totalSeconds = nanoseconds / 1_000_000_000.0;
                    
                    // 時、分、秒、ミリ秒に分解
                    int hours = (int)(totalSeconds / 3600);
                    int minutes = (int)((totalSeconds % 3600) / 60);
                    int seconds = (int)(totalSeconds % 60);
                    int milliseconds = (int)((totalSeconds * 1000) % 1000);
                    
                    // 時間形式で表示（例: 3h29m15s10.000ms）
                    var parts = new List<string>();
                    
                    if (hours > 0)
                        parts.Add($"{hours}h");
                    if (minutes > 0)
                        parts.Add($"{minutes}m");
                    if (seconds > 0)
                        parts.Add($"{seconds}s");
                    parts.Add($"{milliseconds}.000ms");
                    
                    return string.Join("", parts);
                }
                return rawValue;
            }
            catch
            {
                return rawValue;
            }
        }

        public static string ConvertDateToString(string rawValue)
        {
            try
            {
                if (long.TryParse(rawValue, out long nanoseconds))
                {
                    // ナノ秒を秒に変換
                    long seconds = nanoseconds / 1_000_000_000;
                    
                    // Unix時間（1970年1月1日からの秒数）として解釈
                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
                    
                    return dateTime.ToString("yyyy-MM-dd");
                }
                return rawValue;
            }
            catch
            {
                return rawValue;
            }
        }

        public static string ConvertTimeOfDayToString(string rawValue)
        {
            try
            {
                if (long.TryParse(rawValue, out long nanoseconds))
                {
                    // 1日のナノ秒数
                    const long nanosecondsPerDay = 86_400_000_000_000L;
                    
                    // 日を除いたナノ秒数を計算
                    long timeNanoseconds = nanoseconds % nanosecondsPerDay;
                    
                    // ナノ秒を秒に変換
                    double totalSeconds = timeNanoseconds / 1_000_000_000.0;
                    
                    // 時、分、秒、ミリ秒に分解
                    int hours = (int)(totalSeconds / 3600);
                    int minutes = (int)((totalSeconds % 3600) / 60);
                    int seconds = (int)(totalSeconds % 60);
                    int milliseconds = (int)((totalSeconds * 1000) % 1000);
                    
                    return $"{hours:D2}:{minutes:D2}:{seconds:D2}.{milliseconds:D2}";
                }
                return rawValue;
            }
            catch
            {
                return rawValue;
            }
        }

        private static byte[] ConvertHexToBytes(string hexString)
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

            return bytes;
        }

        private static string ConvertHexToString(string hexString)
        {
            try
            {
                // 16進数文字列をバイト配列に変換
                if (hexString.Length % 2 != 0)
                {
                    // 奇数文字の場合は先頭に0を追加
                    hexString = "0" + hexString;
                }

                byte[] bytes = new byte[hexString.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                // バイト配列を文字列に変換（UTF-8エンコーディング）
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // 変換に失敗した場合は元の値を返す
                return hexString;
            }
        }

        private static string ConvertStringToHex(string text)
        {
            try
            {
                // 文字列をバイト配列に変換（UTF-8エンコーディング）
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                
                // バイト配列を16進数文字列に変換
                return BitConverter.ToString(bytes).Replace("-", "");
            }
            catch
            {
                // 変換に失敗した場合は元の値を返す
                return text;
            }
        }

        public static string ConvertStringToDataTypeValue(string dataType, string displayValue)
        {
            if (string.IsNullOrEmpty(displayValue))
                return string.Empty;

            try
            {
                // STRING[]型の配列を処理
                if (dataType.StartsWith("STRING[") && dataType.EndsWith("]"))
                {
                    return ConvertStringToDataTypeValueForStringArray(dataType, displayValue);
                }

                switch (dataType.ToUpperInvariant())
                {
                    case "BOOL":
                        return bool.TryParse(displayValue, out bool boolValue) ? boolValue.ToString() : displayValue;
                    
                    case "WORD":
                    case "UINT":
                        return ushort.TryParse(displayValue, out ushort wordValue) ? wordValue.ToString() : displayValue;
                    
                    case "INT":
                        return short.TryParse(displayValue, out short intValue) ? intValue.ToString() : displayValue;
                    
                    case "REAL":
                        return ConvertRealToDecimal(displayValue);
                    
                    case "LREAL":
                        return ConvertLRealToDecimal(displayValue);
                    
                    case "STRING":
                        // string型の場合はそのまま文字列として返す
                        return displayValue;
                    
                    case "DATE_AND_TIME":
                        return ConvertStringToDateAndTimeValue(displayValue);
                    
                    case "TIME":
                        return ConvertStringToTimeValue(displayValue);
                    
                    case "DATE":
                        return ConvertStringToDateValue(displayValue);
                    
                    case "TIME_OF_DAY":
                        return ConvertStringToTimeOfDayValue(displayValue);
                    
                    default:
                        // 不明なデータ型の場合はそのまま返す
                        return displayValue;
                }
            }
            catch
            {
                // 変換に失敗した場合は元の値を返す
                return displayValue;
            }
        }

        private static string ConvertStringToDateAndTimeValue(string displayValue)
        {
            try
            {
                // YYYY/MM/DD hh:mm:ss 形式の文字列をDATE_AND_TIME形式に変換
                if (DateTime.TryParseExact(displayValue, "yyyy/MM/dd HH:mm:ss", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                {
                    // 基準日時: 2001年1月1日 00:00:00
                    DateTime baseDateTime = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    
                    // 経過秒数を計算
                    TimeSpan timeSpan = dateTime - baseDateTime;
                    long seconds = (long)timeSpan.TotalSeconds;
                    
                    return seconds.ToString();
                }
                
                return displayValue;
            }
            catch
            {
                return displayValue;
            }
        }

        private static string ConvertStringToDataTypeValueForStringArray(string dataType, string displayValue)
        {
            try
            {
                // 文字列を16進数に変換
                return ConvertStringToHex(displayValue);
            }
            catch
            {
                return displayValue;
            }
        }

        private static string ConvertStringToTimeValue(string displayValue)
        {
            try
            {
                // "100.000ms" 形式の文字列をナノ秒に変換
                if (displayValue.EndsWith("ms"))
                {
                    string numberPart = displayValue.Substring(0, displayValue.Length - 2);
                    if (double.TryParse(numberPart, out double milliseconds))
                    {
                        long nanoseconds = (long)(milliseconds * 1_000_000);
                        return nanoseconds.ToString();
                    }
                }
                return displayValue;
            }
            catch
            {
                return displayValue;
            }
        }

        private static string ConvertStringToDateValue(string displayValue)
        {
            try
            {
                // "yyyy-MM-dd" 形式の文字列をナノ秒に変換
                if (DateTime.TryParseExact(displayValue, "yyyy-MM-dd", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateTime))
                {
                    // Unix時間（1970年1月1日からの秒数）に変換
                    DateTimeOffset dateTimeOffset = new DateTimeOffset(parsedDateTime);
                    long seconds = dateTimeOffset.ToUnixTimeSeconds();
                    long nanoseconds = seconds * 1_000_000_000;
                    return nanoseconds.ToString();
                }
                return displayValue;
            }
            catch
            {
                return displayValue;
            }
        }

        private static string ConvertStringToTimeOfDayValue(string displayValue)
        {
            try
            {
                // "HH:mm:ss.ff" 形式の文字列をナノ秒に変換
                if (TimeSpan.TryParseExact(displayValue, @"hh\:mm\:ss\.ff", 
                    CultureInfo.InvariantCulture, out TimeSpan timeSpan))
                {
                    long nanoseconds = (long)(timeSpan.TotalSeconds * 1_000_000_000);
                    return nanoseconds.ToString();
                }
                return displayValue;
            }
            catch
            {
                return displayValue;
            }
        }

        public static bool IsValidValue(string dataType, string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            try
            {
                // STRING[]型の配列を処理
                if (dataType.StartsWith("STRING[") && dataType.EndsWith("]"))
                {
                    return IsValidStringArrayValue(dataType, value);
                }

                switch (dataType.ToUpperInvariant())
                {
                    case "BOOL":
                        return bool.TryParse(value, out _);
                    
                    case "WORD":
                    case "UINT":
                        return ushort.TryParse(value, out _);
                    
                    case "INT":
                        return short.TryParse(value, out _);
                    
                    case "REAL":
                        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                    
                    case "LREAL":
                        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                    
                    case "STRING":
                        // string型は常に有効
                        return true;
                    
                    case "DATE_AND_TIME":
                        return IsValidDateAndTimeValue(value);
                    
                    case "TIME":
                        return IsValidTimeValue(value);
                    
                    case "DATE":
                        return IsValidDateValue(value);
                    
                    case "TIME_OF_DAY":
                        return IsValidTimeOfDayValue(value);
                    
                    default:
                        // 不明なデータ型は有効とする
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidDateAndTimeValue(string value)
        {
            try
            {
                // 数値形式の場合（ナノ秒）
                if (long.TryParse(value, out long nanoseconds))
                {
                    // ナノ秒を秒に変換
                    long seconds = nanoseconds / 1_000_000_000;
                    
                    // Unix時間（1970年1月1日からの秒数）として解釈
                    DateTime parsedDateTime = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
                    
                    // 妥当な範囲かチェック（例: 1970年～2100年）
                    return parsedDateTime.Year >= 1970 && parsedDateTime.Year <= 2100;
                }
                
                // 文字列形式の場合（yyyy-MM-dd-HH:mm:ss.ff）
                if (DateTime.TryParseExact(value, "yyyy-MM-dd-HH:mm:ss.ff", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateAndTime))
                {
                    // 妥当な範囲かチェック（例: 1970年～2100年）
                    return parsedDateAndTime.Year >= 1970 && parsedDateAndTime.Year <= 2100;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidTimeValue(string value)
        {
            try
            {
                // 数値形式の場合（ナノ秒）
                if (long.TryParse(value, out long nanoseconds))
                {
                    // 妥当な範囲かチェック（例: 0～1日分のナノ秒）
                    return nanoseconds >= 0 && nanoseconds <= 86_400_000_000_000L;
                }
                
                // 文字列形式の場合（100.000ms）
                if (value.EndsWith("ms"))
                {
                    string numberPart = value.Substring(0, value.Length - 2);
                    if (double.TryParse(numberPart, out double milliseconds))
                    {
                        return milliseconds >= 0 && milliseconds <= 86400000; // 1日分のミリ秒
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidDateValue(string value)
        {
            try
            {
                // 数値形式の場合（ナノ秒）
                if (long.TryParse(value, out long nanoseconds))
                {
                    // ナノ秒を秒に変換
                    long seconds = nanoseconds / 1_000_000_000;
                    
                    // Unix時間（1970年1月1日からの秒数）として解釈
                    DateTime parsedDate = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
                    
                    // 妥当な範囲かチェック（例: 1970年～2100年）
                    return parsedDate.Year >= 1970 && parsedDate.Year <= 2100;
                }
                
                // 文字列形式の場合（yyyy-MM-dd）
                if (DateTime.TryParseExact(value, "yyyy-MM-dd", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateValue))
                {
                    // 妥当な範囲かチェック（例: 1970年～2100年）
                    return parsedDateValue.Year >= 1970 && parsedDateValue.Year <= 2100;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidTimeOfDayValue(string value)
        {
            try
            {
                // 数値形式の場合（ナノ秒）
                if (long.TryParse(value, out long nanoseconds))
                {
                    // 1日のナノ秒数以内かチェック
                    return nanoseconds >= 0 && nanoseconds < 86_400_000_000_000L;
                }
                
                // 文字列形式の場合（HH:mm:ss.ff）
                if (TimeSpan.TryParseExact(value, @"hh\:mm\:ss\.ff", 
                    CultureInfo.InvariantCulture, out TimeSpan timeSpan))
                {
                    // 1日以内かチェック
                    return timeSpan.TotalHours >= 0 && timeSpan.TotalHours < 24;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidStringArrayValue(string dataType, string value)
        {
            try
            {
                // STRING[数値]の形式から文字数を抽出
                var match = Regex.Match(dataType, @"STRING\[(\d+)\]");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int charCount))
                {
                    // 16進数データの場合、文字数制限をチェック
                    if (IsHexString(value))
                    {
                        // 16進数文字列の場合、文字数制限の半分（バイト数）をチェック
                        return value.Length <= charCount * 2;
                    }
                    else
                    {
                        // 通常の文字列の場合、文字数制限をチェック
                        return value.Length <= charCount;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHexString(string value)
        {
            // キャッシュから取得を試行
            if (_hexStringCache.TryGetValue(value, out bool cachedResult))
            {
                return cachedResult;
            }

            // 16進数文字列かどうかを判定
            bool result = !string.IsNullOrEmpty(value) && 
                         value.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
            
            // キャッシュに保存
            _hexStringCache.TryAdd(value, result);
            return result;
        }

        public static string GetDataTypeDisplayName(string dataType)
        {
            // STRING[]型をすべてSTRINGに統一
            if (dataType.StartsWith("STRING[") && dataType.EndsWith("]"))
            {
                return "STRING";
            }
            return dataType;
        }

        public static string ConvertRealToDecimal(string rawValue)
        {
            try
            {
                // まず、16進数文字列として解析を試行
                if (IsHexString(rawValue))
                {
                    // 16進数文字列を正しい順序に並び替える
                    string correctedHex = rawValue;
                    if (rawValue.Length >= 8)
                    {
                        // 2文字ずつペアにして並び替え
                        correctedHex = rawValue.Substring(6, 2) + rawValue.Substring(4, 2) + 
                                      rawValue.Substring(2, 2) + rawValue.Substring(0, 2);
                    }
                    
                    // 修正された16進数文字列をバイト配列に変換
                    byte[] bytes = ConvertHexToBytes(correctedHex);
                    
                    // ビッグエンディアンからリトルエンディアンに変換
                    if (bytes.Length >= 4 && BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytes);
                    }
                    
                    // 4バイト（32ビット）のREAL型として処理
                    if (bytes.Length >= 4)
                    {
                        // リトルエンディアンで32ビット浮動小数点として解釈
                        float floatValue = BitConverter.ToSingle(bytes, 0);
                        return floatValue.ToString("F6", CultureInfo.InvariantCulture);
                    }
                }
                
                // 16進数でない場合は、通常の数値として解析
                if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float realValue))
                {
                    return realValue.ToString("F6", CultureInfo.InvariantCulture);
                }
                
                return rawValue;
            }
            catch
            {
                return rawValue;
            }
        }

        public static string ConvertLRealToDecimal(string rawValue)
        {
            try
            {
                // まず、16進数文字列として解析を試行
                if (IsHexString(rawValue))
                {
                    // 16進数文字列を正しい順序に並び替える
                    string correctedHex = rawValue;
                    if (rawValue.Length >= 16)
                    {
                        // 2文字ずつペアにして並び替え（8バイト分）
                        correctedHex = rawValue.Substring(14, 2) + rawValue.Substring(12, 2) + 
                                      rawValue.Substring(10, 2) + rawValue.Substring(8, 2) + 
                                      rawValue.Substring(6, 2) + rawValue.Substring(4, 2) + 
                                      rawValue.Substring(2, 2) + rawValue.Substring(0, 2);
                    }
                    
                    // 修正された16進数文字列をバイト配列に変換
                    byte[] bytes = ConvertHexToBytes(correctedHex);
                    
                    // ビッグエンディアンからリトルエンディアンに変換
                    if (bytes.Length >= 8 && BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytes);
                    }
                    
                    // 8バイト（64ビット）のLREAL型として処理
                    if (bytes.Length >= 8)
                    {
                        // リトルエンディアンで64ビット浮動小数点として解釈
                        double doubleValue = BitConverter.ToDouble(bytes, 0);
                        return doubleValue.ToString("F6", CultureInfo.InvariantCulture);
                    }
                }
                
                // 16進数でない場合は、通常の数値として解析
                if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double lrealValue))
                {
                    return lrealValue.ToString("F6", CultureInfo.InvariantCulture);
                }
                
                return rawValue;
            }
            catch
            {
                return rawValue;
            }
        }

        // キャッシュをクリアするメソッド（メモリ管理用）
        public static void ClearCache()
        {
            _conversionCache.Clear();
            _hexStringCache.Clear();
        }
    }
} 
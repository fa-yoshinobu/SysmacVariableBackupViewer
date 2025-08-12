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
                    
                    // Sysmac Studioの基準日時: 1970年1月1日 00:00:00 (Unix時間)
                    DateTime baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    
                    // 基準日時からの経過秒数として解釈
                    DateTime dateTime = baseDateTime.AddSeconds(seconds);
                    
                    // DATE_AND_TIME形式: yyyy-MM-dd-HH:mm:ss.ff
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

        public static string ConvertDateToString(string rawValue)
        {
            try
            {
                if (long.TryParse(rawValue, out long nanoseconds))
                {
                    // ナノ秒を秒に変換
                    long seconds = nanoseconds / 1_000_000_000;
                    
                    // Sysmac Studioの基準日時: 1970年1月1日 00:00:00 (Unix時間)
                    DateTime baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    
                    // 基準日時からの経過秒数として解釈
                    DateTime dateTime = baseDateTime.AddSeconds(seconds);
                    
                    // DATE形式: yyyy-MM-dd
                    return dateTime.ToString("yyyy-MM-dd");
                }
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
                    // 負の値の処理（long.MinValueの特殊ケースを考慮）
                    bool isNegative;
                    long absNanoseconds;
                    
                    if (nanoseconds == long.MinValue)
                    {
                        // long.MinValueの場合は、直接計算で処理
                        absNanoseconds = long.MaxValue;
                        // 1を加算（オーバーフローを避けるため、計算を分離）
                        absNanoseconds = unchecked(absNanoseconds + 1L);
                        // long.MinValueの場合は必ず負の値として扱う
                        isNegative = true;
                        
                        // absNanosecondsが負の値の場合は正の値に修正
                        if (absNanoseconds < 0)
                        {
                            absNanoseconds = Math.Abs(absNanoseconds);
                        }
                    }
                    else
                    {
                        isNegative = nanoseconds < 0;
                        absNanoseconds = Math.Abs(nanoseconds);
                    }
                    
                    // absNanosecondsが負の値の場合は正の値に修正
                    if (absNanoseconds < 0)
                    {
                        absNanoseconds = Math.Abs(absNanoseconds);
                    }
                    
                    // ナノ秒を秒に変換（大きな値に対応するためdoubleを使用）
                    double totalSeconds = absNanoseconds / 1_000_000_000.0;
                    
                    // 日、時、分、秒、ミリ秒に分解
                    long totalDays = (long)(totalSeconds / 86400);
                    int hours = (int)((totalSeconds % 86400) / 3600);
                    int minutes = (int)((totalSeconds % 3600) / 60);
                    int seconds = (int)(totalSeconds % 60);
                    double milliseconds = (totalSeconds * 1000) % 1000;
                    
                    // 時間形式で表示（例: -106751d23h47m16s854.775ms）
                    var parts = new List<string>();
                    
                    if (isNegative)
                    {
                        parts.Add("-");
                    }
                    
                    if (totalDays > 0)
                    {
                        parts.Add($"{totalDays}d");
                    }
                    if (hours > 0)
                    {
                        parts.Add($"{hours}h");
                    }
                    if (minutes > 0)
                    {
                        parts.Add($"{minutes}m");
                    }
                    if (seconds > 0)
                    {
                        parts.Add($"{seconds}s");
                    }
                    parts.Add($"{milliseconds:F3}ms");
                    
                    string result = string.Join("", parts);
                    return result;
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
                // YYYY-MM-dd-HH:mm:ss.ff 形式の文字列をDATE_AND_TIME形式に変換
                if (DateTime.TryParseExact(displayValue, "yyyy-MM-dd-HH:mm:ss.ff", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                {
                    // 基準日時: 1970年1月1日 00:00:00
                    DateTime baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    
                    // 経過秒数を計算
                    TimeSpan timeSpan = dateTime - baseDateTime;
                    long seconds = (long)timeSpan.TotalSeconds;
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
                // "-106751d23h47m16s854.775ms" 形式の文字列をナノ秒に変換
                if (displayValue.EndsWith("ms"))
                {
                    string timePart = displayValue.Substring(0, displayValue.Length - 2);
                    
                    // 負の値の処理
                    bool isNegative = timePart.StartsWith("-");
                    if (isNegative)
                    {
                        timePart = timePart.Substring(1); // マイナス記号を除去
                    }
                    
                    // 日、時、分、秒、ミリ秒を解析
                    int days = 0, hours = 0, minutes = 0, seconds = 0;
                    double milliseconds = 0;
                    
                    // 正規表現で各部分を抽出
                    var dayMatch = System.Text.RegularExpressions.Regex.Match(timePart, @"(\d+)d");
                    if (dayMatch.Success)
                    {
                        days = int.Parse(dayMatch.Groups[1].Value);
                        timePart = timePart.Replace(dayMatch.Value, "");
                    }
                    
                    var hourMatch = System.Text.RegularExpressions.Regex.Match(timePart, @"(\d+)h");
                    if (hourMatch.Success)
                    {
                        hours = int.Parse(hourMatch.Groups[1].Value);
                        timePart = timePart.Replace(hourMatch.Value, "");
                    }
                    
                    var minuteMatch = System.Text.RegularExpressions.Regex.Match(timePart, @"(\d+)m");
                    if (minuteMatch.Success)
                    {
                        minutes = int.Parse(minuteMatch.Groups[1].Value);
                        timePart = timePart.Replace(minuteMatch.Value, "");
                    }
                    
                    var secondMatch = System.Text.RegularExpressions.Regex.Match(timePart, @"(\d+)s");
                    if (secondMatch.Success)
                    {
                        seconds = int.Parse(secondMatch.Groups[1].Value);
                        timePart = timePart.Replace(secondMatch.Value, "");
                    }
                    
                    // ミリ秒部分を解析（新しい形式: 854.775ms）
                    if (timePart.Length > 0)
                    {
                        milliseconds = double.Parse(timePart, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    
                    // 総ナノ秒数を計算
                    long totalNanoseconds = (long)(
                        days * 86400L * 1_000_000_000L +
                        hours * 3600L * 1_000_000_000L +
                        minutes * 60L * 1_000_000_000L +
                        seconds * 1_000_000_000L +
                        milliseconds * 1_000_000L
                    );
                    
                    // 負の値の場合はマイナスを付ける
                    if (isNegative)
                    {
                        totalNanoseconds = -totalNanoseconds;
                    }
                    
                    return totalNanoseconds.ToString();
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
                    // 基準日時: 1970年1月1日 00:00:00
                    DateTime baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    
                    // 経過秒数を計算
                    TimeSpan timeSpan = parsedDateTime - baseDateTime;
                    long seconds = (long)timeSpan.TotalSeconds;
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
                    
                    // Sysmac Studioの基準日時: 1970年1月1日 00:00:00
                    DateTime baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime parsedDateTime = baseDateTime.AddSeconds(seconds);
                    
                    // 妥当な範囲かチェック（例: 1970年～2106年）
                    return parsedDateTime.Year >= 1970 && parsedDateTime.Year <= 2106;
                }
                
                // 文字列形式の場合（yyyy-MM-dd-HH:mm:ss.ff）
                if (DateTime.TryParseExact(value, "yyyy-MM-dd-HH:mm:ss.ff", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateAndTime))
                {
                    // 妥当な範囲かチェック（例: 1970年～2106年）
                    return parsedDateAndTime.Year >= 1970 && parsedDateAndTime.Year <= 2106;
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
                    // 妥当な範囲かチェック（負の値も含む）
                    long absNanoseconds = Math.Abs(nanoseconds);
                    return absNanoseconds <= 86_400_000_000_000L;
                }
                
                // 文字列形式の場合（-106751d23h47m16s854.775ms）
                if (value.EndsWith("ms"))
                {
                    string timePart = value.Substring(0, value.Length - 2);
                    
                    // 負の値の処理
                    if (timePart.StartsWith("-"))
                    {
                        timePart = timePart.Substring(1);
                    }
                    
                    // 時間形式の妥当性チェック
                    if (timePart.Contains("d") || timePart.Contains("h") || timePart.Contains("m") || timePart.Contains("s"))
                    {
                        // 時間形式の場合は基本的に妥当とする
                        return true;
                    }
                    
                    // 単純なミリ秒値の場合（小数点付きも対応）
                    if (double.TryParse(timePart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double milliseconds))
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
                    
                    // Sysmac Studioの基準日時: 1970年1月1日 00:00:00
                    DateTime baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime parsedDate = baseDateTime.AddSeconds(seconds);
                    
                    // 妥当な範囲かチェック（例: 1970年～2106年）
                    return parsedDate.Year >= 1970 && parsedDate.Year <= 2106;
                }
                
                // 文字列形式の場合（yyyy-MM-dd）
                if (DateTime.TryParseExact(value, "yyyy-MM-dd", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateValue))
                {
                    // 妥当な範囲かチェック（例: 1970年～2106年）
                    return parsedDateValue.Year >= 1970 && parsedDateValue.Year <= 2106;
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
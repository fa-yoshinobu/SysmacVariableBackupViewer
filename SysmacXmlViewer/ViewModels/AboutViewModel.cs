using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SysmacXmlViewer.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        public string RuntimeVersion { get; }
        public string OSVersion { get; }
        public string Architecture { get; }
        public string BuildDate { get; }
        public string AssemblyVersion { get; }
        public string ProductName { get; }
        public string ProductVersion { get; }
        public string CompanyName { get; }
        public string Copyright { get; }
        public List<LibraryInfo> Libraries { get; }
        public string CodePagesLicense { get; }
        public string DotNetLicense { get; }
        public string ApplicationLicense { get; }

        public AboutViewModel()
        {
            // システム情報を取得
            RuntimeVersion = Environment.Version.ToString();
            OSVersion = Environment.OSVersion.ToString();
            Architecture = RuntimeInformation.ProcessArchitecture.ToString();
            
            // アセンブリ情報を取得
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            AssemblyVersion = version?.ToString() ?? "Unknown";
            
            // より詳細なアセンブリ情報を取得
            try
            {
                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                ProductName = fileVersionInfo.ProductName ?? "SysmacVariableBackupViewer";
                ProductVersion = fileVersionInfo.ProductVersion ?? "1.0.2";
                CompanyName = fileVersionInfo.CompanyName ?? "SysmacVariableBackupViewer";
                Copyright = fileVersionInfo.LegalCopyright ?? "Copyright © 2025";
            }
            catch
            {
                // ファイルバージョン情報の取得に失敗した場合のフォールバック
                ProductName = "SysmacVariableBackupViewer";
                ProductVersion = "1.0.2";
                CompanyName = "SysmacVariableBackupViewer";
                Copyright = "Copyright © 2025";
            }
            
            // ビルド日時を取得（実際のビルド日時は別途設定が必要）
            BuildDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // ライブラリ情報を設定
            Libraries = new List<LibraryInfo>
            {
                new LibraryInfo
                {
                    Name = "System.Text.Encoding.CodePages",
                    Version = "7.0.0",
                    License = "MIT",
                    Description = "Provides encoding support for various code pages"
                },
                new LibraryInfo
                {
                    Name = ".NET 6.0 Runtime",
                    Version = "6.0.0",
                    License = "MIT",
                    Description = "Microsoft .NET Framework runtime"
                },
                new LibraryInfo
                {
                    Name = "Windows Presentation Foundation (WPF)",
                    Version = "6.0.0",
                    License = "MIT",
                    Description = "UI framework for Windows desktop applications"
                }
            };
            
            // ライセンス情報を設定
            CodePagesLicense = GetCodePagesLicense();
            DotNetLicense = GetDotNetLicense();
            ApplicationLicense = GetApplicationLicense();
        }

        private string GetCodePagesLicense()
        {
            return @"MIT License

Copyright (c) Microsoft Corporation. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.";
        }

        private string GetDotNetLicense()
        {
            return @"MIT License

Copyright (c) Microsoft Corporation. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

For more information about .NET licensing, visit:
https://github.com/dotnet/runtime/blob/main/LICENSE.TXT";
        }

        private string GetApplicationLicense()
        {
            return @"SysmacVariableBackupViewer License Agreement

Copyright (c) 2025 All rights reserved.

This software is provided ""as is"" without warranty of any kind, either express
or implied, including but not limited to the implied warranties of
merchantability and fitness for a particular purpose.

The author shall not be liable for any damages arising out of the use or
inability to use this software, including but not limited to direct, indirect,
incidental, special, exemplary, or consequential damages.

This software is designed for viewing Sysmac Studio XML backup files and
should be used in accordance with Omron's terms of service and licensing
agreements.

For technical support or licensing inquiries, please contact the developer.

Version: 1.0.2
Build Date: " + BuildDate + @"

Features:
- XML backup file parsing and viewing
- Variable data type conversion and display
- Advanced filtering and search capabilities
- CSV export functionality
- High-performance data handling
- Multi-language support";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LibraryInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

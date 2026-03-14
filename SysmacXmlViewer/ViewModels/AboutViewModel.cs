using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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
        public string TargetFrameworkDisplay { get; }
        public List<LibraryInfo> Libraries { get; }
        public string CodePagesLicense { get; }
        public string DotNetLicense { get; }
        public string ApplicationLicense { get; }
        public string Author { get; }
        public string GitHubUrl { get; }

        public AboutViewModel()
        {
            RuntimeVersion = RuntimeInformation.FrameworkDescription;
            OSVersion = RuntimeInformation.OSDescription;
            Architecture = RuntimeInformation.ProcessArchitecture.ToString();
            BuildDate = "Unknown";
            AssemblyVersion = "Unknown";
            ProductName = "SysmacVariableBackupViewer";
            ProductVersion = "Unknown";
            CompanyName = "SysmacVariableBackupViewer";
            Copyright = "Copyright (c) 2025";
            TargetFrameworkDisplay = $".NET {Environment.Version.Major}.0 Windows";
            Libraries = new List<LibraryInfo>();
            Author = "fa-yoshinobu";
            GitHubUrl = "https://github.com/fa-yoshinobu/SysmacVariableBackupViewer";
            CodePagesLicense = GetCodePagesLicense();
            DotNetLicense = GetDotNetLicense();
            ApplicationLicense = "License information unavailable";

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                AssemblyVersion = version?.ToString() ?? "Unknown";
                ProductVersion = AssemblyVersion;
            }
            catch
            {
            }

            try
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(processPath))
                {
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(processPath);

                    ProductName = string.IsNullOrWhiteSpace(fileVersionInfo.ProductName)
                        ? ProductName
                        : fileVersionInfo.ProductName;
                    ProductVersion = string.IsNullOrWhiteSpace(fileVersionInfo.ProductVersion)
                        ? ProductVersion
                        : fileVersionInfo.ProductVersion;
                    CompanyName = string.IsNullOrWhiteSpace(fileVersionInfo.CompanyName)
                        ? CompanyName
                        : fileVersionInfo.CompanyName;
                    Copyright = string.IsNullOrWhiteSpace(fileVersionInfo.LegalCopyright)
                        ? Copyright
                        : fileVersionInfo.LegalCopyright;
                    BuildDate = File.GetLastWriteTime(processPath).ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch
            {
            }

            try
            {
                Libraries = CreateLibraries();
            }
            catch
            {
            }

            try
            {
                ApplicationLicense = GetApplicationLicense();
            }
            catch
            {
            }
        }

        private static List<LibraryInfo> CreateLibraries()
        {
            var runtimeVersion = $"{Environment.Version.Major}.{Environment.Version.Minor}.{Environment.Version.Build}";
            var codePagesVersion = typeof(CodePagesEncodingProvider).Assembly.GetName().Version?.ToString(3) ?? "Unknown";

            return new List<LibraryInfo>
            {
                new LibraryInfo
                {
                    Name = "System.Text.Encoding.CodePages",
                    Version = codePagesVersion,
                    License = "MIT",
                    Description = "Provides encoding support for code pages"
                },
                new LibraryInfo
                {
                    Name = ".NET Runtime",
                    Version = runtimeVersion,
                    License = "MIT",
                    Description = "Microsoft .NET runtime"
                },
                new LibraryInfo
                {
                    Name = "Windows Presentation Foundation (WPF)",
                    Version = runtimeVersion,
                    License = "MIT",
                    Description = "UI framework for Windows desktop applications"
                }
            };
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
            return $@"SysmacVariableBackupViewer License Agreement

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

Version: {ProductVersion}
Build Date: {BuildDate}

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

using System;
using System.Windows;
using System.Text;
using System.Globalization;

namespace SysmacXmlViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 文字エンコーディングをUTF-8に設定
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // 日本語カルチャーを設定
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ja-JP");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ja-JP");
            
            base.OnStartup(e);
        }
    }
} 
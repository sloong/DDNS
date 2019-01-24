using Microsoft.Win32;
using sDDNS;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace sDDNSUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static string SERVICENAME = "SLOONG_DDNSService";
        static string REGISTERPATH = "SOFTWARE\\SLOONG.COM\\SLOONG_DDNS";
        string AppFolder = @"\SLOONG.COM\SLOONG_DDNS";
        string installutil_path = @"\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe";
        string ServiceExeName = "sDDNS.exe";
        string[] installFileList = new string[]
        {
            "sDDNS.exe",
            "sDDNSUI.exe",
            "Newtonsoft.Json.dll"
        };

        RegisterEx reg;

        public MainWindow()
        {
            InitializeComponent();
            AppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + AppFolder;
            installutil_path = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + installutil_path;
            reg = new RegisterEx(Registry.LocalMachine, REGISTERPATH);
            GetDataToUI();
            UpdateServiceStatus();
        }

        public void UpdateServiceStatus()
        {
            var path = Utility.GetServicePath(SERVICENAME);

            var ver = Utility.GetExeVersionInfo(path);
            var status = Utility.GetServiceStatus(SERVICENAME);
            if (status == "")
            {
                _ServiceStatus.Content = "No Install";
                _InstallBtn.Visibility = Visibility.Visible;
                _UninstallBtn.Visibility = Visibility.Hidden;
                _StartServiceBtn.Visibility = Visibility.Hidden;
                _StopServiceBtn.Visibility = Visibility.Hidden;
            }
            else
            {
                _ServiceStatus.Content = string.Format("{0} -- {1}", ver, status);
                _InstallBtn.Visibility = Visibility.Hidden;
                _UninstallBtn.Visibility = Visibility.Visible;
                if (status == "Stopped")
                {
                    _StartServiceBtn.Visibility = Visibility.Visible;
                    _StopServiceBtn.Visibility = Visibility.Hidden;
                }
                else
                {
                    _StopServiceBtn.Visibility = Visibility.Visible;
                    _StartServiceBtn.Visibility = Visibility.Hidden;
                }


            }
        }

        void GetDataToUI()
        {
            _Interval.Text = reg.GetValue("CheckInterval", "60");
            _AccessKeyID.Text = reg.GetValue("AccessKeyID", "");
            _AccessKeySecret.Text = reg.GetValue("AccessKeySecret", "");
            _Record_Name.Text = reg.GetValue("RecordName", "");
            _Domain_Name.Text = reg.GetValue("DomainName", "");

            _PublicIP.Text = Utility.GetPublicIP();
            var opt = reg.GetValue("DDNSType", "");

            foreach (ComboBoxItem item in _DDNSType.Items)
            {
                if (item.Tag.ToString() == opt)
                {
                    _DDNSType.SelectedItem = item;
                    break;
                }
            }
        }

        void SaveUIToData()
        {
            if (string.IsNullOrEmpty(_AccessKeyID.Text) ||
                string.IsNullOrEmpty(_AccessKeySecret.Text) ||
                string.IsNullOrEmpty(_Record_Name.Text) ||
                string.IsNullOrEmpty(_Domain_Name.Text) ||
                _DDNSType.SelectedItem==null)
            {
                MessageBox.Show("Please input!");
                return;
            }
            reg.SetValue("CheckInterval", _Interval.Text);
            reg.SetValue("AccessKeyID", _AccessKeyID.Text);
            reg.SetValue("AccessKeySecret", _AccessKeySecret.Text);
            reg.SetValue("RecordName", _Record_Name.Text);
            reg.SetValue("DomainName", _Domain_Name.Text);

            ComboBoxItem item = _DDNSType.SelectedItem as ComboBoxItem;
            reg.SetValue("DDNSType", item.Tag.ToString());
        }

        private void Button_Install_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(ServiceExeName))
            {
                if (Directory.Exists(AppFolder))
                    Directory.Delete(AppFolder, true);

                Directory.CreateDirectory(AppFolder);
                foreach( var item in installFileList )
                {
                    File.Copy(item, AppFolder + "\\" + item);
                }

                Utility.RunCMD(string.Format("{0} \"{1}\"", installutil_path, AppFolder + "\\"  + ServiceExeName));
                Thread.Sleep(500);

                UpdateServiceStatus();
            }
        }

        private void _StopServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(SERVICENAME);
            sc.Stop();
            UpdateServiceStatus();
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateServiceStatus();
        }

        private void _StartServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(SERVICENAME);
            sc.Start();
            UpdateServiceStatus();
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Uninstall_Click(object sender, RoutedEventArgs e)
        {
            var path = Utility.GetServicePath(SERVICENAME);
            Utility.RunCMD(string.Format("{0} /u \"{1}\"", installutil_path, path));
            Thread.Sleep(500);
            UpdateServiceStatus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveUIToData();
        }
    }
}

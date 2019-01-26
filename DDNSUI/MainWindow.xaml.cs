using Microsoft.Win32;
using DDNS;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Sloong;

namespace DDNSUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] installFileList = new string[]
        {
            "DDNS.exe",
            "DDNSUI.exe",
            "Newtonsoft.Json.dll"
        };

        RegisterEx reg;

        public MainWindow()
        {
            InitializeComponent();
            reg = new RegisterEx(Registry.LocalMachine, Defines.REGISTERPATH);
            foreach (var item in Enum.GetValues(typeof(LogLevel)))
            {
                string strName = Enum.GetName(typeof(LogLevel), item);//获取名称
                string strVaule = item.ToString();//获取值
                _LOG_LEVEL.Items.Add(strName);
            }
            GetDataToUI();
            UpdateServiceStatus();
        }

        public void UpdateServiceStatus()
        {
            var path = Utility.GetServicePath(Defines.SERVICENAME);

            var ver = Utility.GetExeVersionInfo(path);
            var status = Utility.GetServiceStatus(Defines.SERVICENAME);
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
            _IP_Life_Time.Text = reg.GetValue("IPLifeTime", "24");
            _Low_Interval.Text = reg.GetValue("LowInterval", "60");
            _High_Interval.Text = reg.GetValue("HighInterval", "5");
            _AccessKeyID.Text = reg.GetValue("AccessKeyID", "");
            _AccessKeySecret.Text = reg.GetValue("AccessKeySecret", "");
            _Record_Name.Text = reg.GetValue("RecordName", "");
            _Domain_Name.Text = reg.GetValue("DomainName", "");

            var level = reg.GetValue("LogLevel", "Info");
            foreach( var item in _LOG_LEVEL.Items)
            {
                if( item.ToString() == level)
                {
                    _LOG_LEVEL.SelectedItem = item;
                    break;
                }
            }

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
                _DDNSType.SelectedItem == null)
            {
                MessageBox.Show("Please input!");
                return;
            }
            reg.SetValue("IPLifeTime", _IP_Life_Time.Text);
            reg.SetValue("LowInterval", _Low_Interval.Text);
            reg.SetValue("HighInterval", _High_Interval.Text);
            reg.SetValue("AccessKeyID", _AccessKeyID.Text);
            reg.SetValue("AccessKeySecret", _AccessKeySecret.Text);
            reg.SetValue("RecordName", _Record_Name.Text);
            reg.SetValue("DomainName", _Domain_Name.Text);
            reg.SetValue("LogLevel", _LOG_LEVEL.SelectedItem.ToString());

            ComboBoxItem item = _DDNSType.SelectedItem as ComboBoxItem;
            reg.SetValue("DDNSType", item.Tag.ToString());
        }

        private void Button_Install_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Defines.ServiceExeName))
            {
                if (Directory.Exists(Defines.AppFolder))
                    Directory.Delete(Defines.AppFolder, true);

                Directory.CreateDirectory(Defines.AppFolder);
                foreach (var item in installFileList)
                {
                    File.Copy(item, Defines.AppFolder + "\\" + item);
                }

                Utility.RunCMD(string.Format("{0} \"{1}\"", Defines.installutil_path, Defines.AppFolder + "\\" + Defines.ServiceExeName));
                Thread.Sleep(500);

                UpdateServiceStatus();
            }
        }

        private void _StopServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(Defines.SERVICENAME);
            sc.Stop();
            UpdateServiceStatus();
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateServiceStatus();
        }

        private void _StartServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(Defines.SERVICENAME);
            sc.Start();
            UpdateServiceStatus();
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Uninstall_Click(object sender, RoutedEventArgs e)
        {
            var path = Utility.GetServicePath(Defines.SERVICENAME);
            Utility.RunCMD(string.Format("{0} /u \"{1}\"", Defines.installutil_path, path));
            Thread.Sleep(500);
            UpdateServiceStatus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveUIToData();
        }
    }
}

using Microsoft.Win32;
using Sloong;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace DDNS
{
    public partial class ServiceMain : ServiceBase
    {
        Thread _WorkThread = null;
        string sourceName = "SLOONG_DDNS";
        Log log = null;
        bool _Running = false;
        int _CheckInterval;
        RegisterEx reg;
        string ip = null;
        IDDNS iDDNS = null;
        public ServiceMain()
        {
            InitializeComponent();
#if DEBUG
            Thread.Sleep(10000);
#endif
            log = new Log(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\SLOONG.COM\SLOONG_DDNS\DDNS.log");
            //log.EnableEventLog(sourceName);

            reg = new RegisterEx(Registry.LocalMachine, "SOFTWARE\\SLOONG.COM\\" + sourceName);

            log.Write("SLOONG_DDNS Initialize.");
        }

        

        protected override void OnStart(string[] args)
        {
            log.Write("SLOONG_DDNS Service Start.");
#if DEBUG
            Thread.Sleep(10000);
#endif
            // Get param from Registry
            // Check interval time. default is 60 second
            string access_key_id = reg.GetValue("AccessKeyID", "");
            string access_key_secret = reg.GetValue("AccessKeySecret", "");
            string domain_name = reg.GetValue("DomainName", "");
            string record_name = reg.GetValue("RecordName", "");
            string ddns_Type = reg.GetValue("DDNSType", "");
            if (string.IsNullOrEmpty(access_key_id) ||
                string.IsNullOrEmpty(access_key_secret) ||
                string.IsNullOrEmpty(domain_name) ||
                string.IsNullOrEmpty(record_name) ||
                string.IsNullOrEmpty(ddns_Type))
            {
                log.Write("Param error.",LogLevel.Error);
                Environment.Exit(1);
            }

            // Check interval time. default is 60 second
            _CheckInterval = Convert.ToInt32(reg.GetValue("CheckInterval", "60"));

            _CheckInterval = _CheckInterval * 1000;

            if(ddns_Type == "AliDDNS")
            {
                iDDNS = new AliDDNS();
            }
            else
            {
                log.Write("No support ddns type.", LogLevel.Error);
                Environment.Exit(1);
            }

            iDDNS.Initialize(access_key_id, access_key_secret, domain_name, record_name, log);

            /*var timer1 = new System.Timers.Timer();

            timer1.Interval = 3000;  //设置计时器事件间隔执行时间

            timer1.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);

            timer1.Enabled = true;*/

            _Running = true;
            _WorkThread = new Thread(WorkLoop);
            _WorkThread.Name = "Work Thread";
            _WorkThread.Start();
        }

        protected override void OnStop()
        {
            log.Write("SLOONG_DDNS Stopped.");
            _Running = false;
            _WorkThread.Abort();
            log.Dispose();
        }

        void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {

        }

        public void WorkLoop()
        {
            log.Write("Work thread is starting.");
#if DEBUG
            Thread.Sleep(10000);
#endif
            while (_Running)
            {
                try
                {
                    string new_ip = Utility.GetPublicIP();
                    if (ip != new_ip)
                    {
                        log.Write("Update ip to " + new_ip);
                        if(iDDNS.OnUpdate(new_ip))
                            ip = new_ip;
                    }
                    Thread.Sleep(_CheckInterval);
                }
                catch(WebException e)
                {
                    using (Stream s = e.Response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(s, Encoding.UTF8);
                        string result = reader.ReadToEnd();
                        log.Write("WebException happened: " + result, LogLevel.Error);
                        // when fialed, run loop in each minute.
                        Thread.Sleep(600000);
                    }
                }
            }
        }
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sDDNS
{
    public partial class ServiceMain : ServiceBase
    {
        Thread _WorkThread = null;
        string sourceName = "SLOONG_DDNS";
        EventLog _EventLog = null;
        bool _Running = false;
        int _CheckInterval;
        RegisterEx reg;
        string ip = null;
        IDDNS iDDNS = null;
        public ServiceMain()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(sourceName))
            {
                EventLog.CreateEventSource(sourceName, "Application");
            }

            _EventLog = new EventLog();
            _EventLog.Source = sourceName;
            reg = new RegisterEx(Registry.LocalMachine, "SOFTWARE\\SLOONG.COM\\" + sourceName);

            WriteLog("SLOONG_DDNS Initialize.");
        }

        public void WriteLog(string msg, EventLogEntryType type = EventLogEntryType.Information)
        {
            var message = $"{AppDomain.CurrentDomain.BaseDirectory}{Environment.NewLine}";
            message = message + msg;
            _EventLog.WriteEntry(message, type);
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("SLOONG_DDNS Service Start.");

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
                WriteLog("Param error.", EventLogEntryType.Error);
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
                WriteLog("No support ddns type.", EventLogEntryType.Error);
                Environment.Exit(1);
            }

            iDDNS.Initialize(access_key_id, access_key_secret, domain_name, record_name);
            ip = Utility.GetPublicIP();
            iDDNS.OnUpdate(ip);

            _Running = true;
            _WorkThread = new Thread(WorkLoop);
            _WorkThread.Name = "Work Thread";
            _WorkThread.Start();
        }

        protected override void OnStop()
        {
            WriteLog("SLOONG_DDNS Stopped.");
            _Running = false;
            _WorkThread.Abort();
        }

        public void WorkLoop()
        {
            WriteLog("Work thread is starting.");
#if DEBUG
            Thread.Sleep(10000);
#endif
            while (_Running)
            {
                string new_ip = Utility.GetPublicIP();
                if ( ip != new_ip)
                {
                    WriteLog("IP is changed ", EventLogEntryType.Warning);
                    iDDNS.OnUpdate(new_ip);
                }
                
            }
        }
    }
}

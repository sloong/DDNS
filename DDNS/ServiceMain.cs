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
        int _IP_Life_Time,_Low_Interval, _High_Interval, _Beforehand_Time;
        DateTime _IPRefreshTime;
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
            var refresh_time = reg.GetValue("IPRefreshTime", "");
            if ( !DateTime.TryParse(refresh_time,out _IPRefreshTime))
            {
                log.Write("Try parse IPRefreshTime fialed. the value is:" + refresh_time, LogLevel.Warn);
                _IPRefreshTime = DateTime.MinValue;
            }

            // ip lift time, unit is hour, but we need check it beforehand. so need convert to mintue.
            _IP_Life_Time = Convert.ToInt32(reg.GetValue("IPLifeTime", "24")) * 60;
            _Beforehand_Time = Convert.ToInt32(reg.GetValue("BeforehandTime", "30"));
            _IP_Life_Time = _IP_Life_Time - _Beforehand_Time;
            // Time is minute. so need convert to millisecond.
            _Low_Interval = Convert.ToInt32(reg.GetValue("LowInterval", "60")) * 60 * 1000;
            _High_Interval = Convert.ToInt32(reg.GetValue("HighInterval", "5")) * 60 * 1000;

            log.Write(string.Format("Work params:IPLifeTime[{}]", _IP_Life_Time), LogLevel.Debug);

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
            string id = null;
            ip = iDDNS.QueryCurrent(out id);
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
                    // if ip is changed, record the time, and sleep whit the low interval.
                    string new_ip = Utility.GetPublicIP();
                    if (ip != new_ip)
                    {
                        log.Write("Update ip to " + new_ip);
                        if( _IPRefreshTime > DateTime.MinValue )
                        {
                            log.Write("Ip change interval " + (DateTime.Now - _IPRefreshTime).TotalHours, LogLevel.Info);
                        }
                        if(iDDNS.OnUpdate(new_ip))
                        {
                            ip = new_ip;
                            _IPRefreshTime = DateTime.Now;
                            reg.SetValue("IPRefreshTime", _IPRefreshTime.ToString());
                        }
                    }
                    var updatedTime = (DateTime.Now - _IPRefreshTime).TotalMinutes;
                    if (updatedTime > _IP_Life_Time)
                    {
                        log.Write("Sleep with time:" + _High_Interval, LogLevel.Verbos);
                        Thread.Sleep(_High_Interval);
                    }
                    else
                    {
                        // 这里(_IP_Life_Time - updatedTime)是计算当前时间到更新时间的差值
                        // the (life time - update time) is the time span of current time to update time.
                        // we must make sure when update time is big than life time, this loop can work by high interval.
                        // so here use the mined between time span value and low interval. 
                        var sleep_time = Math.Min((int)(_IP_Life_Time - updatedTime), _Low_Interval);
                        log.Write("Sleep with time:" + sleep_time, LogLevel.Verbos);
                        Thread.Sleep(sleep_time);
                    }
                        
                }
                catch(WebException e)
                {
                    using (Stream s = e.Response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(s, Encoding.UTF8);
                        string result = reader.ReadToEnd();
                        log.Write("WebException happened: " + result, LogLevel.Error);
                        // when fialed, run loop in each minute.
                        Thread.Sleep(_High_Interval);
                    }
                }
            }
        }
    }
}

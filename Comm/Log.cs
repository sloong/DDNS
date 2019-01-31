using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Sloong
{
    /// <summary>
    /// 企业应用框架的日志类
    /// </summary>
    /// <remarks>此日志类提供高性能的日志记录实现。
    /// 当调用Write方法时不会造成线程阻塞,而是立即完成方法调用,因此调用线程不用等待日志写入文件之后才返回。</remarks>
    public class Log : IDisposable
    {
        //日志对象的缓存队列
        private static Queue<Msg> msgs;
        //日志文件保存的路径
        private static string path;
        /// <summary>
        /// 日志文件的文件名称后缀
        /// </summary>
        private static string file_ex;
        //日志写入线程的控制标记
        private static bool state;
        //日志记录的类型
        private static LogType type;
        //日志文件生命周期的时间标记
        private static DateTime TimeSign;
        //日志文件写入流对象
        private static StreamWriter writer;

        private static LogLevel logLevel;

        private static bool debug = false;

        private static EventRecordType recordType = EventRecordType.LogFile;

        public enum EventRecordType
        {
            SystemEvent,
            LogFile,
        }


        AutoResetEvent wait_event = new AutoResetEvent(false);

        private static EventLog _EventLog = null;

        public void EnableEventLog(string sourceName)
        {
            if( _EventLog == null )
            {
                if (!EventLog.SourceExists(sourceName))
                {
                    EventLog.CreateEventSource(sourceName, "Application");
                }
                _EventLog = new EventLog();
                _EventLog.Source = sourceName;
                recordType = EventRecordType.SystemEvent;
            }
            
        }

        public void WriteToSystemEvent(string msg, LogLevel level)
        {
            EventLogEntryType type;
            if (level == LogLevel.Fatal || level == LogLevel.Error)
                type = EventLogEntryType.Error;
            else if (level == LogLevel.Warn)
                type = EventLogEntryType.Warning;
            else
                type = EventLogEntryType.Information;
            var message = $"{AppDomain.CurrentDomain.BaseDirectory}{Environment.NewLine}";
            message = message + msg;
            _EventLog.WriteEntry(message, type);
        }

        /// <summary>
        /// 创建日志对象的新实例，采用默认当前程序位置和当前程序名称作为日志文件名
        /// </summary>
        public Log( string fileName )
            : this(fileName,  LogLevel.Info, LogType.Single, "")
        {
        }

        public Log( string fileName, LogLevel level)
            : this( fileName, level, LogType.Single, "")
        {
        }
       

        /// <summary>
        /// 创建日志对象的新实例，根据指定的日志文件路径和指定的日志文件创建类型
        /// </summary>
        /// <param name="p">日志文件保存路径</param>
        /// <param name="t">日志文件创建方式的枚举</param>
        /// <param name="ex">日志文件的扩展文件名。如20170101_R中的“R”</param>
        public Log(string p, LogLevel level, LogType t, string ex)
        {
            if (msgs == null)
            {
                state = true;
                path = p;
                type = t;
                file_ex = ex;
                logLevel = level;
                msgs = new Queue<Msg>();
                Thread thread = new Thread(work);
                thread.Start();
                thread.Name = "Log Thread";
            }
        }


        //日志文件写入线程执行的方法
        private void work()
        {
            while (true)
            {
                //判断队列中是否存在待写入的日志
                if (msgs.Count > 0)
                {
                    Msg msg = null;
                    lock (msgs)
                    {
                        msg = msgs.Dequeue();
                    }
                    if (msg != null)
                    {
                        FileWrite(msg);
                    }
                }
                else
                {
                    //判断是否已经发出终止日志并关闭的消息
                    if (state)
                    {
                        wait_event.WaitOne();
                    }
                    else
                    {
                        FileClose();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 启用log系统的调试模式，这样在写入之后会立即写入到磁盘中，而不是写入到缓存中。
        /// </summary>
        /// <param name="enable"></param>
        public void Debug(bool enable)
        {
            debug = enable;
        }

        //根据日志类型获取日志文件名，并同时创建文件到期的时间标记
        //通过判断文件的到期时间标记将决定是否创建新文件。
        private string GetFilename(string path)
        {
            DateTime now = DateTime.Now;
            string format = "";
            string ex = file_ex + "'.log'";
            if (ex != "")
                ex = "_" + ex;

            switch (type)
            {
                case LogType.Daily:
                    TimeSign = new DateTime(now.Year, now.Month, now.Day);
                    TimeSign = TimeSign.AddDays(1);
                    format = "yyyyMMdd";
                    break;
                case LogType.Single:
                    // 单文件模式直接返回
                    return path;
            }
            return now.ToString(path + format + ex);
        }

        //写入日志文本到文件的方法
        private void FileWrite(Msg msg)
        {
            try
            {
                if (writer == null)
                {
                    FileOpen();
                }

                //判断文件到期标志，如果当前文件到期则关闭当前文件创建新的日志文件
                if (DateTime.Now >= TimeSign)
                {
                    FileClose();
                    FileOpen();
                }
                writer.WriteLine(string.Format("[{0}]:[{1}]:[{2}]", msg.Datetime, msg.Level, msg.Text));
                if (debug)
                    writer.Flush();
            }
            catch (Exception e)
            {
                Console.Out.Write(e);
            }
        }

        //打开文件准备写入
        private void FileOpen()
        {
            if( type != LogType.Single && !Directory.Exists(path))
                Directory.CreateDirectory(path);
            //writer = new FileStream(GetFilename(path), FileMode.Append, FileAccess.Write, FileShare.Write);// StreamWriter( GetFilename(path), true, Encoding.UTF8);
            writer = new StreamWriter( GetFilename(path), true, Encoding.UTF8);
        }

        //关闭打开的日志文件
        private void FileClose()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
                writer = null;
            }
        }

        /// <summary>
        /// 写入新日志，根据指定的日志对象Msg
        /// </summary>
        /// <param name="msg">日志内容对象</param>
        public void Write(Msg msg)
        {
            if( msg.Level < logLevel )
            {
                return;
            }
            if(recordType == EventRecordType.SystemEvent)
            {
                WriteToSystemEvent(msg.Text, msg.Level);
            }
            else
            {
                if (msg != null)
                {
                    lock (msgs)
                    {
                        msgs.Enqueue(msg);
                        wait_event.Set();
                    }
                }
            }            
        }

        public void Flush()
        {
            if (writer != null)
            {
                writer.Flush();
            }
        }

        /// <summary>
        /// 写入新日志，根据指定的日志内容和信息类型，采用当前时间为日志时间写入新日志
        /// </summary>
        /// <param name="text">日志内容</param>
        /// <param name="type">信息类型</param>
        public void Write(string text, LogLevel type = LogLevel.Info)
        {
            Write(new Msg(text, type));
            Console.WriteLine(text);
        }

        /// <summary>
        /// 写入新日志，根据指定的日志时间、日志内容和信息类型写入新日志
        /// </summary>
        /// <param name="dt">日志时间</param>
        /// <param name="text">日志内容</param>
        /// <param name="type">信息类型</param>
        public void Write(DateTime dt, string text, LogLevel type)
        {
            Write(new Msg(dt, text, type));
        }

        /// <summary>
        /// 写入新日志，根据指定的异常类和信息类型写入新日志
        /// </summary>
        /// <param name="e">异常对象</param>
        /// <param name="type">信息类型</param>
        public void Write(Exception e, LogLevel type, string txt)
        {
            Write(new Msg(txt + Environment.NewLine + e.ToString(), type));
        }

        #region IDisposable 成员

        /// <summary>
        /// 销毁日志对象
        /// </summary>
        public void Dispose()
        {
            state = false;
        }

        #endregion
    }
    /// <summary>
    /// 日志类型的枚举
    /// </summary>
    /// <remarks>日志类型枚举指示日志文件创建的方式，如果日志比较多可考虑每天创建一个日志文件
    /// 如果日志量比较小可考虑每周、每月或每年创建一个日志文件</remarks>
    public enum LogType
    {
        /// <summary>
        /// 不自动创建文件，路径输入即为一个日志文件
        /// </summary>
        Single,

        /// <summary>
        /// 此枚举指示每天创建一个新的日志文件
        /// </summary>
        Daily,
    }
    /// <summary>
    /// 表示一个日志记录的对象
    /// </summary>
    public class Msg
    {
        //日志记录的时间
        private DateTime datetime;
        //日志记录的内容
        private string text;
        //日志记录的类型
        private LogLevel level;

        /// <summary>
        /// 创建新的日志记录实例;日志记录的内容为空,消息类型为MsgType.Unknown,日志时间为当前时间
        /// </summary>
        public Msg( string t)
            : this(t, LogLevel.Info)
        {
        }

        /// <summary>
        /// 创建新的日志记录实例;日志事件为当前时间
        /// </summary>
        /// <param name="t">日志记录的文本内容</param>
        /// <param name="p">日志记录的消息类型</param>
        public Msg(string t, LogLevel p)
            : this(DateTime.Now, t, p)
        {
        }

        /// <summary>
        /// 创建新的日志记录实例;
        /// </summary>
        /// <param name="dt">日志记录的时间</param>
        /// <param name="t">日志记录的文本内容</param>
        /// <param name="p">日志记录的消息类型</param>
        public Msg(DateTime dt, string t, LogLevel p)
        {
            datetime = dt;
            level = p;
            text = t;
        }

        /// <summary>
        /// 获取或设置日志记录的时间
        /// </summary>
        public DateTime Datetime
        {
            get { return datetime; }
            set { datetime = value; }
        }

        /// <summary>
        /// 获取或设置日志记录的文本内容
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        /// <summary>
        /// 获取或设置日志记录的消息类型
        /// </summary>
        public LogLevel Level
        {
            get { return level; }
            set { level = value; }
        }


        public new string ToString()
        {
            return datetime.ToString() + "\t" + text + "\n";
        }
    }

    /// <summary>
    /// 日志消息类型的枚举
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 繁琐的细节信息
        /// </summary>
        Verbos,

        /// <summary>
        /// 调试信息
        /// </summary>
        Debug,

        /// <summary>
        /// 普通信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告信息
        /// </summary>
        Warn,

        /// <summary>
        /// 程序仍可以继续运行的错误信息
        /// </summary>
        Error,
        
        /// <summary>
        /// 导致程序无法继续运行的重大错误
        /// </summary>
        Fatal,
    }
}
using System;


namespace DDNS
{
    public static class Defines
    {
        public static string sourceName = "DDNS";
        public static string SERVICENAME = "SLOONG_DDNSService";
        public static string REGISTERPATH = "SOFTWARE\\SLOONG.COM\\DDNS";
        public static string AppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\SLOONG.COM\DDNS";
        public static string installutil_path = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe";
        public static string ServiceExeName = "DDNS.exe";
    }
}
using DDNS;
using Sloong;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDNS
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            DDNSHub hub = new DDNSHub();
            hub.Run();
        }

    }
}
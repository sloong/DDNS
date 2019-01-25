using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDNS
{
    interface IDDNS
    {
        void Initialize(string access_key_id, string access_key_secret, string domain_name, string record_name, Sloong.Log log);
        /// <summary>
        /// 成功返回true
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        bool OnUpdate(string ip);

        string QueryCurrent(out string recordID);
    }
}

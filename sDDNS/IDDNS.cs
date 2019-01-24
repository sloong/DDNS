using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sDDNS
{
    interface IDDNS
    {
        void Initialize(string access_key_id, string access_key_secret, string domain_name, string record_name, Sloong.Log log);
        void OnUpdate(string ip);
    }
}

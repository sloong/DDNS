using Newtonsoft.Json.Linq;
using Sloong;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sDDNS
{
    class AliDDNS : IDDNS
    {
        string record_name;
        AliDns dns = null;
        Log log;
        public void Initialize(string access_key_id , string access_key_secret, string domain_name, string record_name, Log log)
        {
            dns = new AliDns(access_key_id, access_key_secret, domain_name,log );
            this.record_name = record_name;
            this.log = log;
        }


        public bool OnUpdate(string ip)
        {
            var res = dns.describe_domain_records();
            var jRes = JObject.Parse(res);
            var list = jRes["DomainRecords"]["Record"].ToArray();
            string recordID = null;
            string recordValue = null;
            foreach (var item in list)
            {
                if (item["RR"].ToString() == record_name)
                {
                    recordID = item["RecordId"].ToString();
                    recordValue = item["Value"].ToString();
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(recordID))
            {
                log.Write(string.Format("Add domain reocrd [{0}] by {1}", record_name, ip));
                dns.add_domain_record("A", record_name, ip);
            }
            else if( recordValue != ip )
            {
                log.Write(string.Format("Update domain reocrd [{0}] to {1}", record_name, ip));
                dns.update_domain_record(recordID, "A", record_name, ip);
            }
            else
            {
                log.Write("Update called, but value no changed.", LogLevel.Verbos);
            }
            return true;
        }
    }
}

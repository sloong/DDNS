using Newtonsoft.Json.Linq;
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
        public void Initialize(string access_key_id , string access_key_secret, string domain_name, string record_name)
        {
            AliDns dns = new AliDns(access_key_id, access_key_secret, domain_name );
            this.record_name = record_name;
        }

        public bool CheckUpdate(string old_ip)
        {
            string ip = Utility.GetPublicIP();
            if (ip == old_ip)
                return true;
            else
                return false;
        }

        public void OnUpdate(string ip)
        {
            var res = dns.describe_domain_records();
            var jRes = JObject.Parse(res);
            var list = jRes["DomainRecords"]["Record"].ToArray();
            string recordID = null;
            foreach (var item in list)
            {
                if (item["RR"].ToString() == record_name)
                    recordID = item["RecordId"].ToString();
            }
            if (string.IsNullOrWhiteSpace(recordID))
            {
                dns.add_domain_record("A", record_name, ip);
            }
            else
            {
                dns.update_domain_record(recordID, "A", record_name, ip);
            }
        }
    }
}

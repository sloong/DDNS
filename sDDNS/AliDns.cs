using Sloong;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace sDDNS
{
    /* # domain = AliDns(ACCESS_KEY_ID, ACCESS_KEY_SECRET, 'simplehttps.com')
    # domain.describe_domain_records()
    # 增加记录
    # domain.add_domain_record("TXT", "test", "test")

   
    # 修改解析
    #domain.update_domain_record('4011918010876928', 'TXT', 'test2', 'text2')
    # 删除解析记录
    # data = domain.describe_domain_records()
    # record_list = data["DomainRecords"]["Record"]
    # for item in record_list:
    #	if 'test' in item['RR']:
    #		domain.delete_domain_record(item['RecordId'])

   
    #print(sys.argv)
    file_name, certbot_domain, acme_challenge, certbot_validation = sys.argv

    domain = AliDns(ACCESS_KEY_ID, ACCESS_KEY_SECRET, certbot_domain)
    data = domain.describe_domain_records()
    record_list = data["DomainRecords"]["Record"]
    if record_list:
        for item in record_list:
            if acme_challenge == item['RR']:
                domain.delete_domain_record(item['RecordId'])

    domain.add_domain_record("TXT", acme_challenge, certbot_validation)
     */
    class AliDns
    {
        string access_key_id;
        string access_key_secret;
        string domain_name;
        Log log;
        public AliDns(string access_key_id, string access_key_secret, string domain_name, Log log)
        {
            this.access_key_id = access_key_id;
            this.access_key_secret = access_key_secret;
            this.domain_name = domain_name;
            this.log = log;
        }

        //生成一个指定长度(默认14位)的随机数值，其中
        //string.digits = "0123456789'
        string generate_random_str(int length = 14)
        {
            Random random = new Random();
            string random_str = "";
            for (int i = 0; i < length; i++)
                random_str += random.Next(10);

            return random_str;
        }


        string UrlEncode(string str)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in str)
            {
                if (HttpUtility.UrlEncode(c.ToString()).Length > 1)
                {
                    builder.Append(HttpUtility.UrlEncode(c.ToString()).ToUpper());
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
       

        string percent_encode(string str)
        {
            //str = Encoding.UTF8.GetString(str);
            str.Replace("+", "%20");
            str.Replace("*", "%2A");
            str.Replace("%7E", "`");
            return UrlEncode(str);
        }

        //请求的时间戳。日期格式按照ISO8601标准表示，
        //并需要使用UTC时间。格式为YYYY-MM-DDThh:mm:ssZ
        //例如，2015-01-09T12:00:00Z（为UTC时间2015年1月9日12点0分0秒）
        string utc_time()
        {
            var utc_time = DateTime.Now.ToUniversalTime();
            return utc_time.ToString("yyyy-MM-ddThh:mm:ssZ");
        }

   


        string urlencode(Dictionary<string, string> url_param)
        {
            string param_str = "";
            foreach (var item in url_param)
            {
                param_str += '&' + UrlEncode(item.Key) + '=' + UrlEncode(item.Value);
            }
            return param_str.Substring(1);
        }

        string sign_string(Dictionary<string, string> url_param)
        {
            SortedDictionary<string, string> url_paramSort = new SortedDictionary<string, string>(url_param, StringComparer.Ordinal);

            string param_str = "";
            foreach (var item in url_paramSort)
            {
                param_str += '&' + percent_encode(item.Key) + '=' + percent_encode(item.Value);
            }
            param_str = param_str.Substring(1);
            return "GET" + "&" + "%2F" + "&" + percent_encode(param_str);
        }

        string access_url(string url)
        {
            log.Write("Access url:" + url, LogLevel.Verbos);
            WebRequest wr = WebRequest.Create(url);
            using (Stream s = wr.GetResponse().GetResponseStream())
            {
                StreamReader reader = new StreamReader(s, Encoding.UTF8);
                string result = reader.ReadToEnd();
                return result;
            }
        }

        string visit_url(Dictionary<string, string> action_param)
        {
            Dictionary<string, string> common_param = new Dictionary<string, string>{
                { "Format","json"},
                { "Version", "2015-01-09"},
                { "AccessKeyId", this.access_key_id },
                { "SignatureMethod", "HMAC-SHA1" },
                //{ "Timestamp", "2015-01-09T12:00:00Z" },//this.utc_time() },
                { "Timestamp", this.utc_time() },
                { "SignatureVersion", "1.0" },
                //{ "SignatureNonce", "01234567890123" },//this.generate_random_str() },
                { "SignatureNonce", this.generate_random_str() },
                { "DomainName", this.domain_name }
            };
            foreach (var item in action_param)
            {
                common_param.Add(item.Key, item.Value);
            }
            var string_to_sign = this.sign_string(common_param);
            log.Write("string_to_sign:"+string_to_sign, LogLevel.Debug);
            string hash_bytes = this.access_key_secret + "&";
            HMACSHA1 myHMACSHA1 = new HMACSHA1(Encoding.UTF8.GetBytes(hash_bytes));
            byte[] byteText = myHMACSHA1.ComputeHash(Encoding.UTF8.GetBytes(string_to_sign));
            var signature = System.Convert.ToBase64String(byteText);
            log.Write("signature:"+signature, LogLevel.Debug);
            common_param.Add("Signature", signature);
            string url = "https://alidns.aliyuncs.com/?" + urlencode(common_param);
            return access_url(url);
        }

        // 最多只能查询此域名的 500条解析记录
        //PageNumber 当前页数，起始值为1，默认为1
        //PageSize  分页查询时设置的每页行数，最大值500，默认为20
        public string describe_domain_records()
        {
            Dictionary<string, string> action_param = new Dictionary<string, string>{
                { "Action" , "DescribeDomainRecords"},
                { "PageNumber",  "1"},
                { "PageSize",  "500"}};
            var result = visit_url(action_param);
            return result;
        }

        // 增加解析
        public string add_domain_record(string type, string rr, string value)
        {
            Dictionary<string, string> action_param = new Dictionary<string, string>
            {
                {"Action","AddDomainRecord" },
                {"RR", rr },
                {"Type", type },
                {"Value", value },
            };
            return visit_url(action_param);
        }

        //修改解析
        public string update_domain_record(string id, string type, string rr, string value)
        {
            Dictionary<string, string> action_param = new Dictionary<string, string>
            {
                {"Action","UpdateDomainRecord" },
                {"RecordId", id },
                {"RR", rr },
                {"Type", type },
                {"Value", value },
            };
            return visit_url(action_param);
        }

        //删除解析
        public string delete_domain_record(string id)
        {
            Dictionary<string, string> action_param = new Dictionary<string, string>
            {
                {"Action","DeleteDomainRecord" },
                {"RecordId", id },
            };
            return visit_url(action_param);
        }

    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sDDNS
{
    public class RegisterEx
    {
        string _Path;
        RegistryKey _RegKey;

        public RegisterEx(RegistryKey pos, string path)
        {
            _Path = path;
            _RegKey = pos.CreateSubKey(path);
        }

        ~RegisterEx()
        {
            _RegKey.Close();
        }


        public string GetValue(string key, string def)
        {
            try
            {
                var value = _RegKey.GetValue(key);
                if (value == null)
                    return def;
                return value.ToString();
            }
            catch (Exception)
            {
                return def;
            }
        }

        public void SetValue(string key, string value)
        {
            _RegKey.SetValue(key, value);
        }
    }
}

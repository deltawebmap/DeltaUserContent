using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaUserContent
{
    public class ServiceConfig
    {
        public string database_config = @"E:\database_config.json";
        public int port = 43293;
        public int max_filesize = 10485760;
        public string content_path = @"C:\Users\Roman\Documents\delta_dev\backend\usercontent\saved\";
        public string node_name = "prod-usercontent-v1";
        public string content_url = "https://user-content.deltamap.net/u/";
        public List<ServiceConfig_Application> applications;
    }

    public class ServiceConfig_Application
    {
        public string id;
        public string type;
        public string name;
        public JObject settings;

        public T ReadSettings<T>()
        {
            return settings.ToObject<T>();
        }
    }
}

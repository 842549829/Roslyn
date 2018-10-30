using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Xml;

namespace Model
{
    /// <summary>
    /// 配置信息
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// 获取配置文件对象
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static IConfigurationRoot GetConfigurationRootByJson(string path)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(path);
            return configurationBuilder.Build();
        }

        /// <summary>
        /// 获取配置文件对象
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static IConfigurationRoot GetConfigurationRootByXml(string path)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            IConfigurationSource configurationSource = new XmlConfigurationSource
            {
                Path = path
            };
            configurationBuilder.Add(configurationSource);
            return configurationBuilder.Build();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Model
{
    /// <summary>
    /// http
    /// </summary>
    public class HttpClientUtility
    {
        /// <summary>
        /// 服务提供者
        /// </summary>
        private static readonly IServiceProvider serviceProvider;

        /// <summary>
        /// 调用名称
        /// </summary>
        private readonly string name = string.Empty;

        /// <summary>
        /// 调用地址
        /// </summary>
        private string url;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static HttpClientUtility()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(Options.DefaultName);
            // 根据配置文件加载
            AddHttpClient(serviceCollection);
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(serviceCollection);
            serviceProvider = new AutofacServiceProvider(containerBuilder.Build());

            /*
            serviceCollection.AddHttpClient("www.cnblogs.com", configureClient =>
            {
                configureClient.BaseAddress = new Uri("http://www.cnblogs.com");
                //configureClient.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Test");
            });

            // 测试
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("www.cnblogs.com");
            var response = client.GetAsync("/liuxiaoji/").ConfigureAwait(false).GetAwaiter().GetResult();
            var rel = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            */
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url"></param>
        public HttpClientUtility(string url)
        {
            this.url = url;
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="name">名称</param>
        public HttpClientUtility(string url, string name)
            : this(url)
        {
            this.name = name;
        }

        /// <summary>
        /// 头信息
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// AddHttpClient
        /// </summary>
        /// <param name="serviceCollection">serviceCollection</param>
        private static void AddHttpClient(ServiceCollection serviceCollection)
        {
            if (File.Exists(Const.HttpClienFactoryJson))
            {
                IConfiguration configuration = Configuration.GetConfigurationRootByJson(Const.HttpClienFactoryJson);
                foreach (var rootItem in AsEnumerable(configuration))
                {
                    var rootConfigurationSection = (ConfigurationSection)rootItem;
                    var name = rootConfigurationSection.Key;
                    string url = null;
                    var headersDictionary = new Dictionary<string, string>();
                    foreach (var nameItem in rootItem.GetChildren())
                    {
                        if (nameItem.Key == Const.Url)
                        {
                            url = nameItem.Value;
                        }

                        if (nameItem.Key == Const.Headers)
                        {
                            var headers = nameItem.GetChildren();
                            foreach (var headersItem in headers)
                            {
                                foreach (var item in headersItem.GetChildren())
                                {
                                    headersDictionary.Add(item.Key, item.Value);
                                }
                            }
                        }
                    }

                    serviceCollection.AddHttpClient(name, configureClient =>
                    {
                        if (url != null)
                        {
                            configureClient.BaseAddress = new Uri(url);
                        }

                        foreach (var item in headersDictionary)
                        {
                            configureClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// GET
        /// </summary>
        /// <returns>结果</returns>
        public string Get()
        {
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient(name);
            foreach (var header in Headers)
            {
                http.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = http.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// GET
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <returns>结果</returns>
        public T Get<T>()
        {
            var response = Get();
            return JsonConvert.DeserializeObject<T>(response);
        }

        /// <summary>
        /// GET
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>结果</returns>
        public string Get(IEnumerable<string> parameters)
        {
            url = url + GteParameters(parameters);
            return Get();
        }

        /// <summary>
        /// GET
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="parameters">参数</param>
        /// <returns>结果</returns>
        public T Get<T>(IEnumerable<string> parameters)
        {
            var response = Get(parameters);
            return JsonConvert.DeserializeObject<T>(response);
        }

        /// <summary>
        /// POST
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="data">data</param>
        /// <returns>结果</returns>
        public string Post<T>(T data)
        {
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(name);
            client.DefaultRequestHeaders.Clear();
            if (this.Headers.Any())
            {
                foreach (var header in Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            else
            {
                client.DefaultRequestHeaders.Add(Const.Accept, Const.ApplicationJson);
            }
            var map = JsonConvert.SerializeObject(data, new JsonSerializerSettings { DateFormatString = Const.yyyyMMddHHmmss });
            StringContent stringContent = new StringContent(map, Encoding.UTF8, Const.ApplicationJson);
            var response = client.PostAsync(url, stringContent).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// POST
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <typeparam name="TValue">TValue</typeparam>
        /// <param name="data">data</param>
        /// <returns>结果</returns>
        public TValue Post<T, TValue>(T data)
        {
            var result = Post(data);
            return JsonConvert.DeserializeObject<TValue>(result);
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="data">data</param>
        /// <returns>结果</returns>
        public string Put<T>(T data)
        {
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(name);
            client.DefaultRequestHeaders.Clear();
            if (this.Headers.Any())
            {
                foreach (var header in Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            else
            {
                client.DefaultRequestHeaders.Add(Const.Accept, Const.ApplicationJson);
            }
            var map = JsonConvert.SerializeObject(data, new JsonSerializerSettings { DateFormatString = Const.yyyyMMddHHmmss });
            StringContent stringContent = new StringContent(map, Encoding.UTF8, Const.ApplicationJson);
            var response = client.PutAsync(url, stringContent).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <typeparam name="TValue">TValue</typeparam>
        /// <param name="data">data</param>
        /// <returns>结果</returns>
        public TValue Put<T, TValue>(T data)
        {
            var result = Put(data);
            return JsonConvert.DeserializeObject<TValue>(result);
        }

        /// <summary>
        /// Patch
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="data">data</param>
        /// <returns>结果</returns>
        public string Patch<T>(T data)
        {
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(name);
            client.DefaultRequestHeaders.Clear();
            if (this.Headers.Any())
            {
                foreach (var header in Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            else
            {
                client.DefaultRequestHeaders.Add(Const.Accept, Const.ApplicationJson);
            }
            var map = JsonConvert.SerializeObject(data, new JsonSerializerSettings { DateFormatString = Const.yyyyMMddHHmmss });
            StringContent stringContent = new StringContent(map, Encoding.UTF8, Const.ApplicationJson);
            var response = client.PatchAsync(url, stringContent).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// Patch
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <typeparam name="TValue">TValue</typeparam>
        /// <param name="data">data</param>
        /// <returns>结果</returns>
        public TValue Patch<T, TValue>(T data)
        {
            var result = Patch(data);
            return JsonConvert.DeserializeObject<TValue>(result);
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <returns>结果</returns>
        public string Delete()
        {
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient(name);
            foreach (var header in Headers)
            {
                http.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = http.DeleteAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <returns>结果</returns>
        public T Delete<T>()
        {
            var response = Delete();
            return JsonConvert.DeserializeObject<T>(response);
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>结果</returns>
        public string Delete(IEnumerable<string> parameters)
        {
            url = url + GteParameters(parameters);
            return Get();
        }

        /// <summary>
        /// GET
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="parameters">参数</param>
        /// <returns>结果</returns>
        public T Delete<T>(IEnumerable<string> parameters)
        {
            var response = Get(parameters);
            return JsonConvert.DeserializeObject<T>(response);
        }

        /// <summary>
        /// GET参数转化
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>参数</returns>
        private string GteParameters(IEnumerable<string> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return string.Empty;
            }

            var param = string.Join("/", parameters);
            return "/" + param;
        }

        /// <summary>
        /// 遍历配置文件的根
        /// </summary>
        /// <param name="configuration">配置文件</param>
        /// <returns>结果集</returns>
        private static IEnumerable<IConfiguration> AsEnumerable(IConfiguration configuration)
        {
            foreach (var child in configuration.GetChildren())
            {
                yield return child;
            }
        }

        public static void Test()
        {
            //GET
            var httpClient1 = new HttpClientUtility("api/user", "localhost");
            var response1 = httpClient1.Get();
            var response2 = httpClient1.Get(new[] { "是非得失单独辅导" });

            //POST
            var httpClient2 = new HttpClientUtility("api/user", "localhost");
            var response3 = httpClient2.Post<TestClass>(new TestClass { Account = "xxx" });
            var response4 = httpClient2.Post<TestClass, dynamic>(new TestClass { Account = "xxx" });

            //PUT
            var httpClient3 = new HttpClientUtility("api/user", "localhost");
            var response5 = httpClient3.Put<TestClass>(new TestClass { Account = "xxx" });
            var response6 = httpClient3.Put<TestClass, dynamic>(new TestClass { Account = "xxx" });

            //Patch
            var httpClient4 = new HttpClientUtility("api/user", "localhost");
            var response7 = httpClient4.Patch<TestClass>(new TestClass { Account = "xxx" });
            var response8 = httpClient4.Patch<TestClass, dynamic>(new TestClass { Account = "xxx" });

            //GET
            var httpClient5 = new HttpClientUtility("api/user", "localhost");
            var response9 = httpClient5.Delete();
            var response10 = httpClient5.Delete(new[] { "xxsfdfedfdf" });

        }

        public class TestClass
        {
            /// <summary>
            /// 帐号
            /// </summary>
            public string Account { get; set; }

            /// <summary>
            /// 密码
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Ip
            /// </summary>
            public string Ip { get; set; }

            /// <summary>
            /// 浏览器
            /// </summary>
            public string Browser { get; set; }

            /// <summary>
            /// 跳转URL
            /// </summary>
            public string RedirectUrl { get; set; }
        }
    }
}
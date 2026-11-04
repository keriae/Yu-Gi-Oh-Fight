using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MDPro3
{
    public static class Json
    {
        /// <summary>
        /// 从 JSON 文件加载对象。
        /// 会自动检测目标类型 <typeparamref name="T"/> 中的可序列化成员，
        /// 如果 JSON 中缺失对应的属性，则用默认值补齐并重写文件。
        /// 若文件不存在，则创建包含默认值的 JSON 文件并返回默认实例。
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="filePath">JSON 文件路径</param>
        /// <returns>反序列化得到的对象</returns>
        public static T LoadFromFile<T>(string filePath) where T : new()
        {
            if (!File.Exists(filePath))
            {
                T defaultInstance = new();
                SaveToFile(filePath, defaultInstance);
                return defaultInstance;
            }

            try
            {
                string jsonText = File.ReadAllText(filePath);
                JObject jObject = JObject.Parse(jsonText);
                T defaultInstanceForMissing = new T();

                var resolver = new DefaultContractResolver();
                var contract = resolver.ResolveContract(typeof(T)) as JsonObjectContract;
                var properties = contract?.Properties;
                bool hasMissing = false;

                foreach (var prop in properties)
                {
                    if (!jObject.ContainsKey(prop.PropertyName))
                    {
                        object value = prop.ValueProvider.GetValue(defaultInstanceForMissing);
                        jObject.Add(new JProperty(prop.PropertyName, JToken.FromObject(value)));
                        hasMissing = true;
                    }
                }

                if (hasMissing)
                {
                    string updatedJson = jObject.ToString(Formatting.Indented);
                    File.WriteAllText(filePath, updatedJson);
                }

                return jObject.ToObject<T>();
            }
            catch
            {
                T defaultInstance = new();
                SaveToFile(filePath, defaultInstance);
                return defaultInstance;
            }
        }

        /// <summary>
        /// 将对象序列化为 JSON 并保存到指定路径。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">保存路径</param>
        /// <param name="obj">要保存的对象</param>
        public static void SaveToFile<T>(string filePath, T obj)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            string tmpPath = filePath + ".tmp";
            File.WriteAllText(tmpPath, json);

            if (File.Exists(filePath))
                File.Replace(tmpPath, filePath, null);
            else
                File.Move(tmpPath, filePath);
        }
    }
}
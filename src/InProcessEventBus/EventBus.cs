namespace InProcessEventBus
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class EventBus
    {
        public static readonly JsonSerializerSettings SerializeSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        public static readonly JsonSerializerSettings DeserializeSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        private readonly Dictionary<Type, List<HandleTypeInfo>> eventDataHandles = new Dictionary<Type, List<HandleTypeInfo>>();

        protected IServiceProvider serviceProvider;
        protected ILogger logger;

        public void ScanEventHandles(IServiceCollection services, params string[] assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = Assembly.Load(assemblyName);
                var handleTypes = assembly.GetTypes().Where(t => t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(EventHandle<>)).ToList();
                foreach (var handleType in handleTypes)
                {
                    var eventDataType = handleType.BaseType.GetGenericArguments()[0];
                    if (!eventDataHandles.TryGetValue(eventDataType, out var handleList))
                    {
                        handleList = new List<HandleTypeInfo>();
                        eventDataHandles[eventDataType] = handleList;
                    }
                    if (handleList.Any(o => o.HandleType == handleType))
                    {
                        continue;
                    }
                    var handleMethodInfo = handleType.GetMethod("Handle");
                    var handleTypeInfo = new HandleTypeInfo()
                    {
                        ClassName = $"{handleType.FullName}@{handleType.Assembly.FullName}",
                        HandleType = handleType,
                        HandleMethodInfo = handleMethodInfo
                    };
                    handleList.Add(handleTypeInfo);

                    services.AddTransient(handleType);
                }
            }
        }


        public virtual void Start(IServiceProvider serviceProvider, string loggerName = nameof(EventBus))
        {
            this.serviceProvider = serviceProvider;
            logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(loggerName);
        }

        public virtual void Stop()
        {

        }

        public virtual void Publish(EventData eventData)
        {
            var json = JsonConvert.SerializeObject(eventData, SerializeSettings);
            ManualPublish(json);
        }

        /// <summary>
        /// 手动补发消息
        /// </summary>
        /// <param name="json">必须是由 EventData的实现类 序列化成的带 "$type" 的json(使用new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })</param>
        public virtual void ManualPublish(string json)
        {
            EventData eventData;
            Type eventDataType;
            try
            {
                eventData = JsonConvert.DeserializeObject<EventData>(json, DeserializeSettings);
                eventDataType = eventData.GetType();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "无法解析的json: " + json);
                return;
            }

            if (eventDataHandles.TryGetValue(eventDataType, out var handleList))
            {
                foreach (var handle in handleList)
                {
                    if (eventData.TargetClassName == null || eventData.TargetClassName == handle.ClassName)
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            using (var scope = serviceProvider.CreateScope())
                            {
                                var handleTypeInstance = scope.ServiceProvider.GetRequiredService(handle.HandleType);
                                try
                                {
                                    handle.HandleMethodInfo.Invoke(handleTypeInstance, new object[] { eventData });
                                }
                                catch (Exception ex)
                                {
                                    var retryJson = CreateRetryJson(json, handle.ClassName, ex.InnerException.Message);
                                    logger.LogError(ex, retryJson);
                                }
                            }
                        });
                    }
                }
            }
        }


        private string CreateRetryJson(string json, string targetClassName, string exceptionMessage)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);
            jObject.Property("TargetClassName").Value = targetClassName;
            jObject.Add("ExceptionMessage", exceptionMessage);
            return jObject.ToString(Formatting.None);
        }

        private class HandleTypeInfo
        {
            public string ClassName { get; set; }

            public Type HandleType { get; set; }

            public MethodInfo HandleMethodInfo { get; set; }
        }
    }
}
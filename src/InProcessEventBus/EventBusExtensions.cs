namespace Microsoft.Extensions.DependencyInjection
{
    using InProcessEventBus;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Hosting;
    using System.Linq;

    public static class EventBusExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblyNames"> 调用方所在的 Assembly 会自动添加的</param>
        public static void AddInProcessEventBus(this IServiceCollection services, params string[] assemblyNames)
        {
            var assemblyNameOfCaller = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType.Assembly.FullName;
            assemblyNames = assemblyNames.ToArray().Union(new[] { assemblyNameOfCaller }).ToArray();

            var eventBus = new EventBus();
            eventBus.ScanEventHandles(services, assemblyNames);

            services.AddSingleton(eventBus);
        }

        public static void EventBusRegisterToApplicationLifetime(this IApplicationBuilder app, IHostApplicationLifetime hostApplicationLifetime)
        {
            hostApplicationLifetime.ApplicationStarted.Register(() =>
            {
                var eventBus = app.ApplicationServices.GetRequiredService<EventBus>();
                eventBus.Start(app.ApplicationServices);
            });

            hostApplicationLifetime.ApplicationStopping.Register(() =>
            {
                var eventBus = app.ApplicationServices.GetRequiredService<EventBus>();
                eventBus.Stop();
            });
        }
    }
}
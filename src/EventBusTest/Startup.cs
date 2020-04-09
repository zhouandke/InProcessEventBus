using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InProcessEventBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventBusTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddInProcessEventBus();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.EventBusRegisterToApplicationLifetime(hostApplicationLifetime);


        }
    }


    public class TimeEventData : EventData
    {
        public DateTime Time { get; set; }
    }

    public class TimeEventHandle : EventHandle<TimeEventData>
    {
        public TimeEventHandle(IServiceProvider serviceProvider)
        {
            //this.serviceProvider = serviceProvider;
        }

        public override void Handle(TimeEventData eventData)
        {
            Console.WriteLine("****************************************************TimeEventHandle:       " + eventData.Time);
        }
    }
}

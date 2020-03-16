using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using timrlink.net.Core.API;
using timrlink.net.Core.Service;

namespace timrlink.net.Core
{
    public abstract class BaseApplication
    {
        private readonly IServiceProvider serviceProvider;

        protected BaseApplication(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected ILoggerFactory LoggerFactory => GetService<ILoggerFactory>();
        protected IUserService UserService => GetService<IUserService>();
        protected ITaskService TaskService => GetService<ITaskService>();
        protected IWorkItemService WorkItemService => GetService<IWorkItemService>();
        protected IWorkTimeService WorkTimeService => GetService<IWorkTimeService>();
        protected IProjectTimeService ProjectTimeService => GetService<IProjectTimeService>();
        protected IConfiguration Configuration => GetService<IConfiguration>();

        protected T GetService<T>() => serviceProvider.GetService<T>();
    }
}

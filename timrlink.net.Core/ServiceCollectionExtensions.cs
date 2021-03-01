using System;
using System.ServiceModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using timrlink.net.Core.API;
using timrlink.net.Core.Service;

namespace timrlink.net.Core
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddTimrLink(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddScoped<IUserService, UserService>()
                .AddScoped<IWorkItemService, WorkItemService>()
                .AddScoped<IWorkTimeService, WorkTimeService>()
                .AddScoped<ITaskService, TaskService>()
                .AddScoped<IProjectTimeService, ProjectTimeService>()
                .AddScoped(provider => ServiceFactory.BuildTimrSync(provider));
        }

    }
}

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.DragonFruit;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using timrlink.net.CLI.Actions;
using timrlink.net.Core;
using timrlink.net.Core.Service;

[assembly: InternalsVisibleTo("timrlink.net.CLI.Test")]

namespace timrlink.net.CLI
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            return await new CommandLineBuilder()
                .UseHost(hostBuilder => hostBuilder
                    .ConfigureAppConfiguration(configurationBuilder => configurationBuilder
                        .AddJsonFile("config.json")
                    )
                    .ConfigureLogging((context, loggingBuilder) => loggingBuilder
                        .AddSerilog(new LoggerConfiguration()
                            .ReadFrom.Configuration(context.Configuration)
                            .WriteTo.RollingFile("timrlink.net.{Date}.log")
                            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.ConsoleTheme.None)
                            .CreateLogger())
                    )
                    .ConfigureServices(serviceCollection => serviceCollection
                        .AddHostedService<ApplicationImpl>()
                        .AddTimrLink()
                    )
                )
                .Build()
                .InvokeAsync(args);
        }
    }

    internal class ApplicationImpl : IHostedService
    {
        private readonly string[] args;
        private readonly ILoggerFactory loggerFactory;
        private readonly IUserService userService;
        private readonly ITaskService taskService;
        private readonly IProjectTimeService projectTimeService;

        public ApplicationImpl(IConfiguration configuration, ILoggerFactory loggerFactory, IUserService userService, ITaskService taskService, IProjectTimeService projectTimeService)
        {
            this.args = args;
            this.loggerFactory = loggerFactory;
            this.userService = userService;
            this.taskService = taskService;
            this.projectTimeService = projectTimeService;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var filenameArgument = new Argument<string>("filename");
            filenameArgument.LegalFilePathsOnly();

            var projectTimeCommand = new Command("projecttime", "Import project times");
            projectTimeCommand.AddAlias("pt");
            projectTimeCommand.AddArgument(filenameArgument);
            projectTimeCommand.Handler = CommandHandler.Create<string>(ImportProjectTime);

            var updateTasks = new Option<bool>("--update", "Update existing tasks with same externalId");
            updateTasks.AddAlias("-u");
            updateTasks.Argument.SetDefaultValue(true);
            updateTasks.Argument.ArgumentType = typeof(bool);

            var taskCommand = new Command("task", "Import tasks");
            taskCommand.AddAlias("t");
            taskCommand.AddArgument(filenameArgument);
            taskCommand.AddOption(updateTasks);
            taskCommand.Handler = CommandHandler.Create<string, bool>(ImportTasks);

            var exportProjectTimeCommand = new Command("export-projecttime", "Export Project times");
            exportProjectTimeCommand.AddOption(new Option<string>("connectionstring"));
            exportProjectTimeCommand.Handler = CommandHandler.Create<string>(ExportProjectTime);

            var rootCommand = new RootCommand("timrlink command line interface")
            {
                projectTimeCommand,
                taskCommand,
                exportProjectTimeCommand,
            };
            rootCommand.Name = "timrlink";
            rootCommand.TreatUnmatchedTokensAsErrors = true;
            
            await rootCommand.InvokeAsync(args);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        private async Task ImportProjectTime(string filename)
        {
            ImportAction action;
            switch (Path.GetExtension(filename))
            {
                case ".csv":
                    action = new ProjectTimeCSVImportAction(loggerFactory, filename, taskService, projectTimeService);
                    break;
                case ".xlsx":
                    action = new ProjectTimeXLSXImportAction(loggerFactory, filename, taskService, projectTimeService);
                    break;
                default:
                    throw new ArgumentException($"Unsupported file type '{filename}' - use .csv or .xlsx!");
            }

            await action.Execute();
        }

        private async Task ImportTasks(string filename, bool update)
        {
            await new TaskImportAction(loggerFactory, filename, update, taskService).Execute();
        }

        private async Task ExportProjectTime(string connectionString)
        {
            await new ProjectTimeDatabaseExportAction(loggerFactory, connectionString, userService, taskService, projectTimeService).Execute();
        }
    }
}

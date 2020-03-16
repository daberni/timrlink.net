using System;
using System.CommandLine;
using System.CommandLine.Builder;
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
                        .AddTimrLink()
                    )
                )
                .Build()
                .InvokeAsync(args);
        }
    }

    internal class ApplicationImpl : BaseApplication, IHostedService
    {
        private readonly string[] args;

        public ApplicationImpl(string[] args, IServiceProvider provider) : base(provider)
        {
            this.args = args;
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
                    action = new ProjectTimeCSVImportAction(LoggerFactory, filename, TaskService, ProjectTimeService);
                    break;
                case ".xlsx":
                    action = new ProjectTimeXLSXImportAction(LoggerFactory, filename, TaskService, ProjectTimeService);
                    break;
                default:
                    throw new ArgumentException($"Unsupported file type '{filename}' - use .csv or .xlsx!");
            }

            await action.Execute();
        }

        private async Task ImportTasks(string filename, bool update)
        {
            await new TaskImportAction(LoggerFactory, filename, update, TaskService).Execute();
        }

        private async Task ExportProjectTime(string connectionString)
        {
            await new ProjectTimeDatabaseExportAction(LoggerFactory, connectionString, UserService, TaskService, ProjectTimeService).Execute();
        }
    }
}

// See https://aka.ms/new-console-template for more information

using CS2LuaDoc.Cli;
using NLog;
using NLog.Config;
using NLog.Targets;
using Spectre.Console.Cli;

var config = new LoggingConfiguration();
var consoleTarget = new ColoredConsoleTarget()
{
    Name = "console",
    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}",
};
config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "*");
LogManager.Configuration = config;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<GenerateLuaDocCommand>("generate");
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

return app.Run(args);
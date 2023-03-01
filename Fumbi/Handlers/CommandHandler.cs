using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;

namespace Fumbi.Handlers
{
    public class CommandHandler
    {
        public static CommandService _commands;
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;

        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(CommandHandler));

        public CommandHandler(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _commands.CommandExecuted += CommandExecutedAsync;
        }

        private static async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (result.IsSuccess)
            {
                Logger.Information("Command {command} used by {name}({uid}).", context.Message.Content, context.User.Username, context.User.Id);
                return;
            }

            switch (result.Error)
            {
                case CommandError.BadArgCount:
                case CommandError.ParseFailed:
                    Logger.Information("{name}({uid}) used a command wrong -> {command}.", context.User.Username, context.User.Id, context.Message.Content);
                    await context.Channel.SendMessageAsync("Wrong usage.");
                    break;

                default:
                    await context.Channel.SendMessageAsync($"Something went wrong -> {result.ToString()}");
                    Logger.Error($"Something went wrong -> {result.ToString()}");
                    break;
            }
        }
    }
}

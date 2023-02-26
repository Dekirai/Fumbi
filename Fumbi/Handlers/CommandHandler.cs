using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fumbi.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private static async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message) || message.Author.Id == _client.CurrentUser.Id ||
                message.Author.IsBot || message.Channel.GetType() == typeof(SocketDMChannel))
                return;

            var user = await UserService.FindUserAsync(message.Author.Id, message.Author.Username);

            if (await user.MessageRecievedAsync((uint)message.Content.Length, message.Author.Username))
            {
                await user.UpdateUserAsync();

                var guild = (message.Channel as SocketGuildChannel).Guild;
                if (guild.GetUser(_client.CurrentUser.Id).Roles.Where(r => r.Permissions.ManageRoles == true).ToList().Count != 0)
                {
                    var guilduser = (message.Author as IGuildUser);

                    if (user.Level == 1)
                    {
                        var rookie = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Rookie");
                        if (rookie != null)
                            await guilduser.AddRoleAsync(rookie);
                    }
                    else if (user.Level == 20)
                    {
                        var rookie = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Rookie");
                        var ama = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Amateur");
                        if (rookie != null && ama != null)
                        {
                            await guilduser.RemoveRoleAsync(rookie);
                            await guilduser.AddRoleAsync(ama);
                        }
                    }
                    else if (user.Level == 40)
                    {
                        var ama = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Amateur");
                        var semipro = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Semi-Pro");
                        if (ama != null && semipro != null)
                        {
                            await guilduser.RemoveRoleAsync(ama);
                            await guilduser.AddRoleAsync(semipro);
                        }
                    }
                    else if (user.Level == 60)
                    {
                        var semipro = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Semi-Pro");
                        var pro = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Pro");
                        if (semipro != null && semipro != null)
                        {
                            await guilduser.RemoveRoleAsync(semipro);
                            await guilduser.AddRoleAsync(pro);
                        }
                    }
                    else if (user.Level == 80)
                    {
                        var pro = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Pro");
                        var s4 = guild.Roles.FirstOrDefault(x => x.Name.ToString() == "S4");
                        if (pro != null && s4 != null)
                        {
                            await guilduser.RemoveRoleAsync(pro);
                            await guilduser.AddRoleAsync(s4);
                        }
                    }
                }

                if ((user.Level < 20 && user.Level % 4 == 0) || user.Level >= 20)
                {
                    using (var image = await user.DrawLevelUpImage())
                    {
                        string extension = image.Length < 700000 ? ".png" : ".gif";

                        await message.Channel.SendFileAsync(image, "levelup" + extension);
                    }
                }

                Logger.Information("Level up for {name}({uid}), new level -> {newlevel}", message.Author.Username, message.Author.Id, user.Level);
            }

            int argPos = 0;
            if (!message.HasStringPrefix(Config.Instance.BotPrefix, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(context, argPos, _services);
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
                case CommandError.UnknownCommand:
                    await context.Channel.SendMessageAsync("Command not found.");
                    Logger.Information("Non-existing command {command} used by {name}({uid}).", context.Message.Content, context.User.Username, context.User.Id);
                    break;

                case CommandError.UnmetPrecondition:
                    Logger.Warning("Command {command} is on cooldown for {name}({uid}).", context.Message, context.User.Username, context.User.Id);
                    await context.Channel.SendMessageAsync(result.ToString().Substring(19, result.ToString().Length - 19));
                    break;

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

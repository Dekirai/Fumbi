using Dapper.FastCrud;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fumbi.Handlers;
using Fumbi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Fumbi
{
    public class Bot
    {
        private static DiscordSocketClient _client;
        private static string s_token;
        private static IServiceProvider _services;
        private static InteractionService _interactionService;

        private static readonly ILogger Logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Bot));

        public static void Start(string token)
        {
            s_token = token;

            StartAsync().GetAwaiter().GetResult();
        }

        private static async Task StartAsync()
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            _client = new DiscordSocketClient(config);
            _services = new ServiceCollection()
               .AddSingleton(_client)
               .AddSingleton<InteractionService>()
               .BuildServiceProvider();

            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;

            _client.Ready += _client_Ready;

            await _client.LoginAsync(TokenType.Bot, s_token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        public static async Task _client_Ready()
        {
            List<ApplicationCommandProperties> deleteGlobalCommands = new();
            var time = $"{DateTime.Now.ToLongTimeString()}";

            _interactionService = new InteractionService(_client);
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.RegisterCommandsGloballyAsync();
            await _client.SetGameAsync("Use / for commands!");
            _client.InteractionCreated += async interaction =>
            {
                var scope = _services.CreateScope();
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
            };
        }

        public static async Task _client_SlashCommandExecuted(SocketSlashCommand command)
        {
            var time = $"{DateTime.Now.ToLongTimeString()}";
            var user = $"{command.User.Username}#{command.User.Discriminator}";
            var userid = $"{command.User.Username}#{command.User.Id}";
            Logger.Information("User {0} issued command {2}.", user, userid, command.CommandName);
        }

        private static async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message) || message.Author.Id == _client.CurrentUser.Id ||
                message.Author.IsBot || message.Channel.GetType() == typeof(SocketDMChannel))
                return;

            var user = await UserService.FindUserAsync(message.Author.Id, message.Author.GlobalName);

            if (await user.MessageRecievedAsync((uint)message.Content.Length, message.Author.GlobalName))
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

                Logger.Information("Level up for {name}({uid}), new level -> {newlevel}", message.Author.GlobalName, message.Author.Id, user.Level);
            }

            int argPos = 0;
        }

        private static async Task LogAsync(LogMessage log)
        {
            if (log.Message.StartsWith("Executed"))
                return;

            if (log.Exception is CommandException cmdException)
            {
                await cmdException.Context.Channel.SendMessageAsync("Something went terribly wrong -> " + cmdException.Message);
                Logger.Error("{command} failed to execute by {user}({uid}).", cmdException.Context.Message, cmdException.Context.User.Username, cmdException.Context.User.Id);
            }

            else
            {
                switch (log.Severity)
                {
                    case LogSeverity.Critical:
                    case LogSeverity.Error:
                        Logger.Error(log.Message);
                        break;

                    case LogSeverity.Warning:
                        Logger.Warning(log.Message);
                        break;

                    case LogSeverity.Debug:
                        Logger.Debug(log.Message);
                        break;

                    default:
                        Logger.Information(log.Message);
                        break;
                }
            }
        }

        public static void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private static async Task StopAsync()
        {
            await _client.LogoutAsync();
            await _client.StopAsync();

            _client.Dispose();
        }
    }
}

﻿using Discord;
using Discord.Interactions;
using Fumbi.Handlers;
using Fumbi.Services;
using Serilog;
using Serilog.Core;
using System.Data;

namespace Fumbi.Modules
{
    public class MainModule : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(MainModule));

        [SlashCommand("profile", "Displays the profile of the mentioned user.")]
        public async Task ProfileCommand(IUser User)
        {
            if (User.GetAvatarUrl() == null)
            {
                await RespondAsync("User does not have an avatar.");
                Logger.Information("/profile used by {name}({uid}) with empty avatar(mentioned)", Context.User.Username, Context.User.Id);
                return;
            }

            var user = await UserService.FindUserAsync(User.Id, User.Username);

            using (var image = await user.DrawProfileImageAsync(await UserService.CalculateRankAsync(User.Id), User.GetAvatarUrl()))
            {
                await DeferAsync();
                string extension = image.Length < 1000000 ? ".png" : ".gif";

                await FollowupWithFileAsync(image, "profile" + extension);
            }
        }

        [SlashCommand("avatar", "Displays the avatar of the mentioned user.")]
        public async Task AvatarCommand(IUser User)
        {
            await DeferAsync();
            await FollowupAsync(User.GetAvatarUrl().Replace("size=128", "size=2048") ?? User.GetDefaultAvatarUrl());
        }

        [SlashCommand("rank", "Displays the current rank of the mentioned user.")]
        public async Task RankCommand(IUser User)
        {
            if (User.GetAvatarUrl() == null)
            {
                await RespondAsync("User does not have an avatar.");
                Logger.Information("/rank used by {name}({uid}) with empty avatar(mentioned)", Context.User.Username, Context.User.Id);
                return;
            }

            var user = await UserService.FindUserAsync(User.Id, User.Username);

            using (var image = await user.DrawRankImageAsync(await UserService.CalculateRankAsync(User.Id), User.GetAvatarUrl()))
            {
                string extension = image.Length < 700000 ? ".png" : ".gif";
                await RespondWithFileAsync(image, "rank" + extension);
            }
        }

        [SlashCommand("shop", "Displays the items available in the shop.")]
        public async Task ShopCommand()
        {
            var embed = new EmbedBuilder();
            embed.WithDescription("Write /buy + themes id to buy, each theme is 250k pen");
            embed.WithTitle("Profile theme shop");
            foreach (var theme in Enum.GetValues(typeof(Shop.ProfileTheme)).Cast<Shop.ProfileTheme>().Skip(1).ToArray())
                embed.AddField(theme.ToString(), theme.GetHashCode(), true);

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("buy", "Buy an item from the shop")]
        public async Task BuyCommand(byte Theme)
        {

            var user = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            if (Theme <= 0 || Theme > 9)
            {
                await RespondAsync("Theme not found.");
                Logger.Information("/buy used by {name}({uid}) with unknown theme -> {theme}", Context.User.Username, Context.User.Id, Theme);
                return;
            }

            if (user.Pen < 250000)
            {
                await RespondAsync("Not enough pen.");
                Logger.Information("/buy used by {name}({uid}) with insufficient pen -> {pen}", Context.User.Username, Context.User.Id, user.Pen);
                return;
            }

            var inventory = await InventoryService.FindInventory(Context.User.Id);

            var i = InventoryService.FindTheme(Theme);

            if (inventory.CheckInventory(i))
            {
                await RespondAsync("You already have that theme.");
                Logger.Information("/buy used by {name}({uid}) with already acquired theme -> ", Context.User.Username, Context.User.Id, i.GetHashCode());
                return;
            }

            inventory.AddItem(i.ToString());
            await inventory.UpdateInventoryAsync();

            user.ProfileTheme = (byte)(i.GetHashCode());
            user.Pen -= 250000;
            await user.UpdateUserAsync();

            await RespondAsync("Theme successfully bought!");
        }

        [SlashCommand("use", "Equip an item from your inventory.")]
        public async Task UseCommand(byte Theme)
        {
            var user = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            if (Theme < 0 || Theme > 9)
            {
                await RespondAsync("Theme not found.");
                Logger.Information("/use used by {name}({uid}) with unknown theme -> {theme}", Context.User.Username, Context.User.Id, Theme);
                return;
            }

            var inventory = await InventoryService.FindInventory(Context.User.Id);

            var i = InventoryService.FindTheme(Theme);

            if (!inventory.CheckInventory(i))
            {
                await RespondAsync("You dont have that theme.");
                Logger.Information("/use used by {name}({uid}) without the theme -> {theme}", Context.User.Username, Context.User.Id, i.GetHashCode());
                return;
            }

            user.ProfileTheme = (byte)(i.GetHashCode());
            await user.UpdateUserAsync();

            await RespondAsync("Theme successfully equipped!");
        }

        [SlashCommand("daily", "Claim your daily reward.")]
        public async Task DailyCommand()
        {
            var user = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            if (user.LastDaily == null || UserService.CheckDaily(DateTime.Parse(user.LastDaily)))
            {
                uint penGain = UserService.CalculateDaily() * 1000;

                user.Pen += penGain;
                user.LastDaily = DateTime.Now.ToString();
                if (user.Pen > 500000000)
                    user.Pen = 500000000;
                await user.UpdateUserAsync();

                using (var image = user.DrawDailyImage(penGain))
                {
                    await RespondWithFileAsync(image, "daily.png");
                }
                return;
            }

            double cd = (24 * 60 - (DateTime.Now - DateTime.Parse(user.LastDaily)).TotalMinutes);

            uint h = (uint)(cd / 60);
            uint m = (uint)(cd % 60);

            if (h != 0)
            {
                await RespondAsync("You can claim your daily in " + h + "h " + m + "m");
                Logger.Information("/daily used by {name}({uid}) while on cooldown, cooldown left -> {hours}h {minutes}m", Context.User.Username, Context.User.Id, h, m);
            }
            else
            {
                await RespondAsync("You can claim your daily in " + m + "m");
                Logger.Information("/daily used by {name}({uid}) while on cooldown, cooldown left -> {minutes}m", Context.User.Username, Context.User.Id, m);
            }
        }

        [SlashCommand("dailyexp", "Check how much experience you can obtain for today.")]
        public async Task DailyExpCommand()
        {
            var user = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            var lastdaily = DateTime.Parse(user.DailyExp.Remove(0, 8));
            uint totalexp = uint.Parse(user.DailyExp.Remove(5));

            double cd = (24 * 60 - (DateTime.Now - lastdaily).TotalMinutes);

            uint h = (uint)(cd / 60);
            uint m = (uint)(cd % 60);

            if (totalexp >= 75000 && (DateTime.Now - lastdaily).Days < 1)
            {
                if (h != 0)
                {
                    await RespondAsync("You have capped your daily exp limit and it will reset in " + h + "h " + m + "m");
                }
                else
                {
                    await RespondAsync("You have capped your daily exp limit and it will reset in " + m + "m");
                }
            }

            if (totalexp < 75000)
                await RespondAsync(75000 - totalexp + " exp remaining for today");
        }

        [SlashCommand("top", "Displays the top ranking.")]
        public async Task TopCommand()
        {
            var rankList = await UserService.GetTopListAsync();

            var embed = new EmbedBuilder();
            embed.WithTitle("Rank list");
            embed.WithColor(Color.Purple);

            uint count = 0;
            foreach (var player in rankList)
            {
                count++;

                embed.AddField("#" + count.ToString() + " " + player.Name, "Exp: " + player.Exp, true);

                if (count == 10)
                    break;
            }

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("gamble", "Gamble with your PEN for a chance to obtain more PEN.")]
        public async Task GambleCommand(ulong Amount)
        {
            var user = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            if (Amount > user.Pen)
            {
                await RespondAsync($"You don't have enough {Emotes.Emotes.PEN}.");
                Logger.Information("/gamble used by {name}({uid}) with insufficient pen -> {currentPen}({amount})", Context.User.Username, Context.User.Id, user.Pen, Amount);
                return;
            }

            if (UserService.GambleIsWon() == true)
            {
                uint multiplier = UserService.GambleCalculateMultiplier();

                user.Pen += Amount * (multiplier - 1);
                if (user.Pen > 500000000)
                {
                    user.Pen = 500000000;
                    await RespondAsync($"You have received the limit of the obtainable {Emotes.Emotes.PEN} which is 500.000.000!");
                }
                else
                    await RespondAsync($"Congratulations, you won {Amount * (multiplier - 1):n0}{Emotes.Emotes.PEN}!\nYour new balance is {user.Pen:n0}{Emotes.Emotes.PEN}.");
                await user.UpdateUserAsync();
                
                Logger.Information("/gamble won by {name}({uid}) -> {amount} pen", Context.User.Username, Context.User.Id, Amount * (multiplier - 1));
                return;
            }
            else
            {
                user.Pen -= Amount;
                if (user.Pen > 500000000)
                    user.Pen = 500000000;
                await user.UpdateUserAsync();
                await RespondAsync($"You lost {Amount:n0}{Emotes.Emotes.PEN}! 😭\nYour new balance is {user.Pen:n0}{Emotes.Emotes.PEN}.");
                Logger.Information("/gamble lost by {name}({uid}) -> {amount} pen", Context.User.Username, Context.User.Id, Amount);
            }
        }

        [SlashCommand("balance", "Displays your current amount of PEN.")]
        public async Task BalanceCommand()
        {
            var user = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            await RespondAsync($"Your current balance is {user.Pen:n0}{Emotes.Emotes.PEN}");
        }

        [SlashCommand("givepen", "Gives a specific amount of PEN to a user. Owner only.")]
        public async Task PenCommand(IUser User, ulong Amount)
        {
            if (Context.User.Id != Config.Instance.OwnerId)
            {
                Logger.Warning("/givepen used by {name}({uid}) with no permission.", Context.User.Username, Context.User.Id);
                await RespondAsync("No permission!");
                return;
            }
            var user = await UserService.FindUserAsync(User.Id, User.Username);

            user.Pen += Amount;
            if (user.Pen > 500000000)
                user.Pen = 500000000;
            await user.UpdateUserAsync();
        }

        [SlashCommand("transfer", "Transfer a specific amount of PEN to another user from your balance.")]
        public async Task TransferCommand(IUser User, ulong Amount)
        {
            var transferrer = await UserService.FindUserAsync(Context.User.Id, Context.User.Username);

            if (transferrer.Pen < Amount)
            {
                await RespondAsync($"You don't have enough {Emotes.Emotes.PEN}");
                Logger.Information("/transfer used by {name}({uid}) with insufficient pen -> {currentPen}({amount})", Context.User.Username, Context.User.Id, transferrer.Pen, Amount);
                return;
            }

            transferrer.Pen -= Amount;
            await transferrer.UpdateUserAsync();

            var transferee = await UserService.FindUserAsync(User.Id, User.Username);

            transferee.Pen += Amount;
            if (transferee.Pen > 500000000)
                transferee.Pen = 500000000;
            await transferee.UpdateUserAsync();

            await RespondAsync("Transfer successful!");
        }

        [SlashCommand("giveexp", "Gives Experience to a user. Owner only.")]
        public async Task ExpCommand(IUser User, uint Amount)
        {
            if (Context.User.Id != Config.Instance.OwnerId)
            {
                Logger.Warning("/giveexp used by {name}({uid}) with no permission.", Context.User.Username, Context.User.Id);
                await RespondAsync("No permission!");
                return;
            }

            var user = await UserService.FindUserAsync(User.Id, User.Username);

            user.Exp += Amount;
            user.Level = UserService.CalculateLevel(user.Exp);
            await user.UpdateUserAsync();
        }
    }
}

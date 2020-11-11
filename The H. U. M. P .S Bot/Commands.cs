using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace THUMPSBot
{
    [Group("Tests")]
    [RequireOwner(ErrorMessage = "This is a test command and is not intended for public use.", Group = "Permission")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        SaveTest save = new SaveTest();

        [Command("test")]
        [Summary("A test command")]
        public async Task Test() => await ReplyAsync("test");

        [Command("inputTest")]
        [Summary("Tests input by sending a echo")]
        public async Task InputTest([Remainder]string input = "No input provided") => await ReplyAsync(input);

        [Command("save")]
        [Summary("Test saving mechanics")]
        public async Task SaveTest([Remainder] string input)
        {
            await save.Save(input);
            await ReplyAsync("Saved: \"" + input + "\" to text file. Do !load to view all saved items");
        }
        [Command("load")]
        [Summary("Test loading mechanics")]
        public async Task LoadTest()
        {
            await ReplyAsync(save.Load().Result);
        }
        [Command("colour")]
        [Summary("Test embed color")]
        public async Task SendColour()
        {
            await ReplyAsync(embed: new EmbedBuilder { 
                Description = "yellow",
                Color = new Color(230, 200, 0)
            }.Build());
            await ReplyAsync(embed: new EmbedBuilder
            {
                Description = "orange",
                Color = Color.Orange//new Color(230, 95, 30)
            }.Build());
            await ReplyAsync(embed: new EmbedBuilder
            {
                Description = "darkorange",
                Color = new Color(255, 70, 0)
            }.Build());
            await ReplyAsync(embed: new EmbedBuilder
            {
                Description = "red",
                Color = new Color(235, 0, 0)
            }.Build());
        }
    }

    [Group("indev")]
    [RequireOwner(ErrorMessage = "This command are not ready yet")]
    public class InDevModule : ModuleBase<SocketCommandContext>
    {
        
    }
    
    //Finished Commands
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        Mod_Actions actions = new Mod_Actions();
        
        [Command("warn")]
        [RequireUserPermission(GuildPermission.KickMembers, ErrorMessage = "You are not allowed to use this command because you are not a moderator", Group = "Permission", NotAGuildErrorMessage = "This can only be used in a guild")]
        [Summary("Warns a user and logs it")]
        public async Task Warn(IGuildUser user, [Remainder] string reason = "No reason provided")
        {
            //only allow the user in this context warn people who have a lower role than them
            if (Tools_and_Functions.GetHighestRolePosition(user, Context.Client) < Tools_and_Functions.GetHighestRolePosition(Context.Client.GetGuild(597798914606759959).GetUser(Context.User.Id), Context.Client) /*|| Context.User.Id == 374284798820352000*/)
            {
                await actions.LogInfraction(user, Context.User, Context.Channel, reason);
                //Build embeds
                Embed warnReplyEmbed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = user.GetAvatarUrl(),
                        Name = user.Username + " has been warned!"
                    },
                    Description = reason,
                    Color = new Color(230, 200, 0)
                    //add more statistics in later update
                }.Build();
                //reply to executor
                await ReplyAsync(embed: warnReplyEmbed);
            }
            else
            {
                await ReplyAsync("You cannot warn a user that has a equal or higher role than you");
            }
        }

        [Command("infractions")]
        [RequireContext(contexts: ContextType.Guild, ErrorMessage = "This command is for guilds only", Group = "Guild Command")]
        [Summary("Finds infractions")]
        public async Task Infractions(IGuildUser user)
        {
            Embed infractions = await actions.FindInfractions(user, Context.Client);
            //The following zombie code is for spam purposes. In case of spam remove comments
            //if (Context.Client.GetGuild(597798914606759959).GetUser(Context.User.Id).GuildPermissions.KickMembers)
                await ReplyAsync(embed: infractions);
            //else
                //await Context.User.SendMessageAsync(embed: infractions);
        }
    }

    [Group ("DB")]
    [RequireServerOwner]
    public class UserDatabase : ModuleBase<SocketCommandContext>
    {
        [Command("Update")]
        [Summary("Rebuilds the user and status data table.")]
        public async Task Update()
            => await new User_Flow_control(Context.Client).UpdateDB();

        [Command("AddUser")]
        [Summary("Adds a new user to the database.")]
        public async Task AddSocketUser(SocketGuildUser user, [Remainder] string status = "New User")
            => await AddUser(user.Id, status);
        [Command("AddUser")]
        [Summary("Adds a new user to the database.")]
        public async Task AddUserId(ulong userId, [Remainder] string status = "New User")
            => await AddUser(userId, status);
        private async Task AddUser(ulong userId, string status)
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            // Format the status correctly
            status = status.ToLower();
            bool caps = true;
            string formattedStatus = "";
            foreach (char c in status)
            {
                if (caps)
                    formattedStatus += char.ToUpper(c);
                else
                    formattedStatus += c;

                caps = false;

                if (c == ' ')
                    caps = true;
            }

            // Ensure it's valid
            switch (formattedStatus)
            {
                case "Whitelisted":
                case "Blacklisted":
                case "Quarantined":
                case "New User":
                    break;
                default:
                    await ReplyAsync(status + "is not a valid status");
                    return;
            }

            await ReplyAsync($"Adding user <@!{userId}> with the id of {userId} as {formattedStatus}");

            if (await userFlow.AddUser(userId, status))
                await ReplyAsync($"Successfully added <@!{userId}> as {status}");
            else
            {
                await ReplyAsync("A database entry for " + Context.Client.Rest.GetUserAsync(userId).Result.Mention + " already exists, updating user entry...");
                await userFlow.UpdateUser(userId, status);
                await ReplyAsync($"Successfully updated <@!{userId}> as {status}");
            }
        }

        [Command("Blacklist")]
        [Summary("Blacklists a user")]
        public async Task BlacklistSocketUser(SocketGuildUser user, [Remainder] string reason = "")
        => await BlacklistUser(user.Id, reason);
        [Command("Blacklist")]
        [Summary("Blacklists a user")]
        public async Task BlacklistUserId(ulong userId, [Remainder]string reason = "")
            => await BlacklistUser(userId, reason);
        private async Task BlacklistUser(ulong userId, string reason)
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            if (await Context.Client.Rest.GetUserAsync(userId) == null)
                await ReplyAsync("I could not find the user you were looking for.");

            if (await userFlow.AddUser(userId, "Blacklisted"))
                await ReplyAsync($"Successfully added <@!{userId}> as Blacklisted");
            else
            {
                await userFlow.UpdateUser(userId, "Blacklisted");
                await ReplyAsync($"Successfully updated <@!{userId}> as Blacklisted");
            }

            //Now ban the user
            await Context.Guild.AddBanAsync(Context.Client.Rest.GetUserAsync(userId).Result, reason: reason == "" ? "Blacklisted" : reason);
        }

        [Command("Quarantine")]
        [Summary("Quarantines a user")]
        public async Task QuarantineSocketUser(SocketGuildUser user)
            => await QuarentineUser(user.Id);
        [Command("Quarantine")]
        [Summary("Quarantines a user")]
        public async Task QuarantineUserId(ulong userId)
            => await QuarentineUser(userId);
        private async Task QuarentineUser(ulong userId)
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            if (await userFlow.AddUser(userId, "Quarantined"))
                await ReplyAsync($"Successfully added <@!{userId}> as Quarantined");
            else
            {
                await userFlow.UpdateUser(userId, "Quarantined");
                await ReplyAsync($"Successfully updated <@!{userId}> as Quarantined");
            }

            SocketGuildUser user = Context.Guild.GetUser(userId);
            if (user == null)
                return;

            // Now remove all other roles and add quarantined role
            foreach (IRole role in user.Roles)
            {
                if (role.Name == "@everyone")
                    continue;
                await user.RemoveRoleAsync(role);
            }
            await user.AddRoleAsync(Context.Guild.GetRole(645413078405611540));
        }

        [Command("Whitelist")]
        [Summary("Whitelists a user")]
        public async Task WhitelistSocketUser(SocketGuildUser user)
            => await WhitelistUser(user.Id);
        [Command("Whitelist")]
        [Summary("Whitelists a user")]
        public async Task WhitelistUserId(ulong userId)
            => await WhitelistUser(userId);
        private async Task WhitelistUser(ulong userId)
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            if (await userFlow.AddUser(userId, "Whitelisted"))
                await ReplyAsync($"Successfully added <@!{userId}> as Whitelisted");
            else
            {
                await userFlow.UpdateUser(userId, "Whitelisted");
                await ReplyAsync($"Successfully updated <@!{userId}> as Whitelisted");
            }

            // Remove ban if banned
            try
            {
                await Context.Guild.RemoveBanAsync(userId);
                await ReplyAsync($"Unbanned <@!{userId}>");
            }
            catch (Exception e)
            {
                if (e.HResult != -2146233088)
                {
                    Console.WriteLine(e.HResult);
                    await ReplyAsync("A problem occurred while attempting to unban this user");
                }
            }

            // Remove quarantine if quarantined and give I just returned role
            SocketGuildUser guildUser = Context.Guild.GetUser(userId);
            if (guildUser != null)
            {
                await guildUser.RemoveRoleAsync(Context.Guild.GetRole(645413078405611540));
                await guildUser.AddRoleAsync(Context.Guild.GetRole(665758685464625153));
                await ReplyAsync($"Updated roles for <@!{userId}>");
            }
        }
    }
    
    public class Miscellaneous : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Summary("A command to find all available commands to the user")]
        public async Task Help()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            CommandService commandService = new CommandService();
            await commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);

            await ReplyAsync("Here are all my commands that you can use!");
            
            List<string> uselessModules = new List<string>();//list for unusable commands
            uselessModules.Add("TestModule");
            uselessModules.Add("InDevModule");

            foreach (ModuleInfo module in commandService.Modules)
            {
                if (uselessModules.Contains(module.Name))
                {
                    embedBuilder.Title = module.Name;
                    foreach (CommandInfo command in module.Commands)
                    {
                        // Get the command Summary attribute information
                        string embedFieldText = command.Summary ?? "No description available\n";

                        embedBuilder.AddField("!" + command.Name, embedFieldText);
                    }
                    await ReplyAsync(embed: embedBuilder.Build());
                }
            }
        }
    }
}

using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace THUMPSBot
{
    [Group("Tests")]
    [RequireOwner(ErrorMessage = "This is a test command and is not intended for public use.", Group = "Permmision")]
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
        [RequireUserPermission(GuildPermission.KickMembers, ErrorMessage = "You are not allowed to use this command because you are not a moderator", Group = "Permision", NotAGuildErrorMessage = "This can only be used in a guild")]
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
                    Color = Color.Orange
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

    [Group ("DB")] //indev
    [RequireServerOwner]
    public class UserDatabase : ModuleBase<SocketCommandContext>
    {
        [Command("Update")]
        [Summary("Rebuilds the user and status data table.")]
        public async Task Update()
        {
            await new User_Flow_control(Context.Client).UpdateDB();
        }

        [Command("AddUser")]
        [Summary("Adds a new user to the database.")]
        public async Task AddUser(Discord.WebSocket.SocketGuildUser user, string status = "New User")
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            if (await userFlow.AddUser(user.Id, status))
                await ReplyAsync($"Successfully added <@!{user.Id}> as {status}");
            else
            {
                await ReplyAsync("A database entry for " + user.Mention + " already exists, updating user entry...");
                await userFlow.UpdateUser(user.Id, status);
                await ReplyAsync($"Successfully updated <@!{user.Id}> as {status}");
            }
        }
        public async Task AddUser(ulong userId, string status = "New User")
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            if (await userFlow.AddUser(userId, status))
                await ReplyAsync($"Successfully added <@!{userId}> as {status}");
            else
            {
                await ReplyAsync("A database entry for " + Context.Client.GetUser(userId).Mention + " already exists, updating user entry...");
                await userFlow.UpdateUser(userId, status);
                await ReplyAsync($"Successfully updated <@!{userId}> as {status}");
            }
        }

        [Command("Blacklist")]
        [Summary("Blacklists a user")]
        public async Task BlacklistUser(Discord.WebSocket.SocketGuildUser user)
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            if (await userFlow.AddUser(user.Id, "Blacklisted"))
                await ReplyAsync($"Successfully added <@!{user.Id}> as Blacklisted");
            else
            {
                await userFlow.UpdateUser(user.Id, "Blacklisted");
                await ReplyAsync($"Successfully updated <@!{user.Id}> as Blacklisted");
            }
        }
        [Command("Blacklist")]
        [Summary("Blacklists a user")]
        public async Task BlacklistUser(ulong userId)
        {
            User_Flow_control userFlow = new User_Flow_control(Context.Client);

            SocketUser user = Context.Client.GetUser(userId);

            if (await userFlow.AddUser(user.Id, "Blacklisted"))
                await ReplyAsync($"Successfully added <@!{userId}> as Blacklisted");
            else
            {
                await userFlow.UpdateUser(user.Id, "Blacklisted");
                await ReplyAsync($"Successfully updated <@!{userId}> as Blacklisted");
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

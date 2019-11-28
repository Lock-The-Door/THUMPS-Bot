using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
    }

    [Group("indev")]
    [RequireOwner(ErrorMessage = "This command are not ready yet")]
    public class InDevModule : ModuleBase<SocketCommandContext>
    {
        [Command("clear")]
        [RequireContext(contexts: ContextType.Guild, ErrorMessage = "This command can only be used in a guild", Group = "Guild Command")]
        [Summary("Clears messages in a channel")]
        public async Task Clear(int messages = 0)
        {
            //reply with info
            IMessage originalMessage = await ReplyAsync("Clearing " + messages + " messages.");
            Thread.Sleep(1000);
            //delete command usage and reply
            await originalMessage.DeleteAsync();
            await Context.Message.DeleteAsync();

            //set up variables
            var messagesInRangeAsyncEnum = Context.Channel.GetMessagesAsync(limit: messages);
            var messagesInRange = await messagesInRangeAsyncEnum.FlattenAsync();
            ITextChannel contextTextChannel = Context.Channel as ITextChannel;

            //delete messages in bulk
            await contextTextChannel.DeleteMessagesAsync(messagesInRange);

            //reply with successful message and delete it after
            IMessage doneMessage = await ReplyAsync("Message deletion successful!");
            Thread.Sleep(1000);
            await doneMessage.DeleteAsync();

            //The following zombie code is for a better, more secure, and more accidental proof clear command
            /*Tools_and_Tasks tasks = new Tools_and_Tasks 
            {
                client = Context.Client
            };//This command requires tools and tasks class
            tasks.Setup();//setup triggers

            if (messages == 0) //detect for errors in parameter
            {
                await ReplyAsync("You must specify a valid number of messages to delete");
                return;
            }

            //create a warning message before deleting messages
            IUserMessage originalMessage = await ReplyAsync("Attempting to delete " + messages + " messages in 10 seconds. React with :negative_squared_cross_mark: to cancel");
            System.Console.WriteLine("\u2612");
            await originalMessage.AddReactionAsync(new Emoji("❎"));

            //get messages
            var messagesInRangeAsyncEnum = Context.Channel.GetMessagesAsync(limit: messages, fromMessage: originalMessage, dir: Direction.Before);
            var messagesInRange = await messagesInRangeAsyncEnum.FlattenAsync();

            //find amount of messages found
            int messageCountFound = 0;

            //set up varibles for detecting pinned messages and counting messages detected
            int pinnedMessages = 0;
            foreach (var message in messagesInRange)
            {
                messageCountFound++;

                if (message.IsPinned)
                    pinnedMessages++;
            }

            //set up reply string
            string updateReply = "";

            //add information to reply string
            updateReply += messageCountFound + "/" + messages + " messages found in channel and " + pinnedMessages + " of those " + messageCountFound + " messages are pinned. ";

            int nextWait = 5000; //how long to wait until action occurs

            //add certain message depending if there is a pinned message or not
            if (pinnedMessages > 0)
            {
                updateReply += "\nReact with :white_check_mark: to continue anyway or :negative_squared_cross_mark: to cancel. Canceling automatically in 10 seconds.";
                updateReply = ":warning: ***WARNING!*** :warning: " + updateReply;

                nextWait = 10000;
            }
            else
            {
                await tasks.ClearWaiter(nextWait, updateReply, tasks.executeClear, messages, Context.Client, Context.Channel.Id);
            }*/
        }
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
                EmbedBuilder warnLogEmbedBuilder = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = user.GetAvatarUrl(),
                        Name = user.Username + " has been warned!"
                    },
                    Color = Color.Orange
                };
                warnLogEmbedBuilder.AddField("Moderator", Context.User.Mention, true).AddField("Channel", Context.Channel, true);
                warnLogEmbedBuilder.AddField("Reason", reason);
                Embed warnLogEmbed = warnLogEmbedBuilder.Build();

                //reply to executer
                await ReplyAsync(embed: warnReplyEmbed);
                //send mesage to admin channel
                await Context.Guild.GetTextChannel(644941989883674645).SendMessageAsync(embed: warnLogEmbed);
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
    
    public class Miscellaneous : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Summary("A command to find all avalible commands to the user")]
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

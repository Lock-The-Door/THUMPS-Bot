using Discord;
using Discord.Commands;
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
        public async Task InputTest([Remainder]string input) => await ReplyAsync(input);

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
        Mod_Actions actions = new Mod_Actions();

        [Command("infractions")]
        [Summary("Finds infractions")]
        public async Task Infractions(IGuildUser user)
        {
            Embed infractions = await actions.FindInfractions(user, Context.Client);
            await ReplyAsync(embed: infractions);
        }
    }
    
    //Finished Commands
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
            
            List<string> uselessModules = new List<string>(commandServices.Modules.Count);//list for unusable commands
            uselessModules.AddRange("TestModule", "InDevModule");

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

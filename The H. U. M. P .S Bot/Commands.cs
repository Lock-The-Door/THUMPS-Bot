using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace THUMPSBot
{

    [Group("tests")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        SaveTest save = new SaveTest();

        [Command("test")]
        [RequireOwner(ErrorMessage = "This is a test command and is not intended for public use.", Group = "Permmision")]
        [Summary("A test command")]
        public async Task Test() => await ReplyAsync("test");

        [Command("inputTest")]
        [RequireOwner(ErrorMessage = "This is a test command and is not intended for public use.", Group = "Permmision")]
        [Summary("Tests input by sending a echo")]
        public async Task InputTest([Remainder]string input) => await ReplyAsync(input);

        [Command("save")]
        [RequireOwner(ErrorMessage = "This is a test command and is not intended for public use.", Group = "Permmision")]
        [Summary("Test saving mechanics")]
        public async Task SaveTest([Remainder] string input)
        {
            await save.Save(input);
            await ReplyAsync("Saved: \"" + input + "\" to text file. Do !load to view all saved items");
        }
        [Command("load")]
        [RequireOwner(ErrorMessage = "This is a test command and is not intended for public use.", Group = "Permmision")]
        [Summary("Test loading mechanics")]
        public async Task LoadTest()
        {
            await ReplyAsync(save.Load().Result);
        }
    }

    [Group("indev")]
    public class InDevModule : ModuleBase<SocketCommandContext>
    {
        Mod_Actions actions = new Mod_Actions();

        [Command("infractions")]
        [RequireOwner(ErrorMessage = "This command is in development. You cannot use it right now.", Group = "Permmision")]
        [Summary("Finds infractions")]
        public async Task Infractions([Remainder] IUser user)
        {
            string infractions = await actions.FindInfractions();
        }

        [Command("help")]
        [Summary("A command to find all avalible commands to the user")]
        public async Task Help()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            CommandService commandService = new CommandService();
            await commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
            foreach (CommandInfo command in commandService.Commands)
            {
                bool usable = true;

                if (usable)
                {
                    // Get the command Summary attribute information
                    string embedFieldText = command.Summary ?? "No description available\n";

                    embedBuilder.AddField("!" + command.Name, embedFieldText);
                }
            }

            await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
        }
    }
}

using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace THUMPSBot
{

    [Group("tests")]
    [RequireOwner(ErrorMessage = "This is a test command and is not designed for the general public", Group = "tests")]
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
        public async Task Infractions(IUser user)
        {
            string infractions = await actions.FindInfractions(user, Context.Client);
            await ReplyAsync(infractions);
        }
    }
}

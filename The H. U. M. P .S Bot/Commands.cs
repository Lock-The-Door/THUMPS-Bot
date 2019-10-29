using Discord.Commands;
using System.Threading.Tasks;

namespace THUMPSBot
{

    [Group("tests")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("test")]
        [Summary("A test command")]
        public async Task Test()
        {
            await ReplyAsync("test");
        }

        [Command("inputTest")]
        [Summary("tests input by sending a echo")]
        public async Task InputTest([Remainder]string input)
        {
            await ReplyAsync(input);
        }
    }
}

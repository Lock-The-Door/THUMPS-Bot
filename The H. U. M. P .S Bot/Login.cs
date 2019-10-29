using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace THUMPSBot
{
    public class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false
            });

            _client.Log += Log;
            _commands.Log += Log;

            Command_Handler command_Handler = new Command_Handler(_client, _commands);
            await command_Handler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, "NjAwMTU1NDQwMDc2MTYxMDQ3.XSvowA.ml8wm5HLRSzlYPvU8fE7kPw-CnA");
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}

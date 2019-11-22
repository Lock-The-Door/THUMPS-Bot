using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace THUMPSBot
{
    public class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;

        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

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

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("THUMPS_API_Token", EnvironmentVariableTarget.User));
            await _client.StartAsync();

            // Block this task.
            await Task.Delay(-1);
            Thread.CurrentThread.IsBackground = false;
        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}

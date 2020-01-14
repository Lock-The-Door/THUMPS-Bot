using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace THUMPSBot
{
    public class Command_Handler
    {
        private readonly DiscordSocketClient _client;
        public readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public Command_Handler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);

            User_Flow_control userFlow = new User_Flow_control(_client);
            _client.UserJoined += userFlow.Client_UserJoined;
            _client.UserLeft += Client_UserLeft;
            _client.UserUpdated += userFlow.Client_UserUpdated;
            _client.RoleUpdated += userFlow.Client_RoleUpdated;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            Console.WriteLine(message.Content);
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            //load mod commands for bot use
            Mod_Actions actions = new Mod_Actions();
            //detect for banned words
            if (AutoMod.WordFilter(message.Content, out string reason))
            {
                string link = "https://discordapp.com/channels/597798914606759959/" + message.Channel.Id + "/" + message.Id;
                await message.Channel.SendMessageAsync(":exclamation: :eyes: :exclamation: " + message.Author.Mention + ", you used the " + reason + " This will be logged and may be used against you. " + link);
                await actions.LogInfraction(message.Author, _client.CurrentUser, message.Channel, reason);
              
                return;
            }

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
            {
                return;
            }

            Console.WriteLine(message.Content);
            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }

        private async Task Client_UserLeft(SocketGuildUser arg)
        {
            await arg.Guild.GetTextChannel(665723218773934100).SendMessageAsync(string.Format("<@!{0}> just left the server. Good for them...", arg.Id));
        }
    }
}

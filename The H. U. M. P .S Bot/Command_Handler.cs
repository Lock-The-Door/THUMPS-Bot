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
        private readonly CommandService _commands;

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

            _client.UserJoined += Client_UserJoined;
            _client.UserLeft += Client_UserLeft;
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
                await message.Channel.SendMessageAsync(":exclamation: :eyes: :exclamation: " + message.Author.Mention + " " + link + " " + reason + " This will be logged and may be used against you.");
                string infractionMessage = "THUMPS Bot warned " + message.Author.Username + " with the id of " + message.Author.Id + " for " + reason + " in " + message.Channel.Name;
                await actions.LogInfraction(infractionMessage);
                var channel = _client.GetChannel(644941989883674645) as ISocketMessageChannel;
                await channel.SendMessageAsync(infractionMessage);
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

        private async Task Client_UserJoined(SocketGuildUser arg)
        {
            if (arg.Id == 424297184822034444 || arg.Id == 272396560686514176 || arg.Id == 645401167542747136 || arg.Id == 601067664126902275) //rebanner
            {
                await arg.Guild.GetTextChannel(644941983382503484).SendMessageAsync(arg.Username + "is blacklisted from this server.");
                await arg.Guild.AddBanAsync(arg.Id);
            }
            else if (DateTime.Now.Subtract(arg.CreatedAt.Date).TotalDays < 11 || (arg.GetDefaultAvatarUrl() == arg.GetAvatarUrl() && arg.CreatedAt.Date < new DateTime(2019, 11, 1))) //quarentine for new accounts. If the icon is default but created after november 2019 it will also be quarentined
            {
                await arg.Guild.GetTextChannel(644941983382503484).SendMessageAsync(arg.Mention + ", your account is quite new. Due to recent events, we will have to verify you. Please dm " + arg.Guild.Owner.Mention);
                await arg.AddRoleAsync(arg.Guild.GetRole(645413078405611540));
            }
            else //welcome message
            {
                await arg.AddRoleAsync(arg.Guild.GetRole(597929341308895252));
                await arg.Guild.GetTextChannel(644941983382503484).SendMessageAsync("Welcome " + arg.Mention + ", we are currently repairing the server due to a security breach.");
            }
        }

        private async Task Client_UserLeft(SocketGuildUser arg)
        {
            await arg.Guild.GetTextChannel(644941983382503484).SendMessageAsync(string.Format("**{0}** just left the server. Good for them...", arg.Username));
        }
    }
}

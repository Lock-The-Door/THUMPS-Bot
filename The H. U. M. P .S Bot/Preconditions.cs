using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace THUMPSBot
{
    public class RequireServerOwner : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (context.Guild.GetOwnerAsync().Result == gUser)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError("You have to own the server to use this command!"));
            }
            return Task.FromResult(PreconditionResult.FromError("You have to be in a server to use this command!"));
        }
    }
}

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace THUMPSBot
{
    public static class Tools_and_Functions
    {
        public static int GetHighestRolePosition(IGuildUser user, DiscordSocketClient client)
        {
            int highestPosition = -1;
            foreach (ulong roleId in user.RoleIds)
            {
                int nextRolePosition = client.GetGuild(597798914606759959).GetRole(roleId).Position;
                if (nextRolePosition > highestPosition)
                    highestPosition = nextRolePosition;
                Console.WriteLine(highestPosition + "<" + client.GetGuild(597798914606759959).GetRole(roleId).Name + nextRolePosition);
            }

            return highestPosition;
        }
    }

    /*public class Tools_and_Tasks
    {
        public DiscordSocketClient client;
        public bool executeClear = true; //for clear command function
        public void Setup()
        {
            client.ReactionAdded += AddReactionHandler;//create trigger when method created
        }
        public async Task AddReactionHandler(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel originChannel, SocketReaction ReactionAdded)
        {
            //set up everything
            var message = await cachedMessage.GetOrDownloadAsync();
            //prevent bot access
            if (ReactionAdded.User.Value.IsBot)
                return;

            //clear command
            if (message.Author.Discriminator == client.CurrentUser.Discriminator && ReactionAdded.Emote.Name == new Emoji("❎").Name && message.Content.Contains(" messages in 10 seconds. React with :negative_squared_cross_mark: to cancel"))
            {
                //get guild user
                IGuildUser guildUser = client.GetGuild(597798914606759959).GetUser(ReactionAdded.UserId);
                //clear command cancel anyway can only be done by mods, see if done by mod through kick perm
                if (guildUser.GuildPermissions.KickMembers)
                {
                    executeClear = false;
                    await originChannel.SendMessageAsync("Clear has been canceled");
                }
            }
        }

        public async Task ClearWaiter(int nextWait, string updateReply, bool executeClear, int messages, DiscordSocketClient client, ulong channelId)
        {
            if (executeClear)
            {
                updateReply += "\nDeleting " + messages + " messages in 5 seconds";
                //wait 5 seconds before sending information
                Thread.Sleep(5000);
            }
            else
            {
                return; //canceled
            }

            await client.GetGuild(597798914606759959).GetTextChannel(channelId).SendMessageAsync(updateReply);//send info

            Thread.Sleep(nextWait);//wait
        }
    }*/
}

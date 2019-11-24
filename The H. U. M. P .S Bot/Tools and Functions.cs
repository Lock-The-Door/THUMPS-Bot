using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

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
}

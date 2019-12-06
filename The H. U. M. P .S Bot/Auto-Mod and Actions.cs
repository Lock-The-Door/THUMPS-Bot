﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace THUMPSBot
{
    public static class AutoMod
    {
        
        public static bool WordFilter(string message, out string reason)
        {
            //get rid of special characters and spaces
            string combinedMessage = "";
            foreach (char c in message.ToLower().ToCharArray())
            {
                char nc = c;
                if (nc != '!' && nc != '.' && nc != ' ' && nc != ',' && nc != '/' && nc != '?' && nc != '_')
                {
                    /*if (c == '3' : 'ἕ' : 'è' : 'é' : 'ę' : 'ê')
                        nc = 'e';
                    else if (c == '9')
                        nc = 'g';
                    else if (c == '1' : '!' : '|' : 'î' : 'ï')*/
                    switch (c)
                    {
                        case '3':
                        case 'ἕ':
                        case 'è':
                        case 'é':
                        case 'ę':
                        case 'ê':
                            nc = 'e';
                            break;
                        case '9':
                            nc = 'g';
                            break;
                        case '1':
                        case '!':
                        case '|':
                        case 'î':
                        case 'ï':
                            nc = 'i';
                            break;
                        case 'ń':
                            nc = 'n';
                            break;
                    }
                    combinedMessage += nc;
                }
            }

            //test for bad words
            if (combinedMessage.Contains("nigger"))
            { reason = "n-word"; return true; }
            else if (combinedMessage.Contains("niga"))
            { reason = "n-word slang"; return true; }
            else if (combinedMessage.Contains("nlgger"))
            { reason = "n-word with l"; return true; }
            reason = "";
            return false;
        }
    }

    public class Mod_Actions
    {
        public async Task<Embed> FindInfractionsCommand(IGuildUser infringer, DiscordSocketClient client)
        {
            List<DataRow> userInfractions = await GetInfractions(infringer);

            //get average infraction rate (days, weeks)
            double joinedDays = Math.Floor(DateTime.Now.Subtract(infringer.JoinedAt.Value.DateTime).TotalDays);
            int totalinfractions = userInfractions.Count;

            //average days
            double averageDay = totalinfractions / joinedDays;

            //average week
            double averageWeek = totalinfractions / Math.Floor(joinedDays / 7);

            //round
            averageDay = Math.Round(averageDay, 2);
            averageWeek = Math.Round(averageWeek, 2);

            //create new embeded message that will be used to display the history
            EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder
            {
                IconUrl = infringer.GetAvatarUrl(),
                Name = infringer.Username + "'s infraction history"
            };
            EmbedBuilder infractionsEmbed = new EmbedBuilder
            {
                Author = authorBuilder
            };
            // return if no infractions
            if (userInfractions.Count == 0)
            {
                return new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = infringer.GetAvatarUrl(),
                        Name = infringer.Username + " has no infractions!"
                    }
                }.Build();
            }

            //add averages and total to embed
            infractionsEmbed.AddField("Daily Avg.", averageDay + " infractions/day", true);
            infractionsEmbed.AddField("Weekly Avg.", averageWeek + " infractions/week", true);
            //add total infraction number
            infractionsEmbed.AddField("Total Infractions", totalinfractions + " infractions", true);

            //take the 10 latest infractions
            while (userInfractions.Count > 5)
            {
                userInfractions.RemoveAt(0);
            }

            //reverse order so it's from newest to oldest
            userInfractions.Reverse();

            string infractionString = ""; //infractions
                                          //go through each matching infraction
            foreach (DataRow infraction in userInfractions)
            {
                //get the info stored

                //get mod who gave warn
                IUser mod = client.GetUser(Convert.ToUInt64(long.Parse(infraction.ItemArray[2].ToString())));
                infractionString += "**Moderator:** " + mod.Username;

                //channel user was warned in
                IChannel channel = client.GetChannel(Convert.ToUInt64(long.Parse(infraction.ItemArray[3].ToString())));
                infractionString += "\n**Channel:** #" + channel.Name;

                //date and time user was warned
                infractionString += "\n**Date and Time:** " + infraction.ItemArray[4].ToString();

                //reason for warn
                infractionString += "\n**Reason:** " + infraction.ItemArray[5].ToString();
                if (infraction.ItemArray[5].ToString().Length > 30)
                { infractionString.Remove(30); infractionString += "..."; }

                //add lines between different infractions for readablitiy
                infractionString += "\n\n";
            }

            infractionsEmbed.AddField("Last 5 infractions", infractionString);

            return infractionsEmbed.Build();
        }

        public async Task<List<DataRow>> GetInfractions(IGuildUser infringer) //get the infractions from the database
        {
            List<DataRow> userInfractions = new List<DataRow>(); //create the list with all the infranger's infractions (created outside usings to safely close the connection after)

            //access the database
            string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=Infractions;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Infractions", connectionString))
            {
                await connection.OpenAsync();

                //get the infraction data
                DataTable infractions = new DataTable();
                adapter.Fill(infractions);

                userInfractions.Capacity = infractions.Rows.Count; //resize it to minimum size

                //go through each infraction
                foreach (DataRow infraction in infractions.Rows)
                {
                    //find the right value and see if it's the right person. If so move it to the list
                    string found = infraction[infractions.Columns[1]].ToString(); ;
                    if (ulong.TryParse(found, out ulong foundId))
                    {
                        if (foundId == infringer.Id)
                        {
                            userInfractions.Add(infraction);
                        }
                    }
                }
            }

            return userInfractions;
        }

        public async Task LogInfraction(IUser infringingUser, IUser modUser, IChannel channel, string reason)
        {
            //access the database
            string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=Infractions;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            string query = "INSERT INTO Infractions VALUES (@Infringer, @Moderator, @Channel, @Time, @Reason)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                //convert infringing id
                Console.WriteLine(infringingUser.Id);
                long longInfringingId = Convert.ToInt64(infringingUser.Id);
                //convert mod id
                long longModId = Convert.ToInt64(modUser.Id);
                //convert channel id
                long longChannelId = Convert.ToInt64(channel.Id);

                //set varibles
                command.Parameters.AddWithValue("@Infringer", longInfringingId);
                command.Parameters.AddWithValue("@Moderator", longModId);
                command.Parameters.AddWithValue("@Channel", longChannelId);
                command.Parameters.AddWithValue("@Time", DateTime.Now);
                command.Parameters.AddWithValue("@Reason", reason);

                command.ExecuteNonQuery();
            }

            //after an infraction is logged, see if nessesary action needs to be taken
            await automodAction(infringingUser as IGuildUser, channel as ITextChannel);
        }

        public async Task automodAction(IGuildUser infringer, ITextChannel workingChannel) //detects for too many infracions on a user and finds a suitable punishment
        {
            List<DataRow> infringerInfractions = await GetInfractions(infringer); //first get the data for the user that was just warned

            //then from the data, filter out the ones in the n-word chat since it is helpful
            foreach (DataRow infraction in infringerInfractions)
            {
                if (Convert.ToUInt64(infraction[3]) == 644942002630164480 || Convert.ToUInt64(infraction[3]) == 644942010385694720)
                {
                    infraction.Delete();
                }
            }

            //the rest of the infractions is not from the n-word channel or the nsfw channel and deserves to be punished for

            //current punishments: 5/day = 5 min mute, 15/day = 30 min mute, 20/day = mute, 50/month = kick, 75/month = 1 hour ban, 100/month = 1 day ban, 300/year = 1 week ban, 600/year = 1 month ban, 1000/year = ban.

            //calculate required variables: per day, per week, per month, per year
            uint perDay = 0, perWeek = 0, perMonth = 0, perYear = 0;

            //get values for variables
            foreach (DataRow infraction in infringerInfractions)
            {
                if (DateTime.Now.Subtract(Convert.ToDateTime(infraction[4])).TotalDays < 1) //less than one day
                {
                    perDay++;
                }

                if (DateTime.Now.Subtract(Convert.ToDateTime(infraction[4])).TotalDays / 7 < 1) //less than one week
                {
                    perWeek++;
                }

                if (DateTime.Now.Subtract(Convert.ToDateTime(infraction[4])).TotalDays / 30 < 1) //less than one month, benefit of the doubt in days
                {
                    perMonth++;
                }

                if (DateTime.Now.Subtract(Convert.ToDateTime(infraction[4])).TotalDays / 365 < 1) //less than one year, benifit of the doubt
                {
                    perYear++;
                }
            }

            //to prevent repeating punishments, it will have to use equal instead of more than
            char type = 'n';
            int length = -1;
            char format = 'n';
            if (perYear > 0)
            {
                type = 'b';
                format = 'd';
                switch (perYear)
                {
                    case 300:
                        length = 7;
                        break;
                    case 600:
                        length = 30;
                        break;
                    case 1000:
                        length = -1;
                        break;
                }
            }
            else if (perMonth > 0)
            {
                type = 'b';
                format = 'h';
                switch (perMonth)
                {
                    case 50:
                        type = 'k';
                        break;
                    case 75:
                        length = 1;
                        break;
                    case 100:
                        length = 24;
                        break;
                }
            }
            else if (perDay > 0)
            {
                type = 'm';
                format = 'm';
                switch (perDay)
                {
                    case 15:
                        length = 30;
                        break;
                    case 5:
                        length = 5;
                        break;
                }
            }
            await redirectPunishment(type, format, length, infringer, workingChannel);
        }

        public async Task redirectPunishment(char punishmentType, char punishmentLengthformat, int punishmentLength, IGuildUser infringer, ITextChannel workingChannel)
        {
            //ignore mods but give them a warning
            if (infringer.GuildPermissions.KickMembers)
            {
                await workingChannel.SendMessageAsync("Since **" + infringer.Username + "** is a mod, I will not take any actions against **" + infringer.Username + "**, however, " + infringer.Mention + ", be careful or you might be demoted");
                return;
            }

            //calculate punishement
            switch (punishmentType)
            {
                case 'm':
                    await Mute(punishmentLengthformat, punishmentLength, infringer, workingChannel, "getting too many infractions");
                    break;
                case 'k':
                    await Kick(infringer, workingChannel);
                    break;
                case 'b':
                    await Ban(punishmentLengthformat, punishmentLength, infringer, workingChannel, "getting too many infractions");
                    break;
            }
        }

        public async Task Mute(char lengthFormat, int intLength, IGuildUser infringer, ITextChannel workingChannel, string reason)
        {
            //detect length format
            DateTime length = new DateTime();
            switch (lengthFormat)
            {
                case 'm':
                    length.AddMinutes(intLength);
                    break;
                case 'h':
                    length.AddHours(intLength);
                    break;
                case 'd':
                    length.AddDays(intLength);
                    break;
            }
            //make reply
            await workingChannel.SendMessageAsync("Muting " + infringer.Mention + " for " + intLength + lengthFormat + " for " + reason);

            //mute
            await infringer.AddRoleAsync(infringer.Guild.GetRole(652337585414209551));

            //detect if temp mute
            if (length != null)
            {
                //if it is, add the end time to the database
                //access the database
                string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=Infractions;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

                string query = "INSERT INTO Punishments VALUES (@Infringer, Mute, @EndDateTime, @Reason)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    //convert infringing id
                    long longInfringingId = Convert.ToInt64(infringer.Id);

                    //set varibles
                    command.Parameters.AddWithValue("@Infringer", longInfringingId);
                    command.Parameters.AddWithValue("@EndDateTime", length);
                    command.Parameters.AddWithValue("@Reason", reason);

                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task Kick(IGuildUser infringer, ITextChannel workingChannel) //reason not required as linking this to a command is not planned at all (just do it the normal way)
        {
            await workingChannel.SendMessageAsync("Kicking " + infringer.Mention + " for getting too many infractions");
        }

        public async Task Ban(char lengthFormat, int intLength, IGuildUser infringer, ITextChannel workingChannel, string reason)
        {
            //detect length format
            DateTime length = DateTime.Now;
            switch (lengthFormat)
            {
                case 'm':
                    length = new DateTime(0, 0, 0, 0, intLength, 0);
                    break;
                case 'h':
                    length = new DateTime(0, 0, 0, intLength, 0, 0);
                    break;
                case 'd':
                    length = new DateTime(0, 0, intLength, 0, 0, 0);
                    break;
            }
            //make reply
            await workingChannel.SendMessageAsync("Banning " + infringer.Mention + " for " + length + lengthFormat + " for " + reason);

            //ban user
            await infringer.BanAsync(reason: reason);

            //detect if temp ban
            if (length != null)
            {
                //if it is, add the end time to the database
                //access the database
                string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=Infractions;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

                string query = "INSERT INTO Punishments VALUES (@Infringer, Ban, @EndDateTime, @Reason)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    //convert infringing id
                    long longInfringingId = Convert.ToInt64(infringer.Id);

                    //set varibles
                    command.Parameters.AddWithValue("@Infringer", longInfringingId);
                    command.Parameters.AddWithValue("@EndDateTime", length);
                    command.Parameters.AddWithValue("@Reason", reason);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

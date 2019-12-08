using Discord;
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
            reason = "";
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
                        case 'ė':
                            nc = 'e';
                            break;
                        case '9':
                            nc = 'g';
                            break;
                        case '1':
                        case '!':
                        case 'î':
                        case 'ï':
                            nc = 'i';
                            break;
                        case 'ł':
                        case '|':
                            nc = 'l';
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
            { reason = "n-word"; }
            else if (combinedMessage.Contains("nigga") || combinedMessage.Contains("niga"))
            { reason = "n-word slang"; }
            else if (combinedMessage.Contains("nlgger"))
            { reason = "n-word with l"; }
            else if (combinedMessage.Contains("n#gger"))
            { reason = "n-word with #"; }
            else if (combinedMessage.Contains("n’gger") || combinedMessage.Contains("n\"gger"))
            { reason = "n-word with quotation marks (' and \")"; }
            else if (combinedMessage.Contains("kneeger"))
            { reason = "knee-word (n-word that starts with knee)"; }
            else if (combinedMessage.Contains("nicker"))
            { reason = "n-word with ck instead of gg"; }
            else if (combinedMessage.Contains("niggr"))
            { reason = "n-word without an e"; }

            if (reason != "")
                return true;
            

            //secondary checks for unnessary letters that are included
            char lastChar = ' '; //add last character for repeating checks
            string nonrepeatingMessage = ""; //for new message
            foreach (char letter in combinedMessage)
            {
                if (letter != lastChar) //if the character repeats ignore it
                {
                    nonrepeatingMessage += letter; //otherwise keep it
                }
            }

            if (nonrepeatingMessage == combinedMessage) //let them go to not filter niger, a country. Fix this later.
            {
                reason = "";
                return false;
            }

            //then run it through the checks again
            if (combinedMessage.Contains("niger"))
            { reason = "n-word with extra letters"; }
            else if (combinedMessage.Contains("niga"))
            { reason = "n-word slang with extra letters"; }
            else if (combinedMessage.Contains("nlger"))
            { reason = "n-word with l and extra letters"; }
            else if (combinedMessage.Contains("n#ger"))
            { reason = "n-word with # and extra letters"; }
            else if (combinedMessage.Contains("n’ger") || combinedMessage.Contains("n\"gger"))
            { reason = "n-word with quotation marks (' and \") and extra letters"; }
            else if (combinedMessage.Contains("kneger"))
            { reason = "knee-word (n-word that starts with knee) with extra letters"; }

            if (reason != "")
                return true;
            //bad word not detected
            else
                return false;
        }
    }

    public class Mod_Actions
    {
        //set up oftenly used varibles
        ulong adminChannelId = 644941989883674645;

        public async Task<Embed> FindInfractions(IGuildUser infringer, DiscordSocketClient client)
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
            string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=THUMPS;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

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

        public async Task LogInfraction(IUser infringingUser, IUser modUser, ISocketMessageChannel channel, string reason)
        {
            //access the database
            string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=THUMPS;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

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

            //send embeded message to admin log
            EmbedBuilder warnLogEmbedBuilder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = infringingUser.GetAvatarUrl(),
                    Name = infringingUser.Username + " has been warned!"
                },
                Color = new Color(230, 200, 0)
            };
            warnLogEmbedBuilder.AddField("Moderator", modUser, true).AddField("Channel", channel, true);
            warnLogEmbedBuilder.AddField("Reason", reason);
            Embed warnLogEmbed = warnLogEmbedBuilder.Build();
            //send admin message
            await (channel as ITextChannel).Guild.GetTextChannelAsync(adminChannelId).Result.SendMessageAsync(embed: warnLogEmbed);

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

            string reason = "getting too many infractions";
            //calculate punishement
            switch (punishmentType)
            {
                case 'm':
                    await Mute(punishmentLengthformat, punishmentLength, infringer, await infringer.Guild.GetUserAsync(600155440076161047), workingChannel, reason);
                    break;
                case 'k':
                    await Kick(infringer, await infringer.Guild.GetUserAsync(600155440076161047), workingChannel, reason);
                    break;
                case 'b':
                    await Ban(punishmentLengthformat, punishmentLength, infringer, await infringer.Guild.GetUserAsync(600155440076161047), workingChannel, reason);
                    break;
            }
        }

        public async Task Mute(char lengthFormat, int intLength, IGuildUser infringer, IGuildUser mod, ITextChannel workingChannel, string reason)
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

                string query = "INSERT INTO Punishments VALUES (@Infringer, @Moderator, Mute, @EndDateTime, @Reason)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    //convert infringing id
                    long longInfringingId = Convert.ToInt64(infringer.Id);
                    //convert mod id
                    long longModId = Convert.ToInt64(mod.Id);

                    //set varibles
                    command.Parameters.AddWithValue("@Infringer", longInfringingId);
                    command.Parameters.AddWithValue("@Moderator", longModId);
                    command.Parameters.AddWithValue("@EndDateTime", length);
                    command.Parameters.AddWithValue("@Reason", reason);

                    command.ExecuteNonQuery();
                }
            }

            //make admin embed message
            EmbedBuilder muteLogEmbedBuilder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = infringer.GetAvatarUrl(),
                    Name = infringer.Username + " has been warned!"
                },
                Color = Color.Orange
            };
            muteLogEmbedBuilder.AddField("Moderator", mod, true).AddField("Channel", workingChannel, true);
            muteLogEmbedBuilder.AddField("Reason", reason);
            Embed muteLogEmbed = muteLogEmbedBuilder.Build();

            //log in admin log
            await infringer.Guild.GetTextChannelAsync(adminChannelId).Result.SendMessageAsync(embed: muteLogEmbed);
        }

        public async Task Kick(IGuildUser infringer, IGuildUser mod, ITextChannel workingChannel, string reason) //reason not required as linking this to a command is not planned at all (just do it the normal way)
        {
            await workingChannel.SendMessageAsync("Kicking " + infringer.Mention + " for getting too many infractions");

            //kick member
            await infringer.KickAsync();

            //make admin embed message
            EmbedBuilder kickLogEmbedBuilder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = infringer.GetAvatarUrl(),
                    Name = infringer.Username + " has been warned!"
                },
                Color = new Color(255, 70, 0)
            };
            kickLogEmbedBuilder.AddField("Moderator", mod, true).AddField("Channel", workingChannel, true);
            kickLogEmbedBuilder.AddField("Reason", reason);
            Embed kickLogEmbed = kickLogEmbedBuilder.Build();

            //log in admin log
            await infringer.Guild.GetTextChannelAsync(adminChannelId).Result.SendMessageAsync(embed: kickLogEmbed);
        }

        public async Task Ban(char lengthFormat, int intLength, IGuildUser infringer, IGuildUser mod, ITextChannel workingChannel, string reason)
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

                string query = "INSERT INTO Punishments VALUES (@Infringer, @Moderator, Ban, @EndDateTime, @Reason)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    //convert infringing id
                    long longInfringingId = Convert.ToInt64(infringer.Id);
                    //convert mod id
                    long longModId = Convert.ToInt64(mod.Id);

                    //set varibles
                    command.Parameters.AddWithValue("@Infringer", longInfringingId);
                    command.Parameters.AddWithValue("@Moderator", longModId);
                    command.Parameters.AddWithValue("@EndDateTime", length);
                    command.Parameters.AddWithValue("@Reason", reason);

                    command.ExecuteNonQuery();
                }
            }

            //make admin embed message
            EmbedBuilder banLogEmbedBuilder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = infringer.GetAvatarUrl(),
                    Name = infringer.Username + " has been warned!"
                },
                Color = new Color(235, 0, 0)
            };
            banLogEmbedBuilder.AddField("Moderator", mod, true).AddField("Channel", workingChannel, true);
            banLogEmbedBuilder.AddField("Reason", reason);
            Embed banLogEmbed = banLogEmbedBuilder.Build();

            //log in admin log
            await infringer.Guild.GetTextChannelAsync(adminChannelId).Result.SendMessageAsync(embed: banLogEmbed);
        }
    }
}

using Discord;
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
                        case 'ė':
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
                        case 'ł':
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
            { reason = "n-word"; return true; }
            else if (combinedMessage.Contains("niga"))
            { reason = "n-word slang"; return true; }
            else if (combinedMessage.Contains("nlgger"))
            { reason = "n-word with l"; return true; }
            else if (combinedMessage.Contains("n#gger"))
            { reason = "n-word with #"; return true; }
            else if (combinedMessage.Contains("n’gger") || combinedMessage.Contains("n\"gger"))
            { reason = "n-word with quotation marks (' and \")"; return true; }
            reason = "";
            return false;
        }
    }

    public class Mod_Actions
    {
        public async Task<Embed> FindInfractions(IGuildUser infringer, DiscordSocketClient client)
        {
            //access the database
            string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=Infractions;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Infractions", connectionString))
            {
                await connection.OpenAsync();

                //get the infraction data
                DataTable infractions = new DataTable();
                adapter.Fill(infractions);

                //create a list for infractions that deal with 
                List<DataRow> userInfractions = new List<DataRow>
                {
                    //go size to minimum size required
                    Capacity = infractions.Rows.Count
                };

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

                //create new embeded message that will be used to display the history
                EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder
                {
                    IconUrl = infringer.GetAvatarUrl(),
                    Name =  infringer.Username + "'s infraction history"
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

                //add to embed
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
        }
    }
}

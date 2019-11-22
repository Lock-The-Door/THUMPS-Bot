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
                    }
                    combinedMessage += nc;
                }
            }

            //test for bad words
            if (combinedMessage.Contains("nigger"))
            {
                reason = "n-word";
                return true;
            }
            else if (combinedMessage.Contains("niga"))
            {
                reason = "n-word slang";
                return true;
            }
            reason = "";
            return false;
        }
    }

    public class Mod_Actions
    {
        public async Task<Embed> FindInfractions(IGuildUser infringer, DiscordSocketClient client)
        {
            /*string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"infractions.txt");
            StreamReader reader = new StreamReader(path);
            string output = await reader.ReadToEndAsync();
            reader.Close();
            return output;*/

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

                //take the 10 latest infractions
                userInfractions.Capacity = 10;
                userInfractions.TrimExcess();

                string infractionString = "";//infractions
                //go through each matching infraction
                foreach (DataRow infraction in userInfractions)
                {
                    //get the info stored

                    //get mod who gave warn
                    IUser mod = client.GetUser(ulong.Parse(infraction.ItemArray[2].ToString()));
                    infractionString += "Moderator: " + mod.Username;

                    //channel user was warned in
                    IChannel channel = client.GetChannel(ulong.Parse(infraction.ItemArray[3].ToString()));
                    infractionString += "\nChannel: #" + channel.Name;

                    //date and time user was warned
                    infractionString += "\nDate and Time: " + infraction.ItemArray[4].ToString();

                    //reason for warn
                    infractionString += "\nReason: " + infraction.ItemArray[5].ToString();

                    //add lines between different infractions for readablitiy
                    infractionString += "\n\n";
                }

                infractionsEmbed.AddField("Last 10 infractions", infractionString);

                return infractionsEmbed.Build();
            }
        }

        public async Task LogInfraction(string reason)
        {
            /*string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"infractions.txt");
            StreamReader reader = new StreamReader(path);
            string data = await reader.ReadToEndAsync();
            reader.Close(); //close to allow writer to open
            StreamWriter writer = new StreamWriter(path);
            await writer.WriteAsync(data);
            await writer.WriteLineAsync(reason);
            writer.Close();*/
            
        }
    }
}

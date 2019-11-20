using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using Discord;
using Discord.WebSocket;

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
                    switch (c)
                    {
                        case '3':
                        case 'ἕ':
                        case 'è':
                        case 'é':
                        case 'ę':
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
        public async Task<string> FindInfractions(IUser infringer, DiscordSocketClient client)
        {
            /*string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"infractions.txt");
            StreamReader reader = new StreamReader(path);
            string output = await reader.ReadToEndAsync();
            reader.Close();
            return output;*/

            //access the database
            string connectionString = ConfigurationManager.ConnectionStrings[0].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Infractions", connectionString))
            {
                await connection.OpenAsync();

                //get the infraction data
                DataTable infractions = new DataTable();
                adapter.Fill(infractions);

                //create a list for infractions that deal with 
                List<DataRow> userInfractions = new List<DataRow>();

                //create the message that we will print
                string infractionMessage = "`";

                //go through each data row collection
                foreach (DataRowCollection infractionRow in infractions.Rows)
                {
                    //go through each infraction
                    foreach(DataRow infraction in infractionRow)
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

                    //go through each matching infraction
                    foreach (DataRow infraction in userInfractions)
                    {
                        int itemNumber = 0;
                        foreach (var item in infraction.ItemArray)
                        {
                            switch(itemNumber)
                            {
                                case 2:
                                    IUser infringingUser = client.GetUser(ulong.Parse(item.ToString()));
                                    infractionMessage += "Moderator: " + infringingUser.Mention;
                                    break;
                                case 3:
                                    IChannel channel = client.GetChannel(ulong.Parse(item.ToString()));
                                    infractionMessage += " Channel: #" + channel.Name;
                                    break;
                                case 4:
                                    infractionMessage += "Date and Time: " + item.ToString();
                                    break;
                                case 5:
                                    infractionMessage += "Reason" + item.ToString();
                                    break;
                            }
                        }
                        //add new line between infractions
                        infractionMessage += "\n";
                    }
                }
                //return result
                return infractionMessage + "`";
                //the message is surrounded by "`" because it won't actually ping anyone (this is the replace embedding because I don't have a website)
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

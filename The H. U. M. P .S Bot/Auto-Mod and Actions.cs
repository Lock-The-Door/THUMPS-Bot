using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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
        public async Task<string> FindInfractions()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"infractions.txt");
            StreamReader reader = new StreamReader(path);
            string output = await reader.ReadToEndAsync();
            reader.Close();
            return output;
        }

        public async Task LogInfraction(string reason)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"infractions.txt");
            StreamReader reader = new StreamReader(path);
            string data = await reader.ReadToEndAsync();
            reader.Close(); //close to allow writer to open
            StreamWriter writer = new StreamWriter(path);
            await writer.WriteAsync(data);
            await writer.WriteLineAsync(reason);
            writer.Close();
        }
    }
}

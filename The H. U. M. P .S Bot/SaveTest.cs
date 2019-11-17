using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace THUMPSBot
{
    public class SaveTest
    {
        public async Task Save(string input)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Saved.txt");
            StreamReader reader = new StreamReader(path);
            string data = await reader.ReadToEndAsync();
            reader.Close(); //close to allow writer to open
            StreamWriter writer = new StreamWriter(path);
            await writer.WriteAsync(data);
            await writer.WriteLineAsync(input);
            writer.Close();
        }
        public async Task<string> Load()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Saved.txt");
            StreamReader reader = new StreamReader(path);
            string output = await reader.ReadToEndAsync();
            reader.Close();
            return output;
        }
    }
}

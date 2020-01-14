using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace THUMPSBot
{
    class User_Flow_control
    {
        private readonly DiscordSocketClient _client;

        public User_Flow_control(DiscordSocketClient client)
        {
            _client = client;
            
        }

        string connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=THUMPS;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        public async Task Client_UserJoined(SocketGuildUser arg)
        {
            //get all user and bot server status

            // connect to database.

            // Setup DataRow List to read and write to database.
            List<DataRow> serverUsers = new List<DataRow>();

            // Start REading database.

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM MemberServerStatus", connectionString))
            {
                await connection.OpenAsync();

                //get the infraction data
                DataTable serverUsersTable = new DataTable();
                adapter.Fill(serverUsersTable);

                serverUsers.Capacity = serverUsersTable.Rows.Count; //resize it to minimum size

                foreach (DataRow row in serverUsersTable.Rows)
                {
                    serverUsers.Add(row);
                }
            }

            //bool executed = false;// to know when action is taken

            //see if user is blacklisted
            foreach (DataRow row in serverUsers)
            {
                if (Convert.ToUInt64(row[1]) == arg.Id && (string) row[2] == "Blacklisted")
                {
                    await arg.Guild.GetTextChannel(665723218773934100).SendMessageAsync($"<@!{arg.Id}> is blacklisted from this server.");
                    await arg.Guild.AddBanAsync(arg.Id);
                    return;
                }
            }
            //see if user if whitelisted
            foreach (DataRow row in serverUsers)
            {
                if (Convert.ToUInt64(row[1]) == arg.Id && (string) row[2] == "Whitelisted")
                {
                    await arg.AddRoleAsync(arg.Guild.GetRole(665758685464625153));
                    await arg.Guild.GetTextChannel(665723218773934100).SendMessageAsync(string.Format("Welcome back {0}, we will give your previous roles back shortly. However, mod roles will be given back only under certain circumstances. If we slack behind, just give a mod a ping.", arg.Username));
                    await arg.SendMessageAsync(string.Format("Hi person named **{0}**, you just rejoined **{1}** owned by **{2}** so I decided to spam you. I hope you still appreciate it! {3} \n\nStatistics of {1}:\nAmount of other people like you: **{4}**", arg.Username, arg.Guild.Name, arg.Guild.Owner, arg.Guild.IconUrl, arg.Guild.MemberCount));
                    return;
                }
            }
            if (arg.IsBot)// Bots must be whitelisted to join, bots will have to be confirmed to finish the joining process
            {
                await arg.Guild.GetTextChannel(665723218773934100).SendMessageAsync($"Looks like someone invited {arg.Mention}. Since it is a new bot, confirmation is needed.");
                await Quarentine(arg.Id, arg.Guild.Id, true); // run quarantine actions
            }
            else if (DateTime.Now.Subtract(arg.CreatedAt.Date).TotalDays < 11 || (arg.GetDefaultAvatarUrl() == arg.GetAvatarUrl() && arg.CreatedAt.Date < new DateTime(2019, 11, 1))) //quarantine for new accounts. If the icon is default but created after November 2019 it will also be quarantined
            {
                await arg.SendMessageAsync("Your account is quite new. Due to security, we will need to verify you. Please wait, you will get a DM from a mod shortly or from me. Hope you understand :smiley:"); //dm user
                await arg.AddRoleAsync(arg.Guild.GetRole(645413078405611540)); //give user quarantine role
                await Quarentine(arg.Id, arg.Guild.Id, false); // run quarantine actions
            }
            else // New user, do welcome messages and add to db as new user until they get a role.
            {
                // Do welcoming stuff.
                await arg.AddRoleAsync(arg.Guild.GetRole(645413078405611540));
                await arg.Guild.GetTextChannel(665723218773934100).SendMessageAsync(string.Format("Welcome {0}, we are currently repairing the server due to a security breach.", arg.Username));
                await arg.SendMessageAsync(string.Format("Hi person named **{0}**, you just joined **{1}** owned by **{2}** so I decided to spam you. I hope you appreciate it! {3} \n\nStatistics of {1}:\nAmount of other people like you: **{4}**", arg.Username, arg.Guild.Name, arg.Guild.Owner, arg.Guild.IconUrl, arg.Guild.MemberCount));

                // Add user to db as a new user until they get another role.
                await AddUser(arg.Id);
            }
        }

        public async Task Client_UserUpdated(SocketUser arg1, SocketUser arg2)
        {
            await UpdateDB();
        }

        public async Task Client_RoleUpdated(SocketRole arg1, SocketRole arg2)
        {
            await UpdateDB();
        }

        public async Task Quarentine(ulong userId, ulong guildId, bool isBot)
        {
            // Give the user the quarantine role.
            await _client.GetGuild(guildId).GetUser(userId).AddRoleAsync(_client.GetGuild(guildId).GetRole(645413078405611540));

            // Add to db.
            if (!await AddUser(userId, "Quarantined"))
                await UpdateUser(userId, "Quarantined");
                // If the user already exists, update the DB (user already has quarantine role and will be automatically quarantined when updated)

            // Send owner a DM.
            if (isBot)
                await _client.GetGuild(guildId).Owner.SendMessageAsync("Someone invited a bot to your server that was not whitelisted. Here are the details:");
            else
                await _client.GetGuild(guildId).Owner.SendMessageAsync($"{_client.GetUser(userId).Username} just joined your server but their account is only {DateTime.Now.Subtract(_client.GetUser(userId).CreatedAt.Date).TotalDays} days old!");
        }

        public async Task<bool> AddUser(ulong userId, string status = "New User")
        {
            Console.WriteLine($"Adding user {_client.GetUser(userId).ToString()} ({userId}) to the database.");

            // Ensure that a user profile has not already been created.
            List<DataRow> serverUsers = new List<DataRow>();

            // Start REading database.

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM MemberServerStatus", connectionString))
            {
                await connection.OpenAsync();

                //get the infraction data
                DataTable serverUsersTable = new DataTable();
                adapter.Fill(serverUsersTable);

                serverUsers.Capacity = serverUsersTable.Rows.Count; //resize it to minimum size

                foreach (DataRow row in serverUsersTable.Rows)
                {
                    serverUsers.Add(row);
                }
            }

            // Do the check
            foreach (DataRow row in serverUsers)
            {
                if (Convert.ToUInt64(row[1]) == userId)
                    return false;
            }

            // Verified not a duplicate

            // Add the new user

            string query = "INSERT INTO MemberServerStatus VALUES (@Username, @UserId, @Status)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                //convert user id
                long longId = Convert.ToInt64(userId);

                //set the values
                command.Parameters.AddWithValue("@Username", _client.GetUser(userId).ToString());
                command.Parameters.AddWithValue("@UserId", longId);
                command.Parameters.AddWithValue("@Status", status);

                //Execute and add the user into the db
                command.ExecuteNonQuery();
            }

            return true;
        }

        public async Task UpdateDB()
        {
            // Cycles the users to make sure everything is up to date
            Console.WriteLine("Updating Database");

            // Read the DB first
            // connect to database.

            // Setup DataRow List to read and write to database.
            List<DataRow> serverUsers = new List<DataRow>();

            // Start REading database.

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM MemberServerStatus", connectionString))
            {
                await connection.OpenAsync();

                //get the infraction data
                DataTable serverUsersTable = new DataTable();
                adapter.Fill(serverUsersTable);

                serverUsers.Capacity = serverUsersTable.Rows.Count; //resize it to minimum size

                foreach (DataRow row in serverUsersTable.Rows)
                {
                    serverUsers.Add(row);
                }
            }

            // Get list of IDs and statuses.
            List<ulong> IDs = new List<ulong>();
            foreach (DataRow row in serverUsers)
                IDs.Add(Convert.ToUInt64(row[1]));

            foreach (SocketGuild guild in _client.Guilds)
            {
                foreach (SocketGuildUser user in guild.Users)
                {
                    // See what the user should be listed as.
                    if (user.Roles.Count == 0)
                        await Quarentine(user.Id, guild.Id, user.IsBot); //if the user has no roles, quarantine it for security

                    string status = "Whitelisted"; // If the user does not have a joined or quarantined role, they are whitelisted.
                    foreach (SocketRole role in user.Roles)
                    {
                        switch (role.Id)
                        {
                            case 645413078405611540: //quarantine is first priority
                                status = "Quarantined";
                                break;
                            case 599636749156745216: //joined is next priority
                                status = "New User";
                                break;
                        }
                        if (status != "Whitelisted")
                            break; //only continue looking for roles if a role has not already been found
                    }
                    if (!IDs.Contains(user.Id)) // If the user does not exist, then create an entry in the database
                        await AddUser(user.Id, status);
                    else // Otherwise, update the existing user
                        await UpdateUser(user.Id, status);
                }
            }
        }

        public async Task UpdateUser(ulong userId, string status)
        {
            Console.WriteLine($"Updating user {_client.GetUser(userId).ToString()} ({userId}) from the database.");

            string query = "UPDATE MemberServerStatus SET Username = @Username, Status = @Status WHERE UserID = @UserId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                //convert user id
                long longId = Convert.ToInt64(userId);

                //set the values
                command.Parameters.AddWithValue("@Username", _client.GetUser(userId).Username);
                command.Parameters.AddWithValue("@UserId", longId);
                command.Parameters.AddWithValue("@Status", status);

                //Execute and add the user into the db
                command.ExecuteNonQuery();
            }
        }
    }
}

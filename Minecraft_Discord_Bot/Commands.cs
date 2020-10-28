using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Minecraft_Discord_Bot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private CommandService _commands;
        public Commands(CommandService c) { this._commands = c; }

        [Command("commands")]
        [Alias("cmds", "cmd")]
        [Summary("Displays all commands")]
        public async Task ShowCommands([Remainder] string rem = null)
        {
            List<CommandInfo> commands = _commands.Commands.ToList();
            var embd = new EmbedBuilder();
            embd.Title = "Commands:";
            embd.WithColor(Color.Red);
            foreach (CommandInfo ci in commands)
            {
                string aliasList = "";
                if (ci.Aliases.Count > 1)
                {
                    int count = 1;
                    foreach (string alias in ci.Aliases)
                    {
                        if (count != 1)
                        {
                            aliasList += alias;
                            if (count != ci.Aliases.Count)
                            {
                                aliasList += ", ";
                            }
                        }
                        count++;
                    }
                }
                embd.AddField($"!{ci.Name}", $"{ci.Summary} \n\n Alias: {aliasList}", true);
            }
            await ReplyAsync(embed: embd.Build());
        }

        [Command("anchor")]
        [Alias("anch", "liveHere")]
        [Summary("Anchors the bot to the channel this command is called in.")]
        public async Task AnchorBot([Remainder] string rem = null)
        {
            var serverID = Context.Guild.Id;
            var channelID = Context.Message.Channel.Id;
            try
            {
                SetServerProperties(serverID.ToString(), channelID.ToString());
                var chan = GetChannelID();
                if(!string.IsNullOrEmpty(chan))
                {
                    await ReplyAsync($"Successfuly anchored this channel.");
                }
                else
                {
                    await ReplyAsync($"Error: Unable to anchor to channel.");
                }
                return;
            }
            catch(SqlException e)
            {
                if (e.Number == 2627)
                {
                    try
                    {
                        UpdateChannel(serverID.ToString(), channelID.ToString());
                        await ReplyAsync($"Successfuly anchored this channel.");
                        return;
                    }
                    catch
                    {
                        await ReplyAsync($"Error updating channel: {e.Message}");
                        return;
                    }
                }
                await ReplyAsync($"Error updating channel - (server error: {e.Message})");
                return;
            }
        }

        public static void SetServerProperties(string serverID, string channelID)
        {
            using (SqlConnection connection = new SqlConnection(@"Server=tcp:vgaravsql.database.windows.net,1433;Initial Catalog=Minecraft_Db;Persist Security Info=False;User ID=vinnyg96;Password=Volcom24--;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
            using (SqlCommand cmd = new SqlCommand("INSERT INTO Discord (ServerID, ChannelID) VALUES (@ServerID, @ChannelID)", connection))
            {
                cmd.Parameters.AddWithValue("ServerID", serverID);
                cmd.Parameters.AddWithValue("ChannelID", channelID);
                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }
        public static void UpdateChannel(string serverID, string channelID)
        {
            using (SqlConnection connection = new SqlConnection(@"Server=tcp:vgaravsql.database.windows.net,1433;Initial Catalog=Minecraft_Db;Persist Security Info=False;User ID=vinnyg96;Password=Volcom24--;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
            using (SqlCommand cmd = new SqlCommand("UPDATE Discord SET ChannelID= @ChannelID WHERE ServerID = @ServerID", connection))
            {
                cmd.Parameters.AddWithValue("ChannelID", channelID);
                cmd.Parameters.AddWithValue("ServerID", serverID);
                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public static string GetChannelID()
        {
            using (SqlConnection connection = new SqlConnection(@"Server=tcp:vgaravsql.database.windows.net,1433;Initial Catalog=Minecraft_Db;Persist Security Info=False;User ID=vinnyg96;Password=Volcom24--;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
            using (SqlCommand cmd = new SqlCommand($"SELECT * FROM Discord", connection))
            {
                connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Check is the reader has any rows at all before starting to read.
                    if (reader.HasRows)
                    {
                        // Read advances to the next row.
                        while (reader.Read())
                        {
                            string serverID = reader.GetString(reader.GetOrdinal("ServerID"));
                            string channelID = reader.GetString(reader.GetOrdinal("ChannelID"));
                            if(!string.IsNullOrEmpty(channelID))
                            {
                                connection.Close();
                                return channelID;
                            }
                        }
                    }
                }
                connection.Close();
            }
            return null;
        }
    }
}

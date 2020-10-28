using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Minecraft_Discord_Bot.Keys;

namespace Minecraft_Discord_Bot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();


        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private SocketUserMessage message;
        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            string token = APIKeys.DiscordToken;


            _client.Log += _client_Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();
            await Task.Run(() => PollSubscriptions());

            await Task.Delay(-1);

        }
        private void saveChannelId()
        {
            //var t = message.Channel.Id;
        }
        private async Task PollSubscriptions()
        {
            int playerCount = 0;
            while (true)
            {
                string chanIDStr = "";
                ServerPing sp = new ServerPing();
                try
                { 
                    chanIDStr = Commands.GetChannelID();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                if (!string.IsNullOrEmpty(chanIDStr))
                {
                    ulong chanID = Convert.ToUInt64(chanIDStr);
                    var chan = _client.GetChannel(chanID) as IMessageChannel;
                    Task<PingPayload> payload = sp.Ping("", "");
                    if (payload.Result != null)
                    {
                        PingPayload pp = payload.Result;
                        if (pp.Players.Sample != null)
                        {
                            if (playerCount < pp.Players.Sample.Count)
                            {
                                playerCount = pp.Players.Sample.Count;

                                EmbedBuilder emb = new EmbedBuilder();
                                emb.AddField("Online", $"{pp.Players.Online}/{pp.Players.Max}", false)
                                    .WithColor(Color.Green)
                                    .WithThumbnailUrl("https://i.imgur.com/qr3QuK6.png")
                                    .WithFooter($"Minecraft Version {pp.Version.Name}")
                                    .WithTitle($"{pp.Motd}");

                                if (pp.Players.Sample != null)
                                {
                                    string playersConnected = "";
                                    foreach (Player p in pp.Players.Sample)
                                    {
                                        playersConnected += $"{p.Name}\n";
                                    }
                                    emb.AddField("Players Connected:", playersConnected, false);
                                }

                                await chan.SendMessageAsync(embed: emb.Build());
                            }
                        }
                        else
                        {
                            playerCount = 0;
                        }
                    }
                }
                Thread.Sleep(30000);
            }
        }
        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message.Author.IsBot) return;

            int argPos = 0;
            if (message.HasStringPrefix("!", ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
                if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}

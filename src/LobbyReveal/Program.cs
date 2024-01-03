using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ekko;
using Spectre.Console;

namespace LobbyReveal
{
    internal class Program
    {
        private static List<LobbyHandler> _handlers = new List<LobbyHandler>();
        private static bool _update = true;

        public static Task Main(string[] args)
        {
            Console.Title = "LR";
            var watcher = new LeagueClientWatcher();
            watcher.OnLeagueClient += (clientWatcher, client) =>
            {
                /*Console.WriteLine(client.Pid);*/
                var handler = new LobbyHandler(new LeagueApi(client.ClientAuthInfo.RiotClientAuthToken,
                    client.ClientAuthInfo.RiotClientPort));
                _handlers.Add(handler);
                handler.OnUpdate += (lobbyHandler, names) => { _update = true; };
                handler.Start();
                _update = true;
            };
            new Thread(async () => { await watcher.Observe(); })
            {
                IsBackground = true
            }.Start();

            new Thread(() => { Refresh(); })
            {
                IsBackground = true
            }.Start();


            while (true)
            {
                var input = Console.ReadKey(true);
                if (!int.TryParse(input.KeyChar.ToString(), out var i) || i > _handlers.Count || i < 1)
                {
                    Console.WriteLine("Invalid input.");
                    _update = true;
                    continue;
                }

                // OPGG
                /*var link =
                    $"https://www.op.gg/multisearch/{_handlers[i - 1].GetRegion() ?? Region.EUW}?summoners=" +
                    HttpUtility.UrlEncode($"{string.Join(",", _handlers[i - 1].GetSummoners())}");
                */
                // Porofessor
                var link =
                    $"https://porofessor.gg/en/pregame/{(_handlers[i - 1].GetRegion() ?? Region.EUW).ToString().ToLower()}/" +
                    HttpUtility.UrlEncode($"{string.Join(",", _handlers[i - 1].GetSummoners())}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(link);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", link);
                }
                else
                {
                    Process.Start("open", link);
                }
                _update = true;
            }

        }

        private static void Refresh()
        {
            while (true)
            {
                if (_update)
                {
                    Console.Clear();
                    Console.WriteLine();
                    Console.WriteLine();
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        // OPGG
                        /*var link =
                            $"https://www.op.gg/multisearch/{_handlers[i].GetRegion() ?? Region.EUW}?summoners=" +
                            HttpUtility.UrlEncode($"{string.Join(",", _handlers[i].GetSummoners())}");*/

                        // Porofessor
                        var link =
                            $"https://porofessor.gg/en/pregame/{(_handlers[i - 1].GetRegion() ?? Region.EUW).ToString().ToLower()}/" +
                            HttpUtility.UrlEncode($"{string.Join(",", _handlers[i - 1].GetSummoners())}");

                        AnsiConsole.Write(
                            new Panel(new Text($"{string.Join("\n", _handlers[i].GetSummoners())}\n\n{link}")
                                    .LeftJustified())
                                .Expand()
                                .SquareBorder()
                                .Header($"[red]Client {i + 1}[/]"));
                        Console.WriteLine();
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                    AnsiConsole.Write(new Markup("[u][cyan][b]Type the client number to open op.gg![/][/][/]")
                        .LeftJustified());
                    Console.WriteLine();
                    _update = false;
                }

                Thread.Sleep(2000);
            }
        }
    }
}
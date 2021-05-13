using LightVPN.CLI.Auth;
using LightVPN.CLI.Auth.Exceptions;
using LightVPN.CLI.Auth.Interfaces;
using LightVPN.CLI.Auth.Models;
using LightVPN.CLI.Common;
using LightVPN.CLI.Common.Models;
using LightVPN.CLI.Utils;
using LightVPN.Settings;
using LightVPN.Settings.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightVPN.CLI
{
    class Program
    {
        private static readonly IHttp _http = new Http(new ApiHttpClient(new HttpClientHandler
        {
            Proxy = null,
            UseProxy = false,
            ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>
            {
                if (!cert.Issuer.ToLower().Contains("cloudflare") || error != System.Net.Security.SslPolicyErrors.None)
                {
                    return false;
                }
                return true;
            },
        }));
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.WriteLine(@".##......###..######..##.....#.#######.##.....#.########.##....##
.##.......##.##....##.##.....#....##...##.....#.##.....#.###...##
.##.......##.##.......##.....#....##...##.....#.##.....#.####..##
.##.......##.##...###.########....##...##.....#.########.##.##.##
.##.......##.##....##.##.....#....##....##...##.##.......##..####
.##.......##.##....##.##.....#....##.....##.##..##.......##...###
.#######.###..######..##.....#....##......###...##.......##....##");

            Console.ForegroundColor = ConsoleColor.White;

            try
            {
                if (File.Exists(Globals.AuthPath))
                {
                    Console.WriteLine("\n[-] Attempting to authenticate you via session...");

                    try
                    {

                        var authFileContent = Encryption.Decrypt(await File.ReadAllTextAsync(Globals.AuthPath));

                        var json = JsonSerializer.Deserialize<AuthModel>(authFileContent);

                        var sessionResult = await _http.ValidateSessionAsync(json.Username, json.SessionId);

                        if (!sessionResult)
                        {
                            File.Delete(Globals.AuthPath);

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[/] Your session ID is invalid, it has been cleared, please relaunch LightVPN.");
                            return;
                        }
                        else
                        {
                            Globals.SessionId = json.SessionId;

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[!] Authentication success!");
                        }
                    }
                    catch (CorruptedAuthSettingsException)
                    {
                        File.Delete(Globals.AuthPath);

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[/] Your authentication data has corrupted, it has been cleared, please relaunch LightVPN.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("\n[*] Since this is your first time using LightVPN, please enter your account ID and password.");

                    Console.ForegroundColor = ConsoleColor.Cyan;

                    Console.Write("[!] Account ID: ");

                    Console.ForegroundColor = ConsoleColor.White;

                    string accountId = Console.ReadLine();

                    Console.ForegroundColor = ConsoleColor.Cyan;

                    Console.Write($"[!] Hi there, {accountId}. Please enter your password: ");

                    Console.ForegroundColor = ConsoleColor.White;

                    string password = ConsoleUtils.ReadPassword();

                    Console.WriteLine("[-] Attempting to authenticate you...");

                    var authResult = await _http.LoginAsync(accountId, password);

                    Globals.SessionId = authResult.SessionId;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[!] Authentication success!");

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[-] Saving & encrypting session ID...");

                    var auth = new AuthModel
                    {
                        SessionId = authResult.SessionId,
                        Username = accountId
                    };

                    var json = JsonSerializer.Serialize(auth);

                    if (!Directory.Exists(Globals.SettingsPath)) Directory.CreateDirectory(Globals.SettingsPath);

                    await File.WriteAllTextAsync(Globals.AuthPath, Encryption.Encrypt(json));

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[!] Done!");
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[-] Fetching servers...");

                var table = new TableHandler();

                table.SetHeaders("ID", "Country", "Name", "Status", "Type");

                var servers = await _http.GetServersAsync();

                uint serverId = 0;

                foreach (var item in servers)
                {
                    table.AddRow(serverId.ToString(), item.CountryName, item.ServerName, item.Status ? "Online" : "Offline", item.Type.ToString());
                    serverId++;
                }

                Console.Write(table.ToString());

                Console.ForegroundColor = ConsoleColor.Cyan;

                Console.Write("[!] Enter the server ID: ");

                Console.ForegroundColor = ConsoleColor.White;

                var id = Console.ReadLine();


            }
            catch (ClientUpdateRequired)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[/] Client update required! Please update via the website.");
                return;
            }
            catch (InvalidResponseException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[/] {e.Message}");
                return;
            }
            catch (RatelimitedException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[/] {e.Message}");
                return;
            }
            catch (HttpRequestException e)
            {
                switch (e.Message)
                {
                    case "The SSL connection could not be established, see inner exception.":
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[/] API authenticity could not be verified (SSL failure)");
                        return;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[/] API request has failed");
                        return;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[/] Unknown error: {e}");
                return;
            }
        }
    }
}

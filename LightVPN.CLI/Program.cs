using LightVPN.Auth;
using LightVPN.Auth.Exceptions;
using LightVPN.Auth.Interfaces;
using LightVPN.CLI.Common;
using LightVPN.CLI.Common.Models;
using LightVPN.CLI.Utils;
using LightVPN.CLI.Utils.Events;
using LightVPN.OpenVPN;
using LightVPN.OpenVPN.Interfaces;
using LightVPN.Settings;
using LightVPN.Settings.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightVPN.CLI
{
    [SupportedOSPlatform("linux")]
    public class Program
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
        }), PlatformID.Unix);

        private static readonly IManager _manager = new Manager("/usr/bin/openvpn", PlatformID.Unix);

        private static readonly OpenVPN.Utils.Linux.TapManager _tapMan = new();

        static async Task Main(string[] args)
        {
            //Console.WriteLine($"[DEBUG] First arg: {args.First()}, last arg: {args.Last()} ({args.Length})");

            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.WriteLine(@"██╗░░░░░██╗░██████╗░██╗░░██╗████████╗██╗░░░██╗██████╗░███╗░░██╗
██║░░░░░██║██╔════╝░██║░░██║╚══██╔══╝██║░░░██║██╔══██╗████╗░██║
██║░░░░░██║██║░░██╗░███████║░░░██║░░░╚██╗░██╔╝██████╔╝██╔██╗██║
██║░░░░░██║██║░░╚██╗██╔══██║░░░██║░░░░╚████╔╝░██╔═══╝░██║╚████║
███████╗██║╚██████╔╝██║░░██║░░░██║░░░░░╚██╔╝░░██║░░░░░██║░╚███║
╚══════╝╚═╝░╚═════╝░╚═╝░░╚═╝░░░╚═╝░░░░░░╚═╝░░░╚═╝░░░░░╚═╝░░╚══╝");

            Console.ForegroundColor = ConsoleColor.White;

            _manager.Error += ErrorEvent.Error;
            _manager.Connected += ConnectedEvent.Connected;
            _manager.Output += Output;

            try
            {
                if (File.Exists(LightVPN.Common.Models.Globals.LinuxAuthPath))
                {
                    Console.WriteLine("\n[-] Attempting to authenticate you via session...");

                    try
                    {

                        var authFileContent = Encryption.Decrypt(await File.ReadAllTextAsync(LightVPN.Common.Models.Globals.LinuxAuthPath));

                        var json = JsonSerializer.Deserialize<AuthModel>(authFileContent);

                        var sessionResult = await _http.ValidateSessionAsync(json.Username, json.SessionId);

                        if (!sessionResult)
                        {
                            File.Delete(LightVPN.Common.Models.Globals.LinuxAuthPath);

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[/] Your session ID is invalid, it has been cleared, please relaunch LightVPN.");
                            return;
                        }
                        else
                        {
                            LightVPN.Common.Models.Globals.LinuxSessionId = json.SessionId;

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[!] Authentication success!");
                        }
                    }
                    catch (CorruptedAuthSettingsException)
                    {
                        File.Delete(LightVPN.Common.Models.Globals.LinuxAuthPath);

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

                    LightVPN.Common.Models.Globals.LinuxSessionId = authResult.SessionId;

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

                    if (!Directory.Exists(LightVPN.Common.Models.Globals.LinuxSettingsPath)) Directory.CreateDirectory(LightVPN.Common.Models.Globals.LinuxSettingsPath);

                    await File.WriteAllTextAsync(LightVPN.Common.Models.Globals.LinuxAuthPath, Encryption.Encrypt(json));

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[!] Done!");
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[-] Fetching servers...");

                var table = new TableHandler();

                table.SetHeaders("ID", "Country", "Name", "Status", "Type");

                var servers = await _http.GetServersAsync();

                uint serverId = 0;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[-] Caching servers...");

                await _http.CacheConfigsAsync();

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

                if (!int.TryParse(id, out int serverIndex))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[/] Invalid input, must be a number!");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[-] Checking TAP interface...");

                if (!_tapMan.IsAdapterExistant())
                {
                    Console.WriteLine("[-] Creating TAP interface (tun0)...");
                    _tapMan.CreateTapAdapter();
                }

                Console.ForegroundColor = ConsoleColor.Cyan;

                var serverObj = servers.ToList()[serverIndex];

                Console.WriteLine($"[-] Resolving configuration file");
                
                var configPath = ConfigResolver.GetConfigPath(serverObj.FileName);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[-] Connecting to the server...");

                await _manager.ConnectAsync(configPath);

                await Task.Delay(-1);
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

        private static void Output(object sender, Manager.OutputType e, string message)
        {
            Console.WriteLine($"[DEBUG ({e})] {message}");
        }
    }
}

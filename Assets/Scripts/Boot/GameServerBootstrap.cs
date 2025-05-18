using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Network.Server;
using UnityEngine;

namespace Boot
{
    public class GameServerBootstrap : MonoBehaviour
    {
        [SerializeField] private int defaultPort = 12346;

        private void Awake()
        {
            int port = defaultPort;
            int serverId = 0;
            string matchmakerIp = "127.0.0.1";
            int matchmakerPort = 12345;

            try
            {
                string[] args = Environment.GetCommandLineArgs();

                // Parse port
                int portIndex = Array.IndexOf(args, "-port");
                if (portIndex >= 0 && portIndex < args.Length - 1)
                {
                    if (int.TryParse(args[portIndex + 1], out int parsedPort))
                    {
                        port = parsedPort;
                        Debug.Log($"Using port from command line: {port}");
                    }
                }

                // Parse server ID
                int serverIdIndex = Array.IndexOf(args, "-serverId");
                if (serverIdIndex >= 0 && serverIdIndex < args.Length - 1)
                {
                    if (int.TryParse(args[serverIdIndex + 1], out int parsedServerId))
                        serverId = parsedServerId;
                }

                // Parse matchmaker IP
                int matchmakerIpIndex = Array.IndexOf(args, "-matchmakerIp");
                if (matchmakerIpIndex >= 0 && matchmakerIpIndex < args.Length - 1)
                {
                    matchmakerIp = args[matchmakerIpIndex + 1];
                }

                // Parse matchmaker port
                int matchmakerPortIndex = Array.IndexOf(args, "-matchmakerPort");
                if (matchmakerPortIndex >= 0 && matchmakerPortIndex < args.Length - 1)
                {
                    if (int.TryParse(args[matchmakerPortIndex + 1], out int parsedMatchmakerPort))
                        matchmakerPort = parsedMatchmakerPort;
                }

                if (!IsPortAvailable(port))
                {
                    // Try to find an available port
                    port = GetNextAvailablePort(port + 1);
                    Debug.Log($"Original port was busy, using port: {port}");
                }

                Debug.Log(
                    $"Starting game server #{serverId} on port {port}, reporting to matchmaker at {matchmakerIp}:{matchmakerPort}");

                // Store server info in PlayerPrefs
                PlayerPrefs.SetInt("ServerId", serverId);
                PlayerPrefs.SetString("MatchmakerIp", matchmakerIp);
                PlayerPrefs.SetInt("MatchmakerPort", matchmakerPort);
                PlayerPrefs.Save();

                // Create and configure the server manager
                UnityServerManager serverManager;
                
                GameObject serverObj = new GameObject("ServerManager");
                serverManager = serverObj.AddComponent<UnityServerManager>();
                

                // Configure the server manager
                serverManager.ServerPort = port;
                serverManager.ServerId = serverId;
                serverManager.MatchmakerEndpoint = new IPEndPoint(IPAddress.Parse(matchmakerIp), matchmakerPort);
                Thread.Sleep(100);
                serverManager.StartServer();
                // Keep this object alive
                DontDestroyOnLoad(gameObject);
                if (serverManager != null)
                {
                    DontDestroyOnLoad(serverManager.gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing game server: {ex.Message}");
            }
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private int GetNextAvailablePort(int startPort)
        {
            int port = startPort;
            while (!IsPortAvailable(port))
            {
                port++;
                if (port > 65535) break;
            }

            return port;
        }
    }
}
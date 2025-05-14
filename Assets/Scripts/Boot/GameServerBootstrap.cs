using System;
using MultiplayerLib.Network.Server;
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
            
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                
                // Find and parse port argument
                int portIndex = Array.IndexOf(args, "-port");
                if (portIndex >= 0 && portIndex < args.Length - 1)
                {
                    if (int.TryParse(args[portIndex + 1], out int parsedPort))
                        port = parsedPort;
                }
                
                int serverIdIndex = Array.IndexOf(args, "-serverId");
                if (serverIdIndex >= 0 && serverIdIndex < args.Length - 1)
                {
                    if (int.TryParse(args[serverIdIndex + 1], out int parsedServerId))
                        serverId = parsedServerId;
                }
                
                Debug.Log($"Starting game server #{serverId} on port {port}");

                ServerNetworkManager networkManager = new ServerNetworkManager();
                if (networkManager != null)
                {
                    networkManager.StartServer(port);
                    
                    PlayerPrefs.SetInt("ServerId", serverId);
                }
                else
                {
                    Debug.LogError("ServerNetworkManager not found in scene!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing game server: {ex.Message}");
            }
        }
    }
}
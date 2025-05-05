using System;
using System.Net;
using Network.ClientDir;
using Network.Server;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkManagerFactory : MonoBehaviour
    {
        [SerializeField] private ServerNetworkManager serverManagerPrefab;
        [SerializeField] private ClientNetworkManager clientManagerPrefab;
        [SerializeField] private Button DisconnectButton;

        public ServerNetworkManager CreateServerManager(int port)
        {
            ServerNetworkManager manager = Instantiate(serverManagerPrefab);
            manager.StartServer(port);
            DisconnectButton.onClick.AddListener(() =>
            {
                manager.Dispose();
                if (Application.isEditor)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
                else
                {
                    Application.Quit();
                }
            });
            return manager;
        }

        public ClientNetworkManager CreateClientManager(IPAddress ip, int port)
        {
            ClientNetworkManager manager = Instantiate(clientManagerPrefab);
            manager.StartClient(ip, port);
            DisconnectButton.onClick.AddListener(() =>
            {
                manager.Dispose();
                if (Application.isEditor)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
                else
                {
                    Application.Quit();
                }
            });
            return manager;
        }
    }
}
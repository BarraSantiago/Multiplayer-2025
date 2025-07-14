using System.Net;
using AuthClient.Network.ClientDir;
using Game;
using MultiplayerLib.Network.ClientDir;
using Network.ClientDir;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ACNetworkScreen : MonoBehaviour
    {
        [SerializeField] GameObject chatScreen;
        [SerializeField] private TMP_Dropdown ColorSelector;
        [SerializeField] private InputField PlayerNameInput;
        public Button connectBtn;
        public Button disconnectBtn;
        public InputField portInputField;
        public InputField addressInputField;
        private ACClientManager clientManager;
        protected void Awake()
        {
            connectBtn.onClick.AddListener(OnConnectBtnClick);
            clientManager = FindFirstObjectByType<ACClientManager>();
        }

        private void OnConnectBtnClick()
        {
            IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
            int port = System.Convert.ToInt32(portInputField.text);

            CreateClientManager(ipAddress, port);

            SwitchToChatScreen();
        }

        private void CreateClientManager(IPAddress ipAddress, int port)
        {
            disconnectBtn.onClick.AddListener(clientManager._networkManager.Dispose);
            
            
            if (clientManager._networkManager != null)
            {
                ACClientNetworkManager.PlayerColor = ColorSelector.value;
                clientManager.ConnectToServer(ipAddress, port, PlayerNameInput.text, ColorSelector.value);
            }
            else
            {
                Debug.LogError("ClientNetworkManager not found in scene!");
            }
        }

        private void SwitchToChatScreen()
        {
            chatScreen.SetActive(true);
            this.gameObject.SetActive(false);
        }
    }
}
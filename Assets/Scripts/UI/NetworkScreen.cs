using System.Net;
using Network;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI
{
    public class NetworkScreen : MonoBehaviour
    {
        [SerializeField] GameObject chatScreen;
        public Button connectBtn;
        public Button startServerBtn;
        public InputField portInputField;
        public InputField addressInputField;
        private NetworkManagerFactory _networkManagerFactory;

        protected void Awake()
        {
            connectBtn.onClick.AddListener(OnConnectBtnClick);
            startServerBtn.onClick.AddListener(OnStartServerBtnClick);
            _networkManagerFactory = FindAnyObjectByType<NetworkManagerFactory>();
        }

        private void OnConnectBtnClick()
        {
            IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
            int port = System.Convert.ToInt32(portInputField.text);

            _networkManagerFactory.CreateClientManager(ipAddress, port);

            SwitchToChatScreen();
        }

        private void OnStartServerBtnClick()
        {
            int port = System.Convert.ToInt32(portInputField.text);
            _networkManagerFactory.CreateServerManager(port);
            SwitchToChatScreen();
        }

        private void SwitchToChatScreen()
        {
            chatScreen.SetActive(true);
            this.gameObject.SetActive(false);
        }
    }
}
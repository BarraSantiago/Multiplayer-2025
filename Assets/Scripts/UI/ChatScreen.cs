using Network;
using Network.ClientDir;
using Network.interfaces;
using Network.Messages;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI
{
    public class ChatScreen : MonoBehaviour
    {
        public Text messages;
        public InputField inputMessage;
        private ClientNetworkManager _clientNetworkManager;
        protected void Awake()
        {
            inputMessage.onEndEdit.AddListener(OnEndEdit);

            this.gameObject.SetActive(false);

            BaseMessageDispatcher.OnConsoleMessageReceived += OnReceiveMessage;
            _clientNetworkManager ??= FindAnyObjectByType<ClientNetworkManager>();
        }

        private void OnReceiveMessage(string message)
        {
            messages.text += message + System.Environment.NewLine;
        }

        private void OnEndEdit(string str)
        {
            if (inputMessage.text == "") return;
            
            _clientNetworkManager?.SendToServer(inputMessage.text, MessageType.Console);

            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }
    }
}
using MultiplayerLib.Network.ClientDir;
using MultiplayerLib.Network.interfaces;
using MultiplayerLib.Network.Messages;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ChatScreen : MonoBehaviour
    {
        public Text messages;
        public InputField inputMessage;
        protected void Awake()
        {
            inputMessage.onEndEdit.AddListener(OnEndEdit);

            this.gameObject.SetActive(false);

            BaseMessageDispatcher.OnConsoleMessageReceived += OnReceiveMessage;
        }

        private void OnReceiveMessage(string message)
        {
            messages.text += message + System.Environment.NewLine;
        }

        private void OnEndEdit(string str)
        {
            if (inputMessage.text == "") return;
            
            ClientNetworkManager.OnSendToServer?.Invoke(inputMessage.text, MessageType.Console, false);
            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }
    }
}
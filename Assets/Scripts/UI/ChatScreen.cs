using Network;
using Network.Messages;
using UnityEngine.UI;
using Utils;

namespace UI
{
    public class ChatScreen : MonoBehaviourSingleton<ChatScreen>
    {
        public Text messages;
        public InputField inputMessage;

        protected override void Initialize()
        {
            inputMessage.onEndEdit.AddListener(OnEndEdit);

            this.gameObject.SetActive(false);

            MessageDispatcher.OnConsoleMessageReceived += OnReceiveMessage;
        }

        private void OnReceiveMessage(string message)
        {
            messages.text += message + System.Environment.NewLine;
        }

        private void OnEndEdit(string str)
        {
            if (inputMessage.text == "") return;

            if (NetworkManager.Instance.IsServer)
            {
                messages.text += inputMessage.text + System.Environment.NewLine;
            }
            else
            {
                NetworkManager.Instance.SendToServer(inputMessage.text, MessageType.Console);
            }

            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }
    }
}
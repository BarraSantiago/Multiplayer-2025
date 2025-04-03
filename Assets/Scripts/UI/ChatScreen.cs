using System.Net;
using Network;
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

            NetworkManager.Instance.OnReceiveEvent += OnReceiveDataEvent;
        }

        void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
        {
            if (NetworkManager.Instance.IsServer)
            {
                NetworkManager.Instance.Broadcast(data);
            }

            messages.text += System.Text.ASCIIEncoding.UTF8.GetString(data) + System.Environment.NewLine;
        }

        void OnEndEdit(string str)
        {
            if (inputMessage.text == "") return;

            if (NetworkManager.Instance.IsServer)
            {
                NetworkManager.Instance.Broadcast(System.Text.ASCIIEncoding.UTF8.GetBytes(inputMessage.text));
                messages.text += inputMessage.text + System.Environment.NewLine;
            }
            else
            {
                NetworkManager.Instance.SendToServer(System.Text.ASCIIEncoding.UTF8.GetBytes(inputMessage.text));
            }

            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }
    }
}
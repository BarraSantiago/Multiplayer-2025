using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GameResult : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text resultText;
        [SerializeField] Button exitButton;

        private void Awake()
        {
            exitButton.onClick.AddListener(() => { Application.Quit(); });
        }

        public void OnGameResult(bool arg1, float arg2)
        {
            background.gameObject.SetActive(true);
            if (arg1)
            {
                background.color = Color.green;
                resultText.text = "You Win! Elo: " + arg2;
            }
            else
            {
                background.color = Color.red;
                resultText.text = "You Lose! Elo: " + arg2;
            }
        }
    }
}
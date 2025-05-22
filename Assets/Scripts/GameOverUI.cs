using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;
    [SerializeField] private Color tieColor;
    [SerializeField] private Button rematchButton;

    private void Awake()
    {
        rematchButton.onClick.AddListener(() => { GameManager.instance.RematchRpc(); });
    }

    private void Start()
    {
        GameManager.instance.OnGameWin += GameManager_OnGameWin;
        GameManager.instance.OnRematch += GameManager_OnRematch;
        GameManager.instance.OnGameTied += GameManager_OnGameTied;

        Hide();
    }

    private void GameManager_OnGameTied(object sender, EventArgs e)
    {
        resultText.text = "TIE!";
        resultText.color = tieColor;
        Show();
    }

    private void GameManager_OnRematch(object sender, EventArgs e)
    {
        Hide();
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if(e.winPlayerType == GameManager.instance.GetLocalPlayerType())
        {
            resultText.text = "YOU WIN!";
            resultText.color = winColor;
        }
        else
        {
            resultText.text = "YOU LOSE!";
            resultText.color = loseColor;
        }

        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}

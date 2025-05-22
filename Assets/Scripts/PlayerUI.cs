using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject crossArrowObject;
    [SerializeField] private GameObject circleArrowObject;
    [SerializeField] private GameObject crossYouTextObject;
    [SerializeField] private GameObject circleYouTextObject;
    [SerializeField] private TMP_Text crossScroreText;
    [SerializeField] private TMP_Text circleScroreText;

    private void Awake()
    {
        crossArrowObject.SetActive(false);
        circleArrowObject.SetActive(false);
        crossYouTextObject.SetActive(false);
        circleYouTextObject.SetActive(false);

        crossScroreText.text = "";
        circleScroreText.text = "";
    }

    private void Start()
    {
        GameManager.instance.OnGameStarted += GameManager_OnGameStarted;
        GameManager.instance.OnCurrentPlayablePlayerTypeChanged += GameManager_OnCurrentPlayablePlayerTypeChanged;
        GameManager.instance.OnScoreChanged += GameManager_OnScoreChanged;
    }

    private void GameManager_OnScoreChanged(object sender, EventArgs e)
    {
        GameManager.instance.GetScores(out int playerCrossScore, out int playerCircleScore);

        crossScroreText.text = playerCrossScore.ToString();
        circleScroreText.text = playerCircleScore.ToString();
    }


    private void GameManager_OnGameStarted(object sender, System.EventArgs e)
    {
        if(GameManager.instance.GetLocalPlayerType() == GameManager.PlayerType.Cross)
        {
            crossYouTextObject.SetActive(true);
        }
        else
        {
            circleYouTextObject.SetActive(true);
        }

        crossScroreText.text = "0";
        circleScroreText.text = "0";

        UpdateCurrentArrow();
    }

    private void GameManager_OnCurrentPlayablePlayerTypeChanged(object sender, System.EventArgs e)
    {
        UpdateCurrentArrow();
    }

    private void UpdateCurrentArrow()
    {
        if(GameManager.instance.GetCurrentPlayablePlayerType() == GameManager.PlayerType.Cross)
        {
            crossArrowObject.SetActive(true);
            circleArrowObject.SetActive(false);
        }
        else
        {
            crossArrowObject.SetActive(false);
            circleArrowObject.SetActive(true);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerScore;
    [SerializeField] private TextMeshProUGUI enemyScore;

    [SerializeField] private OptionsPopup optionsPopup;
    [SerializeField] private GameOverPopup gameOverPopup;

    private int popupsActive = 0;
    public void ShowGameOverPopup()
    {
        gameOverPopup.Open();
    }

    private void Awake()
    {
        Messenger.AddListener(GameEvent.POPUP_OPENED, OnPopupOpened);
        Messenger.AddListener(GameEvent.POPUP_CLOSED, OnPopupClosed);
        Messenger<float>.AddListener(GameEvent.HEALTH_CHANGED, OnHealthChanged);
    }

    private void OnDestroy()
    {
        Messenger.RemoveListener(GameEvent.POPUP_OPENED, OnPopupOpened);
        Messenger.RemoveListener(GameEvent.POPUP_CLOSED, OnPopupClosed);
        Messenger<float>.RemoveListener(GameEvent.HEALTH_CHANGED, OnHealthChanged);
    }

    private void Start()
    {
        UpdateHealth(1.0f);
        SetGameActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && popupsActive == 0) {
            optionsPopup.Open();
        }
    }

    public void UpdateScore(int newScore)
    {
        score.text = "Score: " + newScore.ToString();
    }

    private void UpdateHealth(float healthPercentage)
    {
        healthBar.fillAmount = healthPercentage;
        healthBar.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }

    private void OnHealthChanged(float healthPercentage)
    {
        UpdateHealth(healthPercentage);
    }

    private void OnPopupOpened()
    {
        if (popupsActive == 0)
        {
            SetGameActive(false);
        }
        popupsActive++;
    }

    private void OnPopupClosed()
    {
        popupsActive--;
        if (popupsActive == 0)
        {
            SetGameActive(true);
        }
    }

    public void SetGameActive(bool active)
    {
        if (active)
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            crossHair.gameObject.SetActive(true);
            Messenger.Broadcast(GameEvent.GAME_ACTIVE);
        }
        else
        {
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            crossHair.gameObject.SetActive(false);
            Messenger.Broadcast(GameEvent.GAME_INACTIVE);
        }
    }
}

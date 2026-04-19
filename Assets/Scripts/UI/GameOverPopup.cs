using TMPro;
using UnityEngine;

// Parent under a branch that stays active during play; UIManager toggles InGameUI/PreGameUI only, but a disabled parent hides children regardless of layer.
public class GameOverPopup : BasePopup
{
    private const string PlayerWinText = "Player Wins!";
    private const string EnemyWinText = "Enemy Wins!";
    private const string TieText = "Game Tie!";

    [SerializeField] private TMP_Text gameResultText;
    [SerializeField] private GameObject winningStars;
    [SerializeField] private GameObject losingStars;

    private static readonly Color32 PlayerWinColor = new Color32(0xF2, 0xC7, 0x53, 0xFF);
    private static readonly Color32 EnemyWinColor = new Color32(0xCD, 0x38, 0x4C, 0xFF);
    private static readonly Color32 TieColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    private void OnEnable()
    {
        Messenger.AddListener(GameEvent.GAME_RESULT_PLAYER_WIN, OnPlayerWin);
        Messenger.AddListener(GameEvent.GAME_RESULT_ENEMY_WIN, OnEnemyWin);
        Messenger.AddListener(GameEvent.GAME_RESULT_TIE, OnTie);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAME_RESULT_PLAYER_WIN, OnPlayerWin);
        Messenger.RemoveListener(GameEvent.GAME_RESULT_ENEMY_WIN, OnEnemyWin);
        Messenger.RemoveListener(GameEvent.GAME_RESULT_TIE, OnTie);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    public void OnPlayAgainButton()
    {
        PlayNavigationSfx();
        SoundManager.Instance.StopMusic();

        if (IsActive())
        {
            Close();
        }

        Messenger.Broadcast(GameEvent.GAME_RESET_REQUESTED, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    public void ShowPlayerWinResult()
    {
        ApplyResult(PlayerWinText, PlayerWinColor, true, false);
    }

    public void ShowEnemyWinResult()
    {
        SoundLibrary library = SoundLibrary.Instance;
        if (library != null)
        {
            AudioClip winSfx = library.EnemyWinSfx;
            if (winSfx != null)
            {
                SoundManager.Instance.PlaySfx(winSfx);
            }
        }

        ApplyResult(EnemyWinText, EnemyWinColor, false, true);
    }

    public void ShowTieResult()
    {
        ApplyResult(TieText, TieColor, false, false);
    }

    private void OnPlayerWin()
    {
        ShowPlayerWinResult();
    }

    private void OnEnemyWin()
    {
        ShowEnemyWinResult();
    }

    private void OnTie()
    {
        ShowTieResult();
    }

    private void OnGameResetRequested()
    {
        if (IsActive())
        {
            Close();
        }
    }

    private void ApplyResult(string resultText, Color resultColor, bool showWinningStars, bool showLosingStars)
    {
        gameResultText.text = resultText;
        gameResultText.color = resultColor;
        winningStars.SetActive(showWinningStars);
        losingStars.SetActive(showLosingStars);

        if (!IsActive())
        {
            Open();
        }
    }
}

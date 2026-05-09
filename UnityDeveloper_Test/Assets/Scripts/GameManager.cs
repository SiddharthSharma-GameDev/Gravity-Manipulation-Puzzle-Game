using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float totalTime = 120f;
    [SerializeField] private TMP_Text timerText;

    [Header("Cubes")]
    [SerializeField] private int totalCubes;
    [SerializeField] private TMP_Text cubeCounterText;

    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;

    [Header("Pause Buttons")]
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseExitButton;

    [Header("Win Polish")]
    [SerializeField] private float extraWinDelay = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip pauseSound;

    private float currentTime;
    private int collectedCubes;

    private bool gameplayActive;
    private bool gameEnded;
    private bool winSequenceStarted;
    private bool isPaused;

    public static GameManager Instance { get; private set; }

    public int CollectedCubes => collectedCubes;
    public int TotalCubes => totalCubes;
    public float CurrentTime => currentTime;
    public bool IsGameplayActive => gameplayActive;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        Instance = this;
        SetupPauseButtons();
    }

    private void Start()
    {
        currentTime = totalTime;
        collectedCubes = 0;
        gameplayActive = false;
        gameEnded = false;
        winSequenceStarted = false;
        isPaused = false;

        if (totalCubes <= 0)
            totalCubes = FindObjectsOfType<CollectibleCube>().Length;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        UpdateTimerUI();
        UpdateCubeCounterUI();
    }

    private void Update()
    {
        if (!gameplayActive)
            return;

        if (gameEnded || winSequenceStarted)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();

            return;
        }

        if (isPaused)
            return;

        currentTime -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTime <= 0f)
            GameOver();
    }

    private void LateUpdate()
    {
        if (isPaused || gameEnded)
            UnlockCursor();
    }

    private void SetupPauseButtons()
    {
        if (pauseResumeButton != null)
        {
            pauseResumeButton.onClick.RemoveAllListeners();
            pauseResumeButton.onClick.AddListener(ResumeGame);
        }

        if (pauseExitButton != null)
        {
            pauseExitButton.onClick.RemoveAllListeners();
            pauseExitButton.onClick.AddListener(ExitToMenu);
        }
    }

    public void ActivateGameplay()
    {
        gameplayActive = true;
        gameEnded = false;
        winSequenceStarted = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (hudRoot != null)
            hudRoot.SetActive(true);

        StartCoroutine(LockCursorAfterFrame());
    }

    public void DeactivateGameplay()
    {
        gameplayActive = false;
        isPaused = false;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        UnlockCursor();
    }

    public void CollectCube(float visualDelay = 0f)
    {
        if (!gameplayActive || gameEnded || winSequenceStarted)
            return;

        collectedCubes++;
        UpdateCubeCounterUI();

        if (collectedCubes >= totalCubes)
            StartCoroutine(WinAfterDelay(visualDelay + extraWinDelay));
    }

    private IEnumerator WinAfterDelay(float delay)
    {
        winSequenceStarted = true;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        WinGame();
    }

    public void PauseGame()
    {
        if (!gameplayActive)
            return;

        if (gameEnded || winSequenceStarted)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        PlayUISound(pauseSound);
        UnlockCursor();
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        StartCoroutine(LockCursorAfterFrame());
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        gameplayActive = false;
        isPaused = false;
        FrontendMenuManager.StartDirectlyInGameplay = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GameOver()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        gameplayActive = false;
        Time.timeScale = 0f;

        if (hudRoot != null)
            hudRoot.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        PlayUISound(gameOverSound);
        UnlockCursor();
    }

    public void WinGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        gameplayActive = false;
        Time.timeScale = 0f;

        if (hudRoot != null)
            hudRoot.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(true);

        PlayUISound(winSound);
        UnlockCursor();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        FrontendMenuManager.StartDirectlyInGameplay = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator LockCursorAfterFrame()
    {
        yield return null;
        yield return null;

        if (gameplayActive && !isPaused && !gameEnded)
            LockCursor();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        float safeTime = Mathf.Max(0f, currentTime);
        int minutes = Mathf.FloorToInt(safeTime / 60f);
        int seconds = Mathf.FloorToInt(safeTime % 60f);

        timerText.text = $"Time: {minutes:00}:{seconds:00}";
    }

    private void UpdateCubeCounterUI()
    {
        if (cubeCounterText == null)
            return;

        cubeCounterText.text = $"Cubes: {collectedCubes}/{totalCubes}";
    }

    private void PlayUISound(AudioClip clip)
    {
        if (uiAudioSource == null || clip == null)
            return;

        uiAudioSource.PlayOneShot(clip);
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
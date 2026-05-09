using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrontendMenuManager : MonoBehaviour
{
    public static bool StartDirectlyInGameplay;

    [System.Serializable]
    public class InstructionPage
    {
        public string heading = "INSTRUCTION";
        [TextArea(3, 8)] public string body;
        public Sprite previewSprite;
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Main Menu")]
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private string gameTitle = "GRAVITY SHIFT";
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;

    [Header("Instruction UI")]
    [SerializeField] private TMP_Text instructionHeadingText;
    [SerializeField] private TMP_Text instructionBodyText;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private Image previewImage;
    [SerializeField] private GameObject previewFrame;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipTypingButton;
    [SerializeField] private Button startMissionButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Typing Effect")]
    [SerializeField] private float characterDelay = 0.02f;
    [SerializeField] private float punctuationDelay = 0.05f;
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingClip;

    [Header("Music")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private float menuMusicFadeDuration = 0.35f;

    [Header("Gameplay Lock")]
    [SerializeField] private Behaviour[] gameplayBehavioursToDisableAtMenu;
    [SerializeField] private GameObject[] gameplayObjectsToDisableAtMenu;
    [SerializeField] private GameObject[] gameplayObjectsToEnableAtGameStart;

    [Header("Instruction Content")]
    [SerializeField] private InstructionPage[] instructionPages;

    private int currentPageIndex;
    private Coroutine typingRoutine;
    private bool isTyping;
    private string fullCurrentText = "";
    private bool gameStarted;
    private bool menuModeActive = true;

    private void Awake()
    {
        BuildFallbackInstructionsIfNeeded();
        SetupTitle();
        SetupButtons();

        if (skipTypingButton != null)
            skipTypingButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (StartDirectlyInGameplay)
        {
            StartDirectlyInGameplay = false;
            StartCoroutine(StartDirectGameplayAfterInitialization());
        }
        else
        {
            gameStarted = false;
            LockGameplayForMenu();
            ShowMainMenu();
        }
    }

    private void Update()
    {
        if (menuModeActive && !gameStarted)
            SetCursorUnlocked();
    }

    private void LateUpdate()
    {
        if (menuModeActive && !gameStarted)
            SetCursorUnlocked();
    }

    private IEnumerator StartDirectGameplayAfterInitialization()
    {
        gameStarted = true;
        menuModeActive = false;

        StopTypingSound();
        StopMenuMusicImmediately();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (hudRoot != null)
            hudRoot.SetActive(true);

        yield return null;
        yield return null;

        UnlockGameplayForGame();
    }

    private void SetupTitle()
    {
        if (gameTitleText != null)
            gameTitleText.text = gameTitle;
    }

    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OpenInstructions);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(PreviousInstruction);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextInstruction);
        }

        if (skipTypingButton != null)
        {
            skipTypingButton.onClick.RemoveAllListeners();
            skipTypingButton.onClick.AddListener(SkipTyping);
        }

        if (startMissionButton != null)
        {
            startMissionButton.onClick.RemoveAllListeners();
            startMissionButton.onClick.AddListener(StartGame);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void BuildFallbackInstructionsIfNeeded()
    {
        if (instructionPages != null && instructionPages.Length > 0)
            return;

        instructionPages = new InstructionPage[6];

        instructionPages[0] = new InstructionPage
        {
            heading = "MOVEMENT",
            body = "Use W, A, S and D to move the character inside the gravity chamber.",
            previewSprite = null
        };

        instructionPages[1] = new InstructionPage
        {
            heading = "JUMP",
            body = "Press SPACE to jump. Time your jumps carefully while moving between surfaces.",
            previewSprite = null
        };

        instructionPages[2] = new InstructionPage
        {
            heading = "GRAVITY PREVIEW",
            body = "Press the ARROW KEYS to preview a gravity direction. A hologram will show where gravity will shift.",
            previewSprite = null
        };

        instructionPages[3] = new InstructionPage
        {
            heading = "APPLY GRAVITY",
            body = "After selecting a direction, press ENTER to apply the gravity shift and move to the new surface.",
            previewSprite = null
        };

        instructionPages[4] = new InstructionPage
        {
            heading = "OBJECTIVE",
            body = "Collect all glowing cubes before the 2-minute timer runs out.",
            previewSprite = null
        };

        instructionPages[5] = new InstructionPage
        {
            heading = "WARNING",
            body = "Do not lose contact with surfaces for too long. Free fall will trigger GAME OVER.",
            previewSprite = null
        };
    }

    private void LockGameplayForMenu()
    {
        menuModeActive = true;
        Time.timeScale = 0f;

        if (GameManager.Instance != null)
            GameManager.Instance.DeactivateGameplay();

        SetBehaviourState(false);
        SetObjectState(gameplayObjectsToDisableAtMenu, false);
        SetObjectState(gameplayObjectsToEnableAtGameStart, false);

        if (hudRoot != null)
            hudRoot.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        SetCursorUnlocked();
    }

    private void UnlockGameplayForGame()
    {
        menuModeActive = false;
        Time.timeScale = 1f;

        StopTypingSound();
        StopMenuMusicImmediately();

        SetBehaviourState(true);
        SetObjectState(gameplayObjectsToDisableAtMenu, true);
        SetObjectState(gameplayObjectsToEnableAtGameStart, true);

        if (hudRoot != null)
            hudRoot.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.ActivateGameplay();

        StartCoroutine(LockCursorNextFrame());
    }

    private IEnumerator LockCursorNextFrame()
    {
        yield return null;
        yield return null;
        SetCursorLocked();
    }

    private void SetBehaviourState(bool state)
    {
        if (gameplayBehavioursToDisableAtMenu == null)
            return;

        for (int i = 0; i < gameplayBehavioursToDisableAtMenu.Length; i++)
        {
            if (gameplayBehavioursToDisableAtMenu[i] != null)
                gameplayBehavioursToDisableAtMenu[i].enabled = state;
        }
    }

    private void SetObjectState(GameObject[] objects, bool state)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(state);
        }
    }

    private void ShowMainMenu()
    {
        menuModeActive = true;
        StopTypingSound();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (menuMusicSource != null)
        {
            menuMusicSource.Stop();
            menuMusicSource.loop = true;
            menuMusicSource.Play();
        }

        currentPageIndex = 0;

        if (!gameStarted)
            LockGameplayForMenu();

        SetCursorUnlocked();
    }

    public void OpenInstructions()
    {
        menuModeActive = true;
        SetCursorUnlocked();
        StopTypingSound();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(true);

        ShowInstructionPage(0);
    }

    public void ReturnToMainMenu()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        isTyping = false;
        StopTypingSound();
        ShowMainMenu();
    }

    private void ShowInstructionPage(int index)
    {
        StopTypingSound();

        if (instructionPages == null || instructionPages.Length == 0)
            return;

        currentPageIndex = Mathf.Clamp(index, 0, instructionPages.Length - 1);

        InstructionPage page = instructionPages[currentPageIndex];

        if (instructionHeadingText != null)
            instructionHeadingText.text = page.heading;

        if (pageIndicatorText != null)
            pageIndicatorText.text = (currentPageIndex + 1) + " / " + instructionPages.Length;

        if (previewImage != null)
        {
            if (page.previewSprite != null)
            {
                previewImage.sprite = page.previewSprite;
                previewImage.gameObject.SetActive(true);

                if (previewFrame != null)
                    previewFrame.SetActive(true);
            }
            else
            {
                previewImage.gameObject.SetActive(false);

                if (previewFrame != null)
                    previewFrame.SetActive(false);
            }
        }

        StartTyping(page.body);
        UpdateButtons();
    }

    private void StartTyping(string textToType)
    {
        fullCurrentText = textToType;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeTextRoutine(fullCurrentText));
    }

    private IEnumerator TypeTextRoutine(string targetText)
    {
        isTyping = true;

        if (instructionBodyText != null)
            instructionBodyText.text = "";

        UpdateButtons();
        StartTypingSound();

        for (int i = 0; i < targetText.Length; i++)
        {
            if (instructionBodyText != null)
                instructionBodyText.text += targetText[i];

            float delay = IsPunctuation(targetText[i]) ? punctuationDelay : characterDelay;
            yield return new WaitForSecondsRealtime(delay);
        }

        isTyping = false;
        StopTypingSound();
        UpdateButtons();
    }

    private bool IsPunctuation(char c)
    {
        return c == '.' || c == ',' || c == ':' || c == ';' || c == '!' || c == '?';
    }

    public void SkipTyping()
    {
        if (!isTyping)
            return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        if (instructionBodyText != null)
            instructionBodyText.text = fullCurrentText;

        isTyping = false;
        StopTypingSound();
        UpdateButtons();
    }

    public void NextInstruction()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (currentPageIndex < instructionPages.Length - 1)
            ShowInstructionPage(currentPageIndex + 1);
    }

    public void PreviousInstruction()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (currentPageIndex > 0)
            ShowInstructionPage(currentPageIndex - 1);
    }

    private void UpdateButtons()
    {
        bool isLastPage = currentPageIndex >= instructionPages.Length - 1;

        if (previousButton != null)
            previousButton.gameObject.SetActive(currentPageIndex > 0);

        if (nextButton != null)
            nextButton.gameObject.SetActive(!isLastPage);

        if (skipTypingButton != null)
            skipTypingButton.gameObject.SetActive(false);

        if (startMissionButton != null)
            startMissionButton.gameObject.SetActive(isLastPage && !isTyping);
    }

    public void StartGame()
    {
        if (gameStarted)
            return;

        if (isTyping)
            SkipTyping();

        StopTypingSound();
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        gameStarted = true;

        if (menuMusicSource != null && menuMusicSource.isPlaying)
            yield return StartCoroutine(FadeOutAudio(menuMusicSource, menuMusicFadeDuration));

        StopMenuMusicImmediately();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        UnlockGameplayForGame();
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }

    private void StartTypingSound()
    {
        if (typingAudioSource == null || typingClip == null)
            return;

        typingAudioSource.Stop();
        typingAudioSource.clip = typingClip;
        typingAudioSource.loop = true;
        typingAudioSource.Play();
    }

    private void StopTypingSound()
    {
        if (typingAudioSource == null)
            return;

        typingAudioSource.Stop();
        typingAudioSource.loop = false;
    }

    private void StopMenuMusicImmediately()
    {
        if (menuMusicSource == null)
            return;

        menuMusicSource.Stop();
    }

    public void ExitGame()
    {
        StopTypingSound();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetCursorUnlocked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetCursorLocked()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
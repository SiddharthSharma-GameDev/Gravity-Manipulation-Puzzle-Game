using UnityEngine;
using UnityEngine.UI;

public class UIButtonClickSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float clickVolume = 0.8f;

    [Header("Auto Setup")]
    [SerializeField] private bool autoRegisterButtonsOnStart = true;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (autoRegisterButtonsOnStart)
            RegisterAllButtons();
    }

    public void RegisterAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            button.onClick.RemoveListener(PlayClickSound);
            button.onClick.AddListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        if (audioSource == null || clickSound == null)
            return;

        audioSource.PlayOneShot(clickSound, clickVolume);
    }
}
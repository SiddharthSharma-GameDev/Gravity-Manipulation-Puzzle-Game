using System.Collections;
using UnityEngine;

public class CollectibleCube : MonoBehaviour
{
    [Header("Idle Animation")]
    [SerializeField] private float idleRotationSpeed = 90f;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Collect Animation")]
    [SerializeField] private float vanishDuration = 1f;
    [SerializeField] private float vanishSpinSpeed = 540f;
    [SerializeField] private float popScaleMultiplier = 1.35f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float collectSoundVolume = 0.8f;

    private bool collected;
    private Collider cubeCollider;
    private Renderer[] renderers;
    private Material[] runtimeMaterials;
    private Vector3 startPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        cubeCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();

        startPosition = transform.position;
        originalScale = transform.localScale;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        CreateRuntimeMaterials();
    }

    private void Update()
    {
        if (collected)
            return;

        transform.Rotate(Vector3.up * idleRotationSpeed * Time.deltaTime, Space.World);

        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = startPosition + Vector3.up * floatOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        collected = true;

        if (cubeCollider != null)
            cubeCollider.enabled = false;

        if (GameManager.Instance != null)
            GameManager.Instance.CollectCube(vanishDuration);

        if (audioSource != null && collectSound != null)
            audioSource.PlayOneShot(collectSound, collectSoundVolume);

        StartCoroutine(VanishRoutine());
    }

    private IEnumerator VanishRoutine()
    {
        float timer = 0f;

        while (timer < vanishDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / vanishDuration);

            float scaleValue;

            if (t < 0.25f)
                scaleValue = Mathf.Lerp(1f, popScaleMultiplier, t / 0.25f);
            else
                scaleValue = Mathf.Lerp(popScaleMultiplier, 0f, (t - 0.25f) / 0.75f);

            transform.localScale = originalScale * scaleValue;
            transform.Rotate(Vector3.up * vanishSpinSpeed * Time.deltaTime, Space.World);

            SetMaterialAlpha(1f - t);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void CreateRuntimeMaterials()
    {
        if (renderers == null || renderers.Length == 0)
            return;

        int totalMaterials = 0;

        for (int i = 0; i < renderers.Length; i++)
            totalMaterials += renderers[i].materials.Length;

        runtimeMaterials = new Material[totalMaterials];

        int index = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            for (int j = 0; j < materials.Length; j++)
            {
                Material materialInstance = materials[j];
                SetupMaterialForFade(materialInstance);
                runtimeMaterials[index] = materialInstance;
                index++;
            }
        }
    }

    private void SetupMaterialForFade(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
    }

    private void SetMaterialAlpha(float alpha)
    {
        if (runtimeMaterials == null)
            return;

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            if (runtimeMaterials[i] == null)
                continue;

            Color color = runtimeMaterials[i].color;
            color.a = alpha;
            runtimeMaterials[i].color = color;
        }
    }
}
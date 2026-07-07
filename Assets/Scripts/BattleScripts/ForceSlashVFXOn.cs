using System.Collections;
using UnityEngine;

public class ForceSlashVFXOn : MonoBehaviour
{
    [SerializeField] private string slashVfxObjectName = "SlashVFX";
    [SerializeField] private float showTime = 0.12f;

    private Transform slashVFX;
    private Coroutine playCoroutine;

    private void Awake()
    {
        CacheSlashVFX();
        HideImmediate();
    }

    private void Start()
    {
        CacheSlashVFX();
        HideImmediate();
    }

    public void PlayOnce()
    {
        CacheSlashVFX();

        if (slashVFX == null)
        {
            Debug.LogWarning($"{gameObject.name} 안에서 {slashVfxObjectName} 오브젝트를 찾지 못했습니다.");
            return;
        }

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }

        playCoroutine = StartCoroutine(PlayRoutine());
    }

    public void HideImmediate()
    {
        CacheSlashVFX();

        if (slashVFX != null)
        {
            slashVFX.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayRoutine()
    {
        slashVFX.gameObject.SetActive(false);
        yield return null;

        slashVFX.gameObject.SetActive(true);
        yield return new WaitForSeconds(showTime);

        slashVFX.gameObject.SetActive(false);
        playCoroutine = null;
    }

    private void CacheSlashVFX()
    {
        if (slashVFX != null)
        {
            return;
        }

        slashVFX = FindChildDeep(transform, slashVfxObjectName);
    }

    private Transform FindChildDeep(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform result = FindChildDeep(child, targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

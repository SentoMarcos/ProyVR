using UnityEngine;

public class FadeInout : MonoBehaviour
{
    public float fadeDuration = 1.5f;
    public bool loop = true;

    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (loop)
            StartCoroutine(FadeLoop());
        else
            StartCoroutine(FadeInOutOnce());
    }

    private System.Collections.IEnumerator FadeLoop()
    {
        while (true)
        {
            // Fade in
            yield return StartCoroutine(Fade(0f, 1f));
            // Fade out
            yield return StartCoroutine(Fade(1f, 0f));
        }
    }

    private System.Collections.IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private System.Collections.IEnumerator FadeInOutOnce()
    {
        yield return StartCoroutine(Fade(0f, 1f));
        yield return StartCoroutine(Fade(1f, 0f));
    }
}

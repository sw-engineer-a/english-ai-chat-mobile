using System.Collections;
using UnityEngine;

public class UiMotion : MonoBehaviour
{
    public float bobAmount = 12f;
    public float bobSpeed = 1.6f;
    public bool bob;

    Vector3 baseScale = Vector3.one;
    Vector3 baseLocal;
    Coroutine pulseRoutine;
    Coroutine fadeRoutine;
    CanvasGroup group;
    bool baseReady;

    void Awake()
    {
        baseLocal = transform.localPosition;
        baseScale = transform.localScale;
        baseReady = true;
        group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        baseLocal = transform.localPosition;
        baseScale = Vector3.one;
        baseReady = true;
    }

    void Update()
    {
        if (!bob) return;
        if (!baseReady)
        {
            baseLocal = transform.localPosition;
            baseReady = true;
        }
        float y = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = baseLocal + new Vector3(0f, y, 0f);
    }

    public void SetBob(bool enabled)
    {
        bob = enabled;
        if (!enabled && baseReady) transform.localPosition = baseLocal;
    }

    public void Pulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    public void FadeIn(float seconds)
    {
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f, 1f, seconds));
    }

    IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3.2f;
            float s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.07f;
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = baseScale;
        pulseRoutine = null;
    }

    IEnumerator FadeRoutine(float from, float to, float seconds)
    {
        if (group == null) yield break;
        group.alpha = from;
        float t = 0f;
        seconds = Mathf.Max(0.05f, seconds);
        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }
        group.alpha = to;
        fadeRoutine = null;
    }
}

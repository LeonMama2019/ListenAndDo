using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIBlink : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float cyclesPerSecond = 2f;
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.25f;

    private Graphic target;
    private Coroutine blinking;
    private float originalAlpha;

    private void Awake()
    {
        target = GetComponent<Graphic>();
        originalAlpha = target.color.a;
    }

    public void StartBlinking()
    {
        if (blinking == null && isActiveAndEnabled)
            blinking = StartCoroutine(Blink());
    }

    public void StopBlinking()
    {
        if (blinking != null)
        {
            StopCoroutine(blinking);
            blinking = null;
        }
        RestoreAlpha();
    }

    private IEnumerator Blink()
    {
        while (true)
        {
            Color color = target.color;
            color.a = Mathf.Lerp(minimumAlpha * originalAlpha, originalAlpha,
                (Mathf.Sin(Time.unscaledTime * cyclesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f);
            target.color = color;
            yield return null;
        }
    }

    private void OnDisable()
    {
        blinking = null;
        RestoreAlpha();
    }

    private void RestoreAlpha()
    {
        if (target == null) return;
        Color color = target.color;
        color.a = originalAlpha;
        target.color = color;
    }
}

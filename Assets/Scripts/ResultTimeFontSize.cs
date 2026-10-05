using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ResultTimeFontSize : MonoBehaviour
{
    [SerializeField, Min(0f)] private float threeDigitReduction = 5f;
    private TMP_Text label;
    private float originalSize;
    private string previousText;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        originalSize = label.fontSize;
    }
    private void OnEnable() => previousText = null;
    private void LateUpdate()
    {
        if (label.text == previousText) return;
        previousText = label.text;
        bool threeDigits = float.TryParse(label.text, NumberStyles.Float,
            CultureInfo.InvariantCulture, out float seconds) && seconds >= 100f;
        label.fontSize = threeDigits ? Mathf.Max(1f, originalSize - threeDigitReduction) : originalSize;
    }
}

using TMPro;
using UnityEngine;

/// <summary>
/// Shows the currently selected player's name in a scene Nameplate.
/// </summary>
public sealed class PlayerNameplateDisplay : MonoBehaviour
{
    private TMP_Text nameText;

    private void Awake()
    {
        nameText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Start()
    {
        if (nameText == null)
        {
            Debug.LogWarning("Nameplate has no TextMeshPro text: " + gameObject.name, this);
            return;
        }

        nameText.richText = false;
        nameText.text = PlayerProfileStore.CurrentName;
    }
}

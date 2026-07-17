using UnityEngine;

public enum SelectorPattern
{
    BeforeObject,
    WithQuantity,   
    WithReference
}

[CreateAssetMenu(
    fileName = "Selector_",
    menuName = "ListenAndDo/Selector"
)]
public class SelectorData : ScriptableObject
{
    [Header("‰pŒê")]
    public string english;

    [Header("‚Ð‚ç‚ª‚È")]
    public string hiragana;

    [Header("Š¿Žš")]
    public string kanji;

    [Header("•¶‚Ì‘g‚Ý—§‚Ä•û")]
    public SelectorPattern pattern;
}
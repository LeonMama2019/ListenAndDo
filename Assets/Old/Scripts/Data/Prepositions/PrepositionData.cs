using UnityEngine;

[CreateAssetMenu(fileName = "Preposition_", menuName = "ListenAndDo/Preposition")]
public class PrepositionData : ScriptableObject
{
    [Header("‰pŒê")]
    public string english;

    [Header("‚Ð‚ç‚ª‚È")]
    public string hiragana;

    [Header("Š¿Žš")]
    public string kanji;
}
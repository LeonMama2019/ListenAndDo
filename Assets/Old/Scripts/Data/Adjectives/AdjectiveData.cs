using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Adjective_", menuName = "ListenAndDo/Adjective")]
public class AdjectiveData : ScriptableObject
{
    [Header("‰pŒê")]
    public string english;

    [Header("‚Ð‚ç‚ª‚È")]
    public string hiragana;

    [Header("Š¿Žš")]
    public string kanji;
}
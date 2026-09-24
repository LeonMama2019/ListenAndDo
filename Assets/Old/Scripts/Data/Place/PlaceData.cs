using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlaceData_", menuName = "ListenAndDo/Place")]
public class PlaceData : ScriptableObject
{
    [Header("‰pŒê")]
    public string english;

    [Header("‚Ð‚ç‚ª‚È")]
    public string hiragana;

    [Header("Š¿Žš")]
    public string kanji;
}
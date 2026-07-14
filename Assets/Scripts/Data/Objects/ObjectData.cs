using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Object", menuName = "ListenAndDo/Object")]
public class ObjectData : ScriptableObject
{
    [Header("‰pŒê")]
    public string english;

    [Header("‚Ð‚ç‚ª‚È")]
    public string hiragana;

    [Header("Š¿Žš")]
    public string kanji;


    [Header("‰æ‘œ")]
    public Sprite image;

    public List<VerbData> availableVerbs;
}
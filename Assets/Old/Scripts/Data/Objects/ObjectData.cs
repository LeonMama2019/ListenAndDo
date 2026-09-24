using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Object_", menuName = "ListenAndDo/Object")]
public class ObjectData : ScriptableObject
{
    [Header("‰pŒê")]
    public string english;

    [Header("‚Ð‚ç‚ª‚È")]
    public string hiragana;

    [Header("Š¿Žš")]
    public string kanji;
    
    public List<VerbData> availableVerbs;
}
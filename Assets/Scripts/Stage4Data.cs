using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stage4PersonEntry
{
    [Tooltip("記録に使う固定ID。例: girl1 / girl2")]
    [SerializeField] private string personId;
    [SerializeField] private string displayName;
    [Tooltip("正面の画像")]
    [SerializeField] private Sprite frontSprite;
    [Tooltip("後ろ姿の画像")]
    [SerializeField] private Sprite backSprite;
    [Tooltip("人物名の音声。例: Aちゃん")]
    [SerializeField] private AudioClip nameVoice;

    public string PersonId => personId;
    public string DisplayName => displayName;
    public Sprite FrontSprite => frontSprite;
    public Sprite BackSprite => backSprite;
    public AudioClip NameVoice => nameVoice;
    public bool IsReady => !string.IsNullOrWhiteSpace(personId)
                           && frontSprite != null
                           && backSprite != null
                           && nameVoice != null;
}

[Serializable]
public class Stage4ObjectEntry
{
    [Tooltip("記録に使う固定ID。例: red_book / apple")]
    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [Tooltip("持ち物名の音声。例: あかいほん")]
    [SerializeField] private AudioClip nameVoice;

    public string ObjectId => objectId;
    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public AudioClip NameVoice => nameVoice;
    public bool IsReady => !string.IsNullOrWhiteSpace(objectId)
                           && sprite != null
                           && nameVoice != null;
}

[CreateAssetMenu(fileName = "Stage4Data", menuName = "ListenAndDo/Stage4/Stage4 Data")]
public class Stage4Data : ScriptableObject
{
    [Header("人物")]
    [SerializeField] private List<Stage4PersonEntry> persons = new();

    [Header("持ち物")]
    [SerializeField] private List<Stage4ObjectEntry> objects = new();

    public IReadOnlyList<Stage4PersonEntry> Persons => persons;
    public IReadOnlyList<Stage4ObjectEntry> Objects => objects;
}

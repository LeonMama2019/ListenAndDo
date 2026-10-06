using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stage3ObjectEntry
{
    [Tooltip("記録に使う固定ID。例: red_cup / blue_box")]
    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [Tooltip("この画像に対応する音声。アイテム名、または「青い箱に」など。")]
    [SerializeField] private AudioClip voice;

    public string ObjectId => objectId;
    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public AudioClip Voice => voice;
    public bool IsReady => !string.IsNullOrWhiteSpace(objectId) && sprite != null && voice != null;
}

[CreateAssetMenu(fileName = "Stage3Objects", menuName = "ListenAndDo/Stage3/Object and Voice List")]
public class Stage3ObjectData : ScriptableObject
{
    [SerializeField] private List<Stage3ObjectEntry> items = new();
    [SerializeField] private List<Stage3ObjectEntry> boxes = new();
    [Tooltip("アイテム音声 → 箱音声 → この音声の順に再生する。")]
    [SerializeField] private AudioClip endVoice;

    public IReadOnlyList<Stage3ObjectEntry> Items => items;
    public IReadOnlyList<Stage3ObjectEntry> Boxes => boxes;
    public AudioClip EndVoice => endVoice;
}

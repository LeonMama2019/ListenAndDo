using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TouchObjectEntry
{
    [SerializeField] private string objectId;
    [SerializeField] private Sprite sprite;
    [SerializeField] private AudioClip touchInstruction;

    public string ObjectId => objectId;
    public Sprite Sprite => sprite;
    public AudioClip TouchInstruction => touchInstruction;
}

[CreateAssetMenu(fileName = "StageObjects", menuName = "ListenAndDo/ObjectID List")]
public class TouchObjectData : ScriptableObject
{
    [SerializeField] private List<TouchObjectEntry> entries = new();

    public IReadOnlyList<TouchObjectEntry> Entries => entries;

    // 表示枠へ異なる行を割り当てるための抽選。結果リストは呼び出し側が保持する。
    public void PickRandomEntries(int count, List<TouchObjectEntry> results)
    {
        results.Clear();
        var remaining = new List<TouchObjectEntry>();
        foreach (var entry in entries)
        {
            if (entry != null && entry.Sprite != null && entry.TouchInstruction != null)
                remaining.Add(entry);
        }

        int numberToPick = Mathf.Min(count, remaining.Count);
        for (int i = 0; i < numberToPick; i++)
        {
            int index = UnityEngine.Random.Range(0, remaining.Count);
            results.Add(remaining[index]);
            remaining.RemoveAt(index);
        }
    }
}

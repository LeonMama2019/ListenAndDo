using System;
using System.Collections.Generic;

[Serializable]
public class Stage3InstructionGroup
{
    public int itemSlot;
    public List<string> boxIds = new();
}

// Independent of UI positions: narration and answer order are separate.
public static class Stage3QuestionPlan
{
    private static readonly int[][] Counts =
    {
        new[] {1}, new[] {1, 1}, new[] {1, 1, 1}, new[] {2, 1},
        new[] {2, 2, 1}, new[] {1, 1, 1, 1}, new[] {1, 2, 1, 2}, new[] {2, 2, 1, 1, 2}
    };

    public static void Shuffle<T>(List<T> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T value = values[i]; values[i] = values[j]; values[j] = value;
        }
    }

    public static List<Stage3InstructionGroup> Create(int level, IList<string> boxIds, Random random)
    {
        if (level < 1 || level > 8) throw new ArgumentOutOfRangeException(nameof(level));
        if (boxIds == null || boxIds.Count != 3 || new HashSet<string>(boxIds).Count != 3)
            throw new ArgumentException("Stage3 requires three distinct box IDs.", nameof(boxIds));
        var slots = new List<int> {0, 1, 2, 3, 4};
        Shuffle(slots, random);
        var counts = new List<int>(Counts[level - 1]);
        if (level == 7) Shuffle(counts, random);
        var groups = new List<Stage3InstructionGroup>();
        for (int i = 0; i < counts.Count; i++)
        {
            var destinations = new List<string>(boxIds);
            Shuffle(destinations, random);
            groups.Add(new Stage3InstructionGroup
            {
                itemSlot = slots[i], boxIds = destinations.GetRange(0, counts[i])
            });
        }
        return groups;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class Stage3QuestionPlanTests
{
    [TestCase(1, 1, 1, "1")]
    [TestCase(2, 2, 2, "1,1")]
    [TestCase(3, 3, 3, "1,1,1")]
    [TestCase(4, 2, 3, "1,2")]
    [TestCase(5, 3, 5, "1,2,2")]
    [TestCase(6, 4, 4, "1,1,1,1")]
    [TestCase(7, 4, 6, "1,1,2,2")]
    [TestCase(8, 5, 8, "1,1,2,2,2")]
    public void EveryLevelMatchesTheAgreedCounts(int level, int itemCount, int placementCount, string counts)
    {
        var boxes = new[] { "red_box", "yellow_box", "blue_box" };
        for (int seed = 0; seed < 200; seed++)
        {
            var plan = Stage3QuestionPlan.Create(level, boxes, new Random(seed));
            Assert.That(plan.Count, Is.EqualTo(itemCount));
            Assert.That(plan.Sum(g => g.boxIds.Count), Is.EqualTo(placementCount));
            Assert.That(string.Join(",", plan.Select(g => g.boxIds.Count).OrderBy(n => n)), Is.EqualTo(counts));
            Assert.That(plan.Select(g => g.itemSlot).Distinct().Count(), Is.EqualTo(itemCount));
            foreach (var group in plan)
            {
                Assert.That(group.itemSlot, Is.InRange(0, 4));
                Assert.That(group.boxIds.Distinct().Count(), Is.EqualTo(group.boxIds.Count));
                Assert.That(group.boxIds.All(boxes.Contains), Is.True);
            }
        }
    }

    [Test]
    public void NarrationIsIndependentOfLeftToRightPositions()
    {
        var firstSlots = new HashSet<int>();
        var assignments = new HashSet<string>();
        for (int seed = 0; seed < 200; seed++)
        {
            var plan = Stage3QuestionPlan.Create(7, new[] {"red", "yellow", "blue"}, new Random(seed));
            firstSlots.Add(plan[0].itemSlot);
            assignments.Add(string.Join(",", plan.Select(g => g.boxIds.Count)));
        }
        Assert.That(firstSlots.Count, Is.EqualTo(5));
        Assert.That(assignments.Count, Is.GreaterThan(1));
    }

    [Test]
    public void InvalidLevelsAndDuplicateBoxesAreRejected()
    {
        var boxes = new[] {"red", "yellow", "blue"};
        Assert.Throws<ArgumentOutOfRangeException>(() => Stage3QuestionPlan.Create(0, boxes, new Random(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Stage3QuestionPlan.Create(9, boxes, new Random(1)));
        Assert.Throws<ArgumentException>(() => Stage3QuestionPlan.Create(4, new[] {"red", "red", "blue"}, new Random(1)));
    }
}

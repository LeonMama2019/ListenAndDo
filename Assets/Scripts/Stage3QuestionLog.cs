using System;
using System.Collections.Generic;

[Serializable]
public class Stage3VisibleItem
{
    public int slot;
    public string objectId;
}
[Serializable]
public class Stage3PlacementTarget
{
    public string objectId, boxId;
    public bool completed;
}
[Serializable]
public class Stage3QuestionEvent
{
    public string kind, objectId, boxId, detail;
    public float seconds;
}
[Serializable]
public class Stage3QuestionLog
{
    public List<Stage3VisibleItem> visibleItems = new();
    public List<Stage3InstructionGroup> instructions = new();
    public List<Stage3PlacementTarget> targets = new();
    public List<Stage3QuestionEvent> events = new();
}

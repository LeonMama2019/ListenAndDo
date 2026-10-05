using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public class Stage2DiceTarget
{
    public string dice;
    public int number;
    public string direction;
    public string initialOrientation;
    public string finalOrientation;
}
[Serializable] public class Stage2Event
{
    public string kind, dice, detail;
    public float seconds;
}
[Serializable] public class Stage2QuestionLog
{
    public bool slow, mouse;
    public List<Stage2DiceTarget> targets = new();
    public List<Stage2Event> events = new();
}

public class Stage2Quiz : MonoBehaviour
{
    [SerializeField] private AudioClip[] numbers = new AudioClip[6];
    [SerializeField] private AudioClip[] slowNumbers = new AudioClip[6];
    // Left, Right, Up, Down
    [SerializeField] private AudioClip[] directions = new AudioClip[4];
    [SerializeField] private AudioClip[] slowDirections = new AudioClip[4];
    [SerializeField] private AudioClip end, endSlow;
    [SerializeField] private int questionCount = 7;
    [SerializeField] private float nextQuestionDelay = 3f;
    private readonly string[] directionNames = { "Left", "Right", "Up", "Down" };
    private readonly List<DiceSwipe> dice = new();
    private readonly List<RectTransform> arrows = new();
    private readonly List<Vector2> arrowPositions = new();
    private AudioSource voice, checkAudio;
    private GameObject check, mouse;
    private StageLevelResultRecorder recorder;
    private Stage2QuestionLog log;
    private Coroutine instruction;
    private int level, question;
    private bool accepting, finished;
    private double started;
    private bool Slow => level == 5 || level == 6 || level == 8;
    private bool Mouse => level == 6 || level == 8;
    private bool Mixed => level == 4 || level == 6 || level == 7 || level == 8;

    private Transform Find(string name)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }
    private void Awake()
    {
        // Stage2 was based on Stage1: retain the old components but disable their behavior.
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var q in root.GetComponentsInChildren<Stage1Level1Quiz>(true)) q.enabled = false;
            foreach (var h in root.GetComponentsInChildren<HandSelector>(true)) h.enabled = false;
        }
        var tutorial = Find("Tutorial");
        if (tutorial != null) tutorial.gameObject.SetActive(false);
        var pen = Find("Pen");
        if (pen != null) pen.gameObject.SetActive(false);
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0;
        check = Find("Check")?.gameObject;
        checkAudio = check == null ? null : check.GetComponent<AudioSource>();
        mouse = Find("Mouse")?.gameObject;
    }
    private void Start()
    {
        level = StageLevelMenu.SelectedStage == 2 ? StageLevelMenu.SelectedLevel : 1;
        int count = level == 1 ? 1 : (level == 3 || level >= 7 ? 3 : 2);
        for (int i = 1; i <= 3; i++)
        {
            var t = Find("dice" + i);
            var a = Find("Arrow" + i) as RectTransform;
            if (t == null || a == null || t.GetComponent<DiceSwipe>() == null)
            { Debug.LogError("Stage2 requires dice1-3 and Arrow1-3.", this); enabled = false; return; }
            a.gameObject.SetActive(false);
            var graphic = a.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = false;
            bool active = count == 3 || (count == 1 ? i == 2 : i != 2);
            t.gameObject.SetActive(active);
            if (!active) continue;
            var d = t.GetComponent<DiceSwipe>();
            dice.Add(d); arrows.Add(a); arrowPositions.Add(a.anchoredPosition);
            d.Rolled += OnRoll;
            d.InputEnabled = false;
        }
        foreach (var clip in Slow ? slowNumbers : numbers)
            if (clip == null) { Debug.LogError("Stage2 number audio is missing.", this); return; }
        foreach (var clip in Slow ? slowDirections : directions)
            if (clip == null) { Debug.LogError("Stage2 direction audio is missing.", this); return; }
        if ((Slow ? endSlow : end) == null) { Debug.LogError("Stage2 end audio is missing.", this); return; }
        recorder = new StageLevelResultRecorder(2, level, questionCount);
        NextQuestion();
    }
    private void Event(string kind, string target = "", string detail = "")
    {
        log.events.Add(new Stage2Event { kind = kind, dice = target, detail = detail,
            seconds = (float)(Time.realtimeSinceStartupAsDouble - started) });
    }
    private void SetInput(bool value)
    {
        accepting = value;
        foreach (var d in dice) d.InputEnabled = value;
    }
    private void HideArrows()
    {
        for (int i = 0; i < arrows.Count; i++)
        { arrows[i].anchoredPosition = arrowPositions[i]; arrows[i].gameObject.SetActive(false); }
    }
    private void NextQuestion()
    {
        question++;
        if (check != null) check.SetActive(false);
        SetInput(false);
        log = new Stage2QuestionLog { slow = Slow, mouse = Mouse };
        started = Time.realtimeSinceStartupAsDouble;
        int common = UnityEngine.Random.Range(0, 4);
        var available = new List<int> {0, 1, 2, 3};
        for (int i = 0; i < dice.Count; i++)
        {
            dice[i].Shuffle();
            int direction = common;
            if (Mixed)
            { int pick = UnityEngine.Random.Range(0, available.Count); direction = available[pick]; available.RemoveAt(pick); }
            string name = directionNames[direction];
            int number;
            do { number = UnityEngine.Random.Range(1, 7); } while (number == dice[i].ValueAt(name));
            log.targets.Add(new Stage2DiceTarget { dice = dice[i].name, number = number,
                direction = name, initialOrientation = dice[i].Orientation });
        }
        recorder.BeginQuestion(question, "dice", "", started);
        recorder.SetDiceQuestion(log);
        if (mouse != null) mouse.SetActive(Mouse);
        if (Mouse) Event("mouseStart");
        instruction = StartCoroutine(Speak());
    }
    private IEnumerator Clip(AudioClip clip, string target)
    {
        Event("audio", target, clip.name);
        voice.clip = clip; voice.Play();
        while (voice.isPlaying) yield return null;
    }
    private IEnumerator Speak()
    {
        SetInput(false);
        HideArrows();
        for (int i = 0; i < dice.Count; i++)
        {
            HideArrows();
            var a = arrows[i];
            a.gameObject.SetActive(true);
            Event("arrow", dice[i].name);
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                a.anchoredPosition = Vector2.Lerp(arrowPositions[i] + Vector2.up * 80,
                    arrowPositions[i], Mathf.Clamp01(elapsed / 0.3f));
                yield return null;
            }
            var target = log.targets[i];
            yield return Clip((Slow ? slowNumbers : numbers)[target.number - 1], target.dice);
            yield return Clip((Slow ? slowDirections : directions)[Array.IndexOf(directionNames, target.direction)], target.dice);
        }
        yield return Clip(Slow ? endSlow : end, "");
        HideArrows();
        instruction = null;
        Event("instructionComplete");
        SetInput(true);
    }
    public void ReplayInstruction()
    {
        if (finished || log == null || instruction != null || !accepting) return;
        recorder.RecordReplay(); Event("replay");
        instruction = StartCoroutine(Speak());
    }
    private void OnRoll(DiceSwipe die, string direction)
    {
        if (!accepting || finished) return;
        Event("roll", die.name, direction + ":" + die.Orientation);
        bool correct = true;
        for (int i = 0; i < dice.Count; i++)
            correct &= dice[i].ValueAt(log.targets[i].direction) == log.targets[i].number;
        // Intermediate moves are logged; they are not counted as wrong submissions.
        if (!correct) return;
        SetInput(false);
        for (int i = 0; i < dice.Count; i++) log.targets[i].finalOrientation = dice[i].Orientation;
        Event("correct");
        if (mouse != null) mouse.SetActive(false);
        if (Mouse) Event("mouseStop");
        recorder.RecordCorrect(Time.realtimeSinceStartupAsDouble);
        if (check != null) check.SetActive(true);
        if (checkAudio != null) checkAudio.Play();
        StartCoroutine(Continue());
    }
    private IEnumerator Continue()
    {
        yield return new WaitForSecondsRealtime(nextQuestionDelay);
        if (question < questionCount) NextQuestion();
        else
        {
            finished = true;
            StageLevelMenu.MarkCompleted(2, level);
            StageResultScreenDisplay.OpenAfterGame(2, level);
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (voice != null) voice.Stop();
        HideArrows();
        SetInput(false);
        foreach (var d in dice) d.Rolled -= OnRoll;
    }
}

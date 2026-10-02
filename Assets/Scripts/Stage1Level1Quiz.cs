using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Level1Quiz : MonoBehaviour
{
    [SerializeField] private Stage1Tutorial tutorialPanel;
    [SerializeField] private TouchObjectData objectList;
    [SerializeField] private TouchObjectTarget object1;
    [SerializeField] private GameObject object2;
    [SerializeField] private GameObject object3;
    [SerializeField] private GameObject checkObject;
    [SerializeField] private AudioClip handLeftInstruction;
    [SerializeField] private AudioClip handRightInstruction;
    [SerializeField, Min(1)] private int questionCount = 7;
    [SerializeField, Min(0f)] private float nextQuestionDelay = 3f;

    [Header("Level3 妨害音")]
    [SerializeField] private AudioClip woodDropDosun;
    [SerializeField, Min(0.1f)] private float obstructionMinInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float obstructionMaxInterval = 3f;
    [SerializeField, Range(0f, 1f)] private float obstructionVolume = 0.7f;
    [Header("Level5 ランダム妨害音")]
    [SerializeField] private AudioClip[] level5ObstructionClips;
    [SerializeField] private AudioClip alarmClip;
    [SerializeField, Range(0f, 1f)] private float alarmVolumeScale = 0.4f;
    private AudioSource obstructionAudio;
    private Coroutine obstructionRoutine;

    [Header("Level4 鉛筆の動き")]
    [SerializeField] private Animator penAnimator;
    [SerializeField, Min(0.1f)] private float penMinDuration = 0.4f;
    [SerializeField, Min(0.1f)] private float penMaxDuration = 2.5f;
    [SerializeField, Min(0.1f)] private float penSlowSpeed = 0.6f;
    [SerializeField, Min(0.1f)] private float penFastSpeed = 2f;
    private Coroutine penRoutine;

    private readonly List<TouchObjectEntry> draw = new();
    private AudioSource audioSource;
    private AudioSource checkAudio;
    private Coroutine instructionRoutine;
    private int questionIndex;
    private int correctCount;
    private HandSelector.HandSide correctHand;
    private HandSelector.HandSide? selectedHand;
    private bool acceptingInput;
    private StageLevelResultRecorder resultRecorder;
    private int levelNumber;
    private int ObjectCount => levelNumber == 1 ? 1 : (levelNumber <= 4 ? 2 : 3);
    private TouchObjectTarget secondTarget;
    private TouchObjectTarget thirdTarget;
    private TouchObjectTarget correctTarget;
    private TouchObjectEntry correctEntry;

    public bool IsRunning { get; private set; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (checkObject != null) checkAudio = checkObject.GetComponent<AudioSource>();
        obstructionAudio = gameObject.AddComponent<AudioSource>();
        obstructionAudio.playOnAwake = false;
        obstructionAudio.loop = false;
        obstructionAudio.spatialBlend = 0f;
    }

    private void Start()
    {
        levelNumber = StageLevelMenu.SelectedStage == 1 ? StageLevelMenu.SelectedLevel : 1;
        if (levelNumber < 1 || levelNumber > 8) levelNumber = 1;
        secondTarget = object2 == null ? null : object2.GetComponent<TouchObjectTarget>();
        thirdTarget = object3 == null ? null : object3.GetComponent<TouchObjectTarget>();
        if (secondTarget != null) secondTarget.SetQuiz(this);
        if (thirdTarget != null) thirdTarget.SetQuiz(this);
        bool completed = levelNumber > 1 || PlayerPrefs.GetInt(Stage1Tutorial.CompletionKey, 0) == 1;
        if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(!completed);
        if (penAnimator != null)
        {
            penAnimator.speed = 0f;
            penAnimator.gameObject.SetActive(levelNumber == 4);
        }
        if (completed) BeginQuiz();
    }

    public bool BeginQuiz()
    {
        if (IsRunning) return false;
        if (objectList == null || object1 == null || checkObject == null || checkAudio == null ||
            handLeftInstruction == null || handRightInstruction == null)
        {
            Debug.LogError("Level1のデータ・Object1・Check(AudioSource)・左右の音声を設定してください。", this);
            return false;
        }

        objectList.PickRandomEntries(ObjectCount, draw);
        if (draw.Count < (ObjectCount))
        {
            Debug.LogError("Stage1のデータにSpriteと音声が設定された行がありません。", this);
            return false;
        }

        if (levelNumber >= 2 && (secondTarget == null || thirdTarget == null))
        {
            Debug.LogError("Level2 requires Object2 and Object3 TouchObjectTarget components.", this);
            return false;
        }

        if (levelNumber == 4 && penAnimator != null)
        {
            penAnimator.gameObject.SetActive(true);
            penAnimator.Play("Base Layer.Roll", 0, 0f);
            penAnimator.speed = 0f;
        }
        IsRunning = true;
        questionIndex = 0;
        correctCount = 0;
        resultRecorder = new StageLevelResultRecorder(1, levelNumber, questionCount);
        checkObject.SetActive(false);
        object1.gameObject.SetActive(ObjectCount != 2);
        if (object2 != null) object2.SetActive(levelNumber >= 2);
        if (object3 != null) object3.SetActive(levelNumber >= 2);
        HandSelector.ResetCursor();
        StartNextQuestion();
        return true;
    }

    private void StartNextQuestion()
    {
        StopPenMovement();
        StopObstruction();
        acceptingInput = false;
        selectedHand = null;
        HandSelector.ResetCursor();
        objectList.PickRandomEntries(ObjectCount, draw);
        if (draw.Count < (ObjectCount))
        {
            IsRunning = false;
            Debug.LogError("出題できるオブジェクトがありません。", this);
            return;
        }

        if (ObjectCount == 3)
        {
            object1.SetEntry(objectList, draw[0]);
            secondTarget.SetEntry(objectList, draw[1]);
            thirdTarget.SetEntry(objectList, draw[2]);
            int correctIndex = Random.Range(0, 3);
            correctTarget = correctIndex == 0 ? object1 : (correctIndex == 1 ? secondTarget : thirdTarget);
            correctEntry = draw[correctIndex];
        }
        else if (ObjectCount == 2)
        {
            secondTarget.SetEntry(objectList, draw[0]);
            thirdTarget.SetEntry(objectList, draw[1]);
            int correctIndex = Random.Range(0, 2);
            correctTarget = correctIndex == 0 ? secondTarget : thirdTarget;
            correctEntry = draw[correctIndex];
        }
        else
        {
            object1.SetEntry(objectList, draw[0]);
            correctTarget = object1;
            correctEntry = draw[0];
        }
        // チュートリアルの右手選択から続く最初の1問だけ右手を正解にする。
        correctHand = levelNumber == 1 && questionIndex == 0 ? HandSelector.HandSide.Right
            : (Random.Range(0, 2) == 0 ? HandSelector.HandSide.Left : HandSelector.HandSide.Right);
        resultRecorder.BeginQuestion(questionIndex + 1, correctEntry.ObjectId,
            correctHand.ToString(), Time.realtimeSinceStartupAsDouble);
        PlayCurrentInstruction();
        if ((levelNumber == 3 && woodDropDosun != null) ||
            (levelNumber == 5 && HasLevel5Clips()))
            obstructionRoutine = StartCoroutine(PlayObstruction());
        if (levelNumber == 4 && penAnimator != null)
            penRoutine = StartCoroutine(RandomPenMovement());
    }

    // Speakerボタンの On Click() から呼ぶ。
    public void ReplayInstruction()
    {
        if (!IsRunning || draw.Count == 0 || (checkObject != null && checkObject.activeSelf)) return;

        resultRecorder.RecordReplay();
        PlayCurrentInstruction();
    }

    private void PlayCurrentInstruction()
    {
        if (instructionRoutine != null) StopCoroutine(instructionRoutine);
        audioSource.Stop();
        acceptingInput = false;
        instructionRoutine = StartCoroutine(PlayInstruction(correctEntry.TouchInstruction));
    }

    private IEnumerator PlayInstruction(AudioClip objectInstruction)
    {
        AudioClip handInstruction = correctHand == HandSelector.HandSide.Right
            ? handRightInstruction : handLeftInstruction;
        audioSource.clip = handInstruction;
        audioSource.Play();
        yield return new WaitWhile(() => audioSource.isPlaying);
        audioSource.clip = objectInstruction;
        audioSource.Play();
        yield return new WaitWhile(() => audioSource.isPlaying);
        acceptingInput = true;
        instructionRoutine = null;
    }

    public bool SelectHand(HandSelector.HandSide hand)
    {
        if (IsRunning && acceptingInput)
        {
            selectedHand = hand;
            return true;
        }
        return false;
    }

    public void TouchObject(TouchObjectTarget target)
    {
        if (!IsRunning || !acceptingInput || !selectedHand.HasValue)
            return;

        if (target != correctTarget || selectedHand.Value != correctHand)
        {
            resultRecorder.RecordWrongAnswer();
            selectedHand = null;
            HandSelector.ResetCursor();
            Debug.Log($"Level{levelNumber} {questionIndex + 1}/{questionCount}: もう一度手を選んでね", this);
            return;
        }

        acceptingInput = false;
        StopObstruction();
        StopPenMovement();
        resultRecorder.RecordCorrect(Time.realtimeSinceStartupAsDouble);
        correctCount++;
        HandSelector.ResetCursor();
        checkObject.SetActive(true);
        checkAudio.Stop();
        checkAudio.Play();
        Debug.Log($"Level{levelNumber} {questionIndex + 1}/{questionCount}: 正解", this);
        StartCoroutine(ContinueAfterCorrect());
    }

    private IEnumerator PlayObstruction()
    {
        while (IsRunning)
        {
            float min = Mathf.Max(0.1f, obstructionMinInterval);
            float max = Mathf.Max(min, obstructionMaxInterval);
            yield return new WaitForSeconds(Random.Range(min, max));
            if (!IsRunning || (checkObject != null && checkObject.activeSelf)) break;
            AudioClip clip = levelNumber == 5 ? PickLevel5Clip() : woodDropDosun;
            if (clip != null)
            {
                float volume = obstructionVolume * (clip == alarmClip ? alarmVolumeScale : 1f);
                obstructionAudio.PlayOneShot(clip, volume);
            }
            // Let each thud finish before starting the next random wait.
            yield return new WaitWhile(() => obstructionAudio.isPlaying);
        }
        obstructionRoutine = null;
    }

    private bool HasLevel5Clips()
    {
        if (level5ObstructionClips == null) return false;
        foreach (AudioClip clip in level5ObstructionClips)
            if (clip != null) return true;
        return false;
    }

    private AudioClip PickLevel5Clip()
    {
        int available = 0;
        foreach (AudioClip clip in level5ObstructionClips)
            if (clip != null) available++;
        if (available == 0) return null;
        int selected = Random.Range(0, available);
        foreach (AudioClip clip in level5ObstructionClips)
            if (clip != null && selected-- == 0) return clip;
        return null;
    }

    private void StopObstruction()
    {
        if (obstructionRoutine != null) StopCoroutine(obstructionRoutine);
        obstructionRoutine = null;
        if (obstructionAudio != null) obstructionAudio.Stop();
    }

    private IEnumerator RandomPenMovement()
    {
        while (IsRunning)
        {
            // Independent draws allow consecutive stops or speed changes.
            int mode = Random.Range(0, 5);
            penAnimator.speed = mode < 2 ? 0f
                : mode == 2 ? Mathf.Max(0.1f, penSlowSpeed)
                : mode == 3 ? 1f : Mathf.Max(0.1f, penFastSpeed);
            float min = Mathf.Max(0.1f, penMinDuration);
            float max = Mathf.Max(min, penMaxDuration);
            yield return new WaitForSeconds(Random.Range(min, max));
        }
        penRoutine = null;
        if (penAnimator != null) penAnimator.speed = 0f;
    }

    private void StopPenMovement()
    {
        if (penRoutine != null) StopCoroutine(penRoutine);
        penRoutine = null;
        if (penAnimator != null) penAnimator.speed = 0f;
    }

    private void OnDisable()
    {
        StopObstruction();
        StopPenMovement();
    }

    private IEnumerator ContinueAfterCorrect()
    {
        yield return new WaitForSeconds(nextQuestionDelay);
        checkObject.SetActive(false);
        questionIndex++;
        if (questionIndex >= questionCount)
        {
            IsRunning = false;
            Debug.Log($"Level{levelNumber}終了: {correctCount}/{questionCount}問正解", this);
            StageLevelMenu.MarkCompleted(1, levelNumber);
            StageResultScreenDisplay.OpenAfterGame(1, levelNumber);
        }
        else
        {
            StartNextQuestion();
        }
    }
}

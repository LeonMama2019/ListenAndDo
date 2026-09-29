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

    private readonly List<TouchObjectEntry> draw = new();
    private AudioSource audioSource;
    private AudioSource checkAudio;
    private Coroutine instructionRoutine;
    private int questionIndex;
    private int correctCount;
    private HandSelector.HandSide correctHand;
    private HandSelector.HandSide? selectedHand;
    private bool acceptingInput;

    public bool IsRunning { get; private set; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (checkObject != null) checkAudio = checkObject.GetComponent<AudioSource>();
    }

    private void Start()
    {
        bool completed = PlayerPrefs.GetInt(Stage1Tutorial.CompletionKey, 0) == 1;
        if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(!completed);
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

        objectList.PickRandomEntries(1, draw);
        if (draw.Count == 0)
        {
            Debug.LogError("Stage1のデータにSpriteと音声が設定された行がありません。", this);
            return false;
        }

        IsRunning = true;
        questionIndex = 0;
        correctCount = 0;
        checkObject.SetActive(false);
        object1.gameObject.SetActive(true);
        if (object2 != null) object2.SetActive(false);
        if (object3 != null) object3.SetActive(false);
        HandSelector.ResetCursor();
        StartNextQuestion();
        return true;
    }

    private void StartNextQuestion()
    {
        acceptingInput = false;
        selectedHand = null;
        HandSelector.ResetCursor();
        objectList.PickRandomEntries(1, draw);
        if (draw.Count == 0)
        {
            IsRunning = false;
            Debug.LogError("出題できるオブジェクトがありません。", this);
            return;
        }

        object1.SetEntry(objectList, draw[0]);
        // チュートリアルの右手選択から続く最初の1問だけ右手を正解にする。
        correctHand = questionIndex == 0 ? HandSelector.HandSide.Right
            : (Random.Range(0, 2) == 0 ? HandSelector.HandSide.Left : HandSelector.HandSide.Right);
        ReplayInstruction();
    }

    // Speakerボタンの On Click() から呼ぶ。
    public void ReplayInstruction()
    {
        if (!IsRunning || draw.Count == 0 || (checkObject != null && checkObject.activeSelf)) return;

        if (instructionRoutine != null) StopCoroutine(instructionRoutine);
        audioSource.Stop();
        acceptingInput = false;
        instructionRoutine = StartCoroutine(PlayInstruction(draw[0].TouchInstruction));
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
        if (!IsRunning || !acceptingInput || !selectedHand.HasValue || target != object1)
            return;

        if (selectedHand.Value != correctHand)
        {
            selectedHand = null;
            HandSelector.ResetCursor();
            Debug.Log($"Level1 {questionIndex + 1}/{questionCount}: もう一度手を選んでね", this);
            return;
        }

        acceptingInput = false;
        correctCount++;
        HandSelector.ResetCursor();
        checkObject.SetActive(true);
        checkAudio.Stop();
        checkAudio.Play();
        Debug.Log($"Level1 {questionIndex + 1}/{questionCount}: 正解", this);
        StartCoroutine(ContinueAfterCorrect());
    }

    private IEnumerator ContinueAfterCorrect()
    {
        yield return new WaitForSeconds(nextQuestionDelay);
        checkObject.SetActive(false);
        questionIndex++;
        if (questionIndex >= questionCount)
        {
            IsRunning = false;
            Debug.Log($"Level1終了: {correctCount}/{questionCount}問正解", this);
        }
        else
        {
            StartNextQuestion();
        }
    }
}

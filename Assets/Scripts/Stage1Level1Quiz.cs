using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Level1Quiz : MonoBehaviour
{
    [SerializeField] private TouchObjectData objectList;
    [SerializeField] private TouchObjectTarget object1;
    [SerializeField] private GameObject object2;
    [SerializeField] private GameObject object3;
    [SerializeField] private AudioClip handLeftInstruction;
    [SerializeField] private AudioClip handRightInstruction;
    [SerializeField, Min(1)] private int questionCount = 7;

    private readonly List<TouchObjectEntry> draw = new();
    private AudioSource audioSource;
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
    }

    public bool BeginQuiz()
    {
        if (IsRunning) return false;
        if (objectList == null || object1 == null || handLeftInstruction == null || handRightInstruction == null)
        {
            Debug.LogError("Level1のデータ・Object1・左右の音声を設定してください。", this);
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
        StartCoroutine(PlayInstruction(draw[0].TouchInstruction));
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

        acceptingInput = false;
        bool correct = selectedHand.Value == correctHand;
        if (correct) correctCount++;
        Debug.Log($"Level1 {questionIndex + 1}/{questionCount}: {(correct ? "正解" : "不正解")}", this);

        questionIndex++;
        if (questionIndex >= questionCount)
        {
            IsRunning = false;
            HandSelector.ResetCursor();
            Debug.Log($"Level1終了: {correctCount}/{questionCount}問正解", this);
            return;
        }
        StartNextQuestion();
    }
}

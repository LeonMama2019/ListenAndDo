using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Stage1Tutorial : MonoBehaviour
{
    public const string CompletionKey = "Stage1.TutorialCompleted";
    [SerializeField] private GameObject step1, step2, step3, step4;
    [SerializeField] private GameObject okButton;
    [SerializeField] private UIBlink rightHandBlink;
    [SerializeField] private AudioClip tutorial1, tutorial2, tutorial3, tutorial4;
    [SerializeField] private Stage1Level1Quiz level1Quiz;
    [SerializeField] private TouchObjectData stage1Data;
    [SerializeField] private TouchObjectTarget object1;
    [SerializeField] private GameObject checkObject;
    [SerializeField] private Button speaker;
    [SerializeField] private AudioClip rightHandInstruction;
    [SerializeField, Min(0f)] private float objectHintDelay = 1.5f;
    [SerializeField, Min(0f)] private float correctDelay = 1.5f;
    private AudioSource audioSource;
    private UIBlink objectBlink, speakerBlink;
    private TouchObjectEntry entry;
    private int currentStep;
    private bool acceptingObject, replaying;
    public bool IsActive => isActiveAndEnabled && gameObject.activeInHierarchy;
    public bool WaitingForRightHand => IsActive && currentStep == 1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        SpeechSpeedSource.Bind(audioSource);
    }

    private void Start()
    {
        if (stage1Data != null)
            foreach (var candidate in stage1Data.Entries)
                if (candidate != null && candidate.ObjectId == "1") { entry = candidate; break; }
        if (entry == null || entry.Sprite == null || entry.TouchInstruction == null
            || object1 == null || rightHandInstruction == null || checkObject == null || speaker == null)
        {
            Debug.LogError("Stage1 Tutorial: Stage1のObjectId 1・Object1・右手音声・Check・Speakerを確認してください。", this);
            return;
        }
        object1.SetEntry(stage1Data, entry);
        object1.gameObject.SetActive(true);
        objectBlink = BlinkFor(object1.gameObject);
        speakerBlink = BlinkFor(speaker.gameObject);
        checkObject.SetActive(false);
        HandSelector.ResetCursor();
        // Explanation graphics must not swallow the hand/object/speaker clicks.
        foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        if (okButton != null)
        {
            var button = okButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(NextTutorialStep);
                if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
            }
        }
        ShowStep(1);
        if (rightHandBlink != null) rightHandBlink.StartBlinking();
        StartCoroutine(Say(tutorial1));
    }

    private static UIBlink BlinkFor(GameObject target)
    {
        var blink = target.GetComponent<UIBlink>();
        if (blink == null) blink = target.AddComponent<UIBlink>();
        return blink;
    }

    public bool OnRightHandSelected()
    {
        if (!WaitingForRightHand || entry == null) return false;
        StopAllCoroutines();
        audioSource.Stop();
        if (rightHandBlink != null) rightHandBlink.StopBlinking();
        ShowStep(2);
        StartCoroutine(ExplainObject());
        return true;
    }

    private IEnumerator Say(AudioClip clip)
    {
        if (clip == null) yield break;
        audioSource.clip = clip;
        audioSource.Play();
        while (audioSource.isPlaying) yield return null;
    }

    private IEnumerator Question()
    {
        yield return Say(rightHandInstruction);
        yield return Say(entry.TouchInstruction);
    }

    private IEnumerator ExplainObject()
    {
        yield return Question();
        objectBlink.StartBlinking();
        acceptingObject = true;
        yield return new WaitForSecondsRealtime(objectHintDelay);
        yield return Say(tutorial2);
    }

    public void TouchObject(TouchObjectTarget target)
    {
        if (!IsActive || target != object1 || currentStep != 2 || !acceptingObject) return;
        acceptingObject = false;
        StopAllCoroutines();
        audioSource.Stop();
        objectBlink.StopBlinking();
        HandSelector.ResetCursor();
        StartCoroutine(ShowCorrect());
    }

    private IEnumerator ShowCorrect()
    {
        ShowStep(3);
        checkObject.SetActive(true);
        var sound = checkObject.GetComponent<AudioSource>();
        if (sound != null && sound.clip != null) sound.PlayOneShot(sound.clip);
        float elapsed = 0;
        while (elapsed < correctDelay || (sound != null && sound.isPlaying))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return Say(tutorial3);
        checkObject.SetActive(false);
        ShowStep(4);
        speakerBlink.StartBlinking();
        yield return Say(tutorial4);
    }

    public void ReplayInstruction()
    {
        if (!IsActive || currentStep != 4 || replaying) return;
        StopAllCoroutines();
        audioSource.Stop();
        speakerBlink.StopBlinking();
        StartCoroutine(ReplayQuestion());
    }

    private IEnumerator ReplayQuestion()
    {
        replaying = true;
        yield return Question();
        replaying = false;
    }

    public void NextTutorialStep()
    {
        if (!IsActive || currentStep != 4) return;
        StopAllCoroutines();
        audioSource.Stop();
        StopBlinks();
        HandSelector.ResetCursor();
        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
        if (StageLevelMenu.ReturnFromTutorial(1)) return;
        gameObject.SetActive(false);
        if (level1Quiz != null) level1Quiz.BeginQuiz();
    }

    private void ShowStep(int step)
    {
        currentStep = step;
        if (step1 != null) step1.SetActive(step == 1);
        if (step2 != null) step2.SetActive(step == 2);
        if (step3 != null) step3.SetActive(step == 3);
        if (step4 != null) step4.SetActive(step == 4);
        if (okButton != null) okButton.SetActive(step == 4);
    }

    private void StopBlinks()
    {
        if (rightHandBlink != null) rightHandBlink.StopBlinking();
        if (objectBlink != null) objectBlink.StopBlinking();
        if (speakerBlink != null) speakerBlink.StopBlinking();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();
        StopBlinks();
    }
}

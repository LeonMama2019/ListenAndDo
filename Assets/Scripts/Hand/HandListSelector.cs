using UnityEngine;
using UnityEngine.UI;

public class HandListSelector : MonoBehaviour
{
    [Header("リスト内で手を表示するImage")]
    [SerializeField] private Image handImage;

    [Header("マウスについてくる手のImage")]
    [SerializeField] private Image handCursorImage;

    [Header("手カーソルのAnimator")]
    [SerializeField] private Animator handCursorAnimator;

    [Header("順番に表示する手の画像")]
    [SerializeField] private Sprite[] handSprites;

    [Header("手の音声を再生するAudioSource")]
    [SerializeField] private AudioSource handAudioSource;

    [Header("手ごとの音声")]
    [SerializeField] private AudioClip[] handVoiceClips;

    [Header("Stage01 Tutorial")]
    [SerializeField] private TutorialStage01 tutorialStage01;

    [Header("Stage02 Tutorial")]
    [SerializeField] private TutorialStage02 tutorialStage02;

    private int currentIndex = 0;
    private string currentHandAction = "";
    private bool handSelected = false;
    private bool cursorEnabled = false;
    private bool waitingForMouseMoveAfterReset = false;
    private Vector3 mousePositionAtReset;

    public bool IsHandSelected() => handSelected;

    private void Start()
    {
        if (handSprites == null || handSprites.Length == 0)
        {
            Debug.LogWarning("手の画像が登録されていません");
            return;
        }

        if (handCursorImage != null)
            handCursorImage.rectTransform.sizeDelta = new Vector2(50f, 50f);

        ShowCurrentHand();
        SetCursorEnabled(false);
        handSelected = false;
    }

    private void Update()
    {
        if (!cursorEnabled || handCursorImage == null) return;

        if (waitingForMouseMoveAfterReset)
        {
            // 答えた直後は手カーソルを中央に置いておき、
            // 実際のマウスが動いたら通常の追従へ戻す。
            if ((Input.mousePosition - mousePositionAtReset).sqrMagnitude < 1f)
                return;

            waitingForMouseMoveAfterReset = false;
        }

        handCursorImage.rectTransform.position = Input.mousePosition;
    }

    public void NextHand()
    {
        if (handSprites == null || handSprites.Length == 0) return;

        // 初回クリックでは現在表示中のHandを選択する。
        if (!handSelected)
        {
            handSelected = true;
            ShowCurrentHand();
            PlayCurrentAnimation();
            SetCursorEnabled(true);

            if (tutorialStage01 != null)
                tutorialStage01.OnClickHand();

            NotifyStage02Tutorial();
            return;
        }

        currentIndex++;
        if (currentIndex >= handSprites.Length)
            currentIndex = 0;

        ShowCurrentHand();
        PlayCurrentAnimation();
        SetCursorEnabled(true);
        NotifyStage02Tutorial();
    }

    private void NotifyStage02Tutorial()
    {
        if (tutorialStage02 != null)
            tutorialStage02.OnHandChanged(currentHandAction);
    }

    public string GetCurrentHandName()
    {
        if (!handSelected || handSprites == null || currentIndex < 0 || currentIndex >= handSprites.Length)
            return "";
        return handSprites[currentIndex].name;
    }

    private void ShowCurrentHand()
    {
        if (handSprites == null || currentIndex < 0 || currentIndex >= handSprites.Length) return;

        Sprite selectedSprite = handSprites[currentIndex];
        if (handImage != null)
        {
            handImage.sprite = selectedSprite;
            handImage.preserveAspect = true;
        }
        if (handCursorImage != null)
        {
            handCursorImage.sprite = selectedSprite;
            handCursorImage.preserveAspect = true;
        }
    }

    private void PlayCurrentAnimation()
    {
        if (handCursorAnimator != null)
        {
            handCursorAnimator.enabled = true;
            switch (currentIndex)
            {
                case 0:
                    currentHandAction = "touch";
                    handCursorAnimator.Play("Touch", 0, 0f);
                    break;
                case 1:
                    currentHandAction = "hit";
                    handCursorAnimator.Play("Hit", 0, 0f);
                    break;
                case 2:
                    currentHandAction = "pick";
                    handCursorAnimator.Play("Pick", 0, 0f);
                    break;
                case 3:
                    currentHandAction = "point";
                    handCursorAnimator.Play("Point", 0, 0f);
                    break;
                default:
                    currentHandAction = "";
                    break;
            }
        }

        PlayCurrentVoice();
    }

    public string GetCurrentHandAction()
    {
        if (!handSelected) return "";
        return currentHandAction;
    }

    private void PlayCurrentVoice()
    {
        if (handAudioSource == null)
        {
            Debug.LogWarning("Hand Audio Sourceが設定されていません");
            return;
        }
        if (handVoiceClips == null || currentIndex < 0 || currentIndex >= handVoiceClips.Length)
        {
            Debug.LogWarning($"currentIndex {currentIndex} に対応する音声がありません");
            return;
        }

        AudioClip clip = handVoiceClips[currentIndex];
        if (clip == null)
        {
            Debug.LogWarning($"Hand Voice ClipsのElement {currentIndex}が未設定です");
            return;
        }

        handAudioSource.Stop();
        handAudioSource.PlayOneShot(clip);
    }

    public Sprite GetSelectedHand()
    {
        if (!handSelected || handSprites == null || currentIndex < 0 || currentIndex >= handSprites.Length)
            return null;
        return handSprites[currentIndex];
    }

    public int GetSelectedIndex()
    {
        if (!handSelected) return -1;
        return currentIndex;
    }

    public void SetCursorEnabled(bool enabled)
    {
        cursorEnabled = enabled;
        if (!enabled) waitingForMouseMoveAfterReset = false;
        if (handCursorImage != null) handCursorImage.enabled = enabled;
        Cursor.visible = !enabled;
    }

    // Stage共通：回答後、次の問題に移る前に手カーソルを画面中央へ戻す。
    // OSカーソルは動かさず、ユーザーがマウスを動かした瞬間から再び追従する。
    public void ResetCursorToScreenCenter()
    {
        if (!cursorEnabled || handCursorImage == null) return;

        handCursorImage.rectTransform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        mousePositionAtReset = Input.mousePosition;
        waitingForMouseMoveAfterReset = true;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}

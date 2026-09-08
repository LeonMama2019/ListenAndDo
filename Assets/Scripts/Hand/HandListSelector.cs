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

    private int currentIndex = 0;
    private string currentHandAction = "";
    private bool handSelected = false;
    private bool cursorEnabled = false;

    public bool IsHandSelected()
    {
        return handSelected;
    }

    private void Start()
    {
        if (handSprites == null || handSprites.Length == 0)
        {
            Debug.LogWarning("手の画像が登録されていません");
            return;
        }

        if (handCursorImage != null)
        {
            handCursorImage.rectTransform.sizeDelta = new Vector2(50f, 50f);
        }

        ShowCurrentHand();
        SetCursorEnabled(false);
        handSelected = false;
    }

    private void Update()
    {
        if (!cursorEnabled)
            return;

        if (handCursorImage == null)
            return;

        handCursorImage.rectTransform.position = Input.mousePosition;
    }

    public void NextHand()
    {
        if (handSprites == null || handSprites.Length == 0)
            return;

        // 初回クリック = Handを実際に選択した瞬間
        if (!handSelected)
        {
            handSelected = true;

            ShowCurrentHand();
            PlayCurrentAnimation();
            SetCursorEnabled(true);

            // Hand選択が成立した瞬間にDarkPanelを消す
            if (tutorialStage01 != null)
                tutorialStage01.OnClickHand();
            else
                Debug.LogWarning("HandListSelector: TutorialStage01が設定されていません");

            return;
        }

        currentIndex++;

        if (currentIndex >= handSprites.Length)
            currentIndex = 0;

        ShowCurrentHand();
        PlayCurrentAnimation();
        SetCursorEnabled(true);
    }

    public string GetCurrentHandName()
    {
        if (!handSelected)
            return "";

        if (handSprites == null || currentIndex < 0 || currentIndex >= handSprites.Length)
            return "";

        return handSprites[currentIndex].name;
    }

    private void ShowCurrentHand()
    {
        if (handSprites == null || currentIndex < 0 || currentIndex >= handSprites.Length)
            return;

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
            }
        }

        PlayCurrentVoice();
    }

    public string GetCurrentHandAction()
    {
        if (!handSelected)
            return "";

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
        if (!handSelected)
            return null;

        if (handSprites == null || currentIndex < 0 || currentIndex >= handSprites.Length)
            return null;

        return handSprites[currentIndex];
    }

    public int GetSelectedIndex()
    {
        if (!handSelected)
            return -1;

        return currentIndex;
    }

    public void SetCursorEnabled(bool enabled)
    {
        cursorEnabled = enabled;

        if (handCursorImage != null)
            handCursorImage.enabled = enabled;

        Cursor.visible = !enabled;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}
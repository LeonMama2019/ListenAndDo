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

    [SerializeField] private TutorialManager tutorialManager;

    // HandListには最初から0番目を表示する
    private int currentIndex = 0;

    // まだ手カーソルは選択されていない
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
            handCursorImage.rectTransform.sizeDelta =
                new Vector2(50f, 50f);
        }

        // HandListには0番目の手を表示
        ShowCurrentHand();

        // ただし、手カーソルはまだ使わない
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

        /*
         * 初回クリックでは、
         * 今HandListに表示されている0番目を選択する。
         */
        if (!handSelected)
        {
            handSelected = true;

            ShowCurrentHand();
            PlayCurrentAnimation();
            SetCursorEnabled(true);

            return;
        }

        // 2回目以降は次の手へ進む
        currentIndex++;

        if (currentIndex >= handSprites.Length)
        {
            currentIndex = 0;

           
        }
        else if(currentIndex >= handSprites.Length - 1)
        {
            if (tutorialManager != null)
            {
                Debug.Log("HandListのアニメーションが完了しました");
                tutorialManager.OnHandListAnimationComplete("Stage01");
            }

        }

        ShowCurrentHand();
        PlayCurrentAnimation();
        SetCursorEnabled(true);
    }

    public string GetCurrentHandName()
    {
        if (!handSelected)
        {
            return "";
        }

        if (handSprites == null ||
            currentIndex < 0 ||
            currentIndex >= handSprites.Length)
        {
            return "";
        }

        return handSprites[currentIndex].name;
    }

    private void ShowCurrentHand()
    {
        if (handSprites == null ||
            currentIndex < 0 ||
            currentIndex >= handSprites.Length)
        {
            return;
        }

        Sprite selectedSprite = handSprites[currentIndex];

        // HandList側の画像
        if (handImage != null)
        {
            handImage.sprite = selectedSprite;
            handImage.preserveAspect = true;
        }

        // マウスについてくる手カーソル側の画像
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
                    currentHandAction = "Touch";
                    handCursorAnimator.Play(currentHandAction, 0, 0f);
                    break;

                case 1:
                    currentHandAction = "Hit";
                    handCursorAnimator.Play(currentHandAction, 0, 0f);
                    break;

                case 2:
                    currentHandAction = "Pick";
                    handCursorAnimator.Play(currentHandAction, 0, 0f);
                    break;

                case 3:
                    currentHandAction = "Point";
                    handCursorAnimator.Play(currentHandAction, 0, 0f);
                    break;

                default:
                    currentHandAction = "";
                    Debug.LogWarning(
                        $"currentIndex {currentIndex} に対応する動作がありません"
                    );
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

        if (handVoiceClips == null ||
            currentIndex < 0 ||
            currentIndex >= handVoiceClips.Length)
        {
            Debug.LogWarning(
                $"currentIndex {currentIndex} に対応する音声がありません"
            );

            return;
        }

        AudioClip clip = handVoiceClips[currentIndex];

        if (clip == null)
        {
            Debug.LogWarning(
                $"Hand Voice ClipsのElement {currentIndex}が未設定です"
            );

            return;
        }

        handAudioSource.Stop();
        handAudioSource.PlayOneShot(clip);
    }

    public Sprite GetSelectedHand()
    {
        if (!handSelected)
            return null;

        if (handSprites == null ||
            currentIndex < 0 ||
            currentIndex >= handSprites.Length)
        {
            return null;
        }

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
        {
            handCursorImage.enabled = enabled;
        }

        // 手カーソル中だけ通常マウスを隠す
        Cursor.visible = !enabled;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}
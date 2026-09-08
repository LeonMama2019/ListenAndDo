using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStage01 : MonoBehaviour
{
    [Header("回答ボタン")]
    [SerializeField] private Button object1Button;
    [SerializeField] private Button object2Button;

    [Header("HandList")]
    [SerializeField] private GameObject handList;
    [SerializeField] private Animator handListAnimator;

    [Header("Speaker")]
    [SerializeField] private Animator speakerAnimator;

    [Header("初回チュートリアル")]
    [SerializeField] private GameObject darkPanel;

    [Header("AnswerStage01")]
    [SerializeField] private AnswerStage01 stage01Answer;

    [Header("チュートリアル音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("HandListを促す音声")]
    [SerializeField] private AudioClip stage01VoiceClip;

    [Header("Speakerを促す音声")]
    [SerializeField] private AudioClip stage01SpeakerClip;

    private bool firstTutorialActive = false;
    private bool hintSpeakerActive = false;

    private void Start()
    {
        StartFirstTutorial();
    }

    /// <summary>
    /// Stage01に初めて入った時だけ行うチュートリアル。
    /// 6秒後に出る操作ヒントとは完全に別処理。
    /// </summary>
    private void StartFirstTutorial()
    {
        if (PlayerPrefs.GetInt("Stage01FirstTutorial", 0) == 1)
        {
            if (darkPanel != null)
                darkPanel.SetActive(false);

            SetAnswerInputEnabled(true);
            return;
        }

        firstTutorialActive = true;
        SetAnswerInputEnabled(false);

        if (darkPanel != null)
            darkPanel.SetActive(true);

        StartSpeakerAnimation();
    }

    /// <summary>
    /// 6秒操作しなかった時などに呼ばれる既存のHandヒント。
    /// 初回チュートリアルとは別。
    /// </summary>
    public void StartTutorial()
    {
        if (firstTutorialActive)
            return;

        if (object1Button != null)
            object1Button.interactable = false;

        if (object2Button != null)
            object2Button.interactable = false;

        PlayVoice(stage01VoiceClip);

        if (handListAnimator != null)
        {
            handListAnimator.enabled = true;
            handListAnimator.ResetTrigger("Start");
            handListAnimator.SetTrigger("Start");
        }
    }

    /// <summary>
    /// 6秒操作しなかった時などに呼ばれる既存のSpeakerヒント。
    /// </summary>
    public void SpeakerTutorial()
    {
        if (firstTutorialActive)
            return;

        hintSpeakerActive = true;
        StartSpeakerAnimation();
        PlayVoice(stage01SpeakerClip);
    }

    private void StartSpeakerAnimation()
    {
        if (speakerAnimator == null)
        {
            Debug.LogWarning("Speaker Animatorが設定されていません");
            return;
        }

        speakerAnimator.enabled = true;
        speakerAnimator.Rebind();
        speakerAnimator.Update(0f);
        speakerAnimator.Play("SpeakerClip", 0, 0f);
        speakerAnimator.Update(0f);
    }

    /// <summary>
    /// Speakerクリック時。
    /// 初回チュートリアルなら、その場でアニメーションを止めてDarkPanelを消す。
    /// 通常の6秒ヒントならSpeakerアニメーションだけ止める。
    /// </summary>
    public void OnClickButton()
    {
        if (firstTutorialActive)
        {
            firstTutorialActive = false;
            StopSpeakerAnimationImmediately();

            if (darkPanel != null)
                darkPanel.SetActive(false);

            SetAnswerInputEnabled(true);

            PlayerPrefs.SetInt("Stage01FirstTutorial", 1);
            PlayerPrefs.Save();
            return;
        }

        if (hintSpeakerActive)
        {
            hintSpeakerActive = false;
            StopSpeakerAnimationImmediately();
        }
    }

    private void StopSpeakerAnimationImmediately()
    {
        if (speakerAnimator == null)
            return;

        speakerAnimator.enabled = false;
        speakerAnimator.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// HandListが選択された時。既存の6秒ヒントを終了する。
    /// </summary>
    public void OnClickHand()
    {
        if (handListAnimator != null)
            handListAnimator.enabled = false;

        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;
    }

    private void SetAnswerInputEnabled(bool enabled)
    {
        if (object1Button != null)
            object1Button.interactable = enabled;

        if (object2Button != null)
            object2Button.interactable = enabled;

        if (stage01Answer != null)
            stage01Answer.enabled = enabled;
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
        {
            Debug.LogWarning("AudioSourceまたはチュートリアル音声が設定されていません");
            return;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }

    public void EndTutorial()
    {
        if (firstTutorialActive)
            return;

        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;

        if (handListAnimator != null)
            handListAnimator.enabled = false;
    }
}
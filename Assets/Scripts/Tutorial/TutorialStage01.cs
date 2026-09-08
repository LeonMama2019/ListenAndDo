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

    private void StartFirstTutorial()
    {
        // TEST中：初回済み判定を一時的に無効化。
        // 本番時はこのブロックのコメントを外す。
        /*
        if (PlayerPrefs.GetInt("Stage01FirstTutorial", 0) == 1)
        {
            if (darkPanel != null)
                darkPanel.SetActive(false);
            return;
        }
        */

        firstTutorialActive = true;

        if (darkPanel != null)
            darkPanel.SetActive(true);
    }

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

    public void OnClickButton()
    {
        if (!hintSpeakerActive)
            return;

        hintSpeakerActive = false;
        StopSpeakerAnimationImmediately();
    }

    private void StopSpeakerAnimationImmediately()
    {
        if (speakerAnimator == null)
            return;

        speakerAnimator.enabled = false;
        speakerAnimator.transform.localScale = Vector3.one;
    }

    public void OnClickHand()
    {
        if (firstTutorialActive)
        {
            firstTutorialActive = false;

            if (darkPanel != null)
                darkPanel.SetActive(false);

            // TEST中でも完了値は保存しておく。
            // 上の判定を戻せば、そのまま本番の「初回だけ」に戻る。
            PlayerPrefs.SetInt("Stage01FirstTutorial", 1);
            PlayerPrefs.Save();
            return;
        }

        if (handListAnimator != null)
            handListAnimator.enabled = false;

        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;
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
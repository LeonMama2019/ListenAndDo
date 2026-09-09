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

    [Header("Stage01 チュートリアル")]
    [SerializeField] private GameObject darkPanel;

    [Header("AnswerStage01")]
    [SerializeField] private AnswerStage01 stage01Answer;

    [Header("チュートリアル音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("HandListを促す音声（touchhand）")]
    [SerializeField] private AudioClip stage01VoiceClip;

    [Header("Speakerを促す音声")]
    [SerializeField] private AudioClip stage01SpeakerClip;

    private bool openingTutorialActive = false;
    private bool hintSpeakerActive = false;

    private void Start()
    {
        StartOpeningTutorial();
    }

    private void StartOpeningTutorial()
    {
        openingTutorialActive = true;

        if (darkPanel != null)
            darkPanel.SetActive(true);

        if (handListAnimator != null)
        {
            handListAnimator.enabled = true;
            handListAnimator.Play("HandListPulse", 0, 0f);
            handListAnimator.Update(0f);
        }

        // Stage01開始時に「Handを選択して」の音声を再生する。
        PlayVoice(stage01VoiceClip);
    }

    public void StartTutorial()
    {
        if (openingTutorialActive)
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
        if (openingTutorialActive)
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
        if (openingTutorialActive)
        {
            openingTutorialActive = false;

            if (darkPanel != null)
                darkPanel.SetActive(false);

            if (handListAnimator != null)
            {
                handListAnimator.enabled = false;
                handListAnimator.transform.localScale = Vector3.one;
            }

            // Handを選択した瞬間を起点に、3秒後に1問目を開始する。
            Stage01Manager manager = FindFirstObjectByType<Stage01Manager>();
            if (manager != null)
                manager.StartQuestionsAfterHandSelected();
            else
                Debug.LogWarning("TutorialStage01: Stage01Managerが見つかりません");

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
        if (openingTutorialActive)
            return;

        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;

        if (handListAnimator != null)
            handListAnimator.enabled = false;
    }
}
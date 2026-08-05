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

    [Header("AnswerStage01")]
    [SerializeField] private AnswerStage01 stage01Answer;

    [Header("チュートリアル音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("HandListを促す音声")]
    [SerializeField] private AudioClip stage01VoiceClip;

    [Header("Speakerを促す音声")]
    [SerializeField] private AudioClip stage01SpeakerClip;

    private bool onButton = false;
    private int tutorialCompleted = 0;

    private Coroutine speakerStopCoroutine;

    /// <summary>
    /// HandListを選ぶように促すチュートリアル
    /// </summary>
    public void StartTutorial()
    {
        if (object1Button != null)
            object1Button.interactable = false;

        if (object2Button != null)
            object2Button.interactable = false;

        Debug.Log("HandListチュートリアル開始");

        PlayVoice(stage01VoiceClip);

        if (handListAnimator != null)
        {
            handListAnimator.enabled = true;

            handListAnimator.ResetTrigger("Start");
            handListAnimator.SetTrigger("Start");
        }
    }

    /// <summary>
    /// Speakerを押すように促すチュートリアル
    /// </summary>
    public void SpeakerTutorial()
    {
        Debug.Log("Speakerチュートリアル開始");

        if (speakerAnimator != null)
        {
            speakerAnimator.enabled = true;

            speakerAnimator.ResetTrigger("Start");
            speakerAnimator.SetTrigger("Start");
        }

        PlayVoice(stage01SpeakerClip);
    }

    /// <summary>
    /// Speakerが押された時
    /// </summary>
    public void OnClickButton()
    {
        // 連打で何度も加算されるのを防ぐ
        if (onButton)
            return;

        onButton = true;
        tutorialCompleted++;

        // 現在のアニメーションを最後まで再生してから止める
        if (speakerStopCoroutine != null)
        {
            StopCoroutine(speakerStopCoroutine);
        }

        speakerStopCoroutine =
            StartCoroutine(StopSpeakerAfterCurrentLoop());

      

        if (tutorialCompleted >= 2)
        {
            OnTutorialComplete();
        }
    }

    /// <summary>
    /// Speakerアニメーションの現在の一周が
    /// 終わってからAnimatorを止める
    /// </summary>
    private IEnumerator StopSpeakerAfterCurrentLoop()
    {
        if (speakerAnimator == null)
            yield break;

        // Animatorの状態が更新されるまで待つ
        yield return null;

        AnimatorStateInfo stateInfo =
            speakerAnimator.GetCurrentAnimatorStateInfo(0);

        float finishTime =
            Mathf.Floor(stateInfo.normalizedTime) + 1f;

        while (speakerAnimator.enabled)
        {
            stateInfo =
                speakerAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= finishTime)
                break;

            yield return null;
        }

        speakerAnimator.enabled = false;
        speakerStopCoroutine = null;
    }

    /// <summary>
    /// HandListが押された時
    /// </summary>
    public void OnClickHand()
    {
        if (handListAnimator != null)
        {
            handListAnimator.enabled = false;
        }

        tutorialCompleted++;

        

        // HandList操作後、3秒待ってSpeakerを促す
        StartCoroutine(DelaySpeakerTutorial());
    }

    private IEnumerator DelaySpeakerTutorial()
    {
        yield return new WaitForSeconds(3f);

        SpeakerTutorial();
    }

    /// <summary>
    /// チュートリアル全体が完了した時
    /// </summary>
    private void OnTutorialComplete()
    {
        if (!onButton)
            return;

        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;

        PlayerPrefs.SetInt("Stage01", 1);
        PlayerPrefs.Save();

        Debug.Log("Stage01チュートリアル完了");
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
        {
            Debug.LogWarning(
                "AudioSourceまたはチュートリアル音声が設定されていません"
            );

            return;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }
}
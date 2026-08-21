using System.Collections;
using UnityEngine;

public class TutorialStage01 : MonoBehaviour
{
    [Header("HandList")]
    [SerializeField] private Animator handListAnimator;

    [Header("Speaker")]
    [SerializeField] private Animator speakerAnimator;

    [Header("チュートリアル音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("HandListを促す音声")]
    [SerializeField] private AudioClip stage01VoiceClip;

    [Header("Speakerを促す音声")]
    [SerializeField] private AudioClip stage01SpeakerClip;

    private Coroutine speakerStopCoroutine;

    /// <summary>
    /// Hand を選ぶように促す演出だけを担当する。
    /// 「いつ出すか」は AnswerStage01 が判断する。
    /// </summary>
    public void ShowHandHint()
    {
        PlayVoice(stage01VoiceClip);

        if (handListAnimator == null)
            return;

        handListAnimator.enabled = true;
        handListAnimator.ResetTrigger("Start");
        handListAnimator.SetTrigger("Start");
    }

    public void StopHandHint()
    {
        if (handListAnimator != null)
        {
            handListAnimator.enabled = false;
        }
    }

    /// <summary>
    /// 問題をもう一度聞けることを Speaker で促す。
    /// </summary>
    public void ShowSpeakerHint()
    {
        Debug.Log("Speakerヒント開始");

        if (speakerAnimator != null)
        {
            speakerAnimator.enabled = true;
            speakerAnimator.ResetTrigger("Start");
            speakerAnimator.SetTrigger("Start");
        }

        PlayVoice(stage01SpeakerClip);
    }

    public void StopSpeakerHintAfterCurrentLoop()
    {
        if (speakerAnimator == null || !speakerAnimator.enabled)
            return;

        if (speakerStopCoroutine != null)
        {
            StopCoroutine(speakerStopCoroutine);
        }

        speakerStopCoroutine = StartCoroutine(StopSpeakerAfterCurrentLoop());
    }

    private IEnumerator StopSpeakerAfterCurrentLoop()
    {
        yield return null;

        AnimatorStateInfo stateInfo = speakerAnimator.GetCurrentAnimatorStateInfo(0);
        float finishTime = Mathf.Floor(stateInfo.normalizedTime) + 1f;

        while (speakerAnimator.enabled)
        {
            stateInfo = speakerAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= finishTime)
                break;

            yield return null;
        }

        speakerAnimator.enabled = false;
        speakerStopCoroutine = null;
    }

    public void StopAllHints()
    {
        StopHandHint();

        if (speakerStopCoroutine != null)
        {
            StopCoroutine(speakerStopCoroutine);
            speakerStopCoroutine = null;
        }

        if (speakerAnimator != null)
        {
            speakerAnimator.enabled = false;
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
        }
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

    // 既存の Inspector / Animation Event を壊さないための互換メソッド。
    public void StartTutorial() => ShowHandHint();
    public void SpeakerTutorial() => ShowSpeakerHint();
    public void OnClickHand() => StopHandHint();
    public void OnClickButton() => StopSpeakerHintAfterCurrentLoop();
    public void EndTutorial() => StopHandHint();
}
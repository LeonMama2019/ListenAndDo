using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Stage4Quiz : MonoBehaviour
{
    private const int QuestionCount = 7;
    [SerializeField] private Stage4Data data;
    private Stage4LevelSetting setting;
    private readonly System.Random random = new();
    private readonly List<Stage4PersonEntry> people = new();
    private readonly List<Stage4ObjectEntry> objects = new();
    private readonly List<Stage4QuestionType> questionPlan = new();
    private readonly List<List<Stage4ObjectEntry>> held = new();

    private Transform[] personRoots = new Transform[3];
    private Image[] personImages = new Image[3];
    private Image[,] heldImages = new Image[3,2];
    private GameObject[] backs = new GameObject[3];
    private GameObject answerPanel, whoPanel, whatPanel, notPanel, checkPanel, checkMark;
    private Button speaker, yesButton, noButton;
    private Image[] selects = new Image[4];
    private TMP_Text questionLabel;
    private AudioSource voice, checkAudio;
    private StageLevelResultRecorder recorder;
    private Coroutine speaking;
    private int level, questionIndex;
    private bool accepting, finished;
    private Stage4QuestionType currentType;
    private Stage4PersonEntry targetPerson;
    private Stage4ObjectEntry targetObject;
    private bool expectedYes, checkPatternA;
    private double answerStarted;

    private void Start()
    {
        if (data == null) { Debug.LogError("Stage4: Stage4Quiz の Data に Stage4Data を設定してください。"); enabled = false; return; }
        level = StageLevelMenu.SelectedStage == 4 ? Mathf.Clamp(StageLevelMenu.SelectedLevel,1,8) : 1;
        if (data.Levels == null || data.Levels.Count < level) { Debug.LogError("Stage4: レベル設定が足りません。"); enabled=false; return; }
        setting = data.Levels[level-1];
        foreach (var p in data.Persons) if (p != null && p.IsReady) people.Add(p);
        foreach (var o in data.Objects) if (o != null && o.IsReady) objects.Add(o);
        if (people.Count < setting.PersonCount) { Debug.LogError($"Stage4 Lv{level}: 人物が{setting.PersonCount}人必要ですが、Stage4Dataには{people.Count}人しか登録されていません。"); enabled=false; return; }
        int maxHeld = setting.PersonCount * setting.MaxObjectsPerPerson;
        if (objects.Count < Mathf.Max(4,maxHeld)) { Debug.LogError("Stage4: 持ち物を4種類以上登録してください。"); enabled=false; return; }

        CacheScene();
        CenterSinglePerson();
        BuildQuestionPlan();
        recorder = new StageLevelResultRecorder(4, level, QuestionCount);
        voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake=false; voice.spatialBlend=0; SpeechSpeedSource.Bind(voice);
        if (checkMark != null) { checkAudio=checkMark.GetComponent<AudioSource>(); checkMark.SetActive(false); }
        BeginQuestion();
    }

    private Transform Find(string n)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == n) return t;
        return null;
    }

    private void CacheScene()
    {
        answerPanel=Find("AnswerPanel")?.gameObject; whoPanel=Find("WhoPanel")?.gameObject;
        whatPanel=Find("WhatPanel")?.gameObject; notPanel=Find("NotPanel")?.gameObject; checkPanel=Find("CheckPanel")?.gameObject;
        checkMark=Find("Check")?.gameObject;
        speaker=Find("Speaker")?.GetComponent<Button>();
        yesButton=Find("YesButton")?.GetComponent<Button>(); noButton=Find("NoButton")?.GetComponent<Button>();
        for(int i=0;i<3;i++)
        {
            personRoots[i]=Find("Person"+(i+1));
            if(personRoots[i]==null) continue;
            personImages[i]=personRoots[i].GetComponent<Image>();
            backs[i]=Find("Person"+(i+1)+"Back")?.gameObject;
            heldImages[i,0]=FindChildImage(personRoots[i],"Object1");
            heldImages[i,1]=FindChildImage(personRoots[i],"Object2");
            int slot=i; EnsureButton(personRoots[i].gameObject,()=>AnswerPerson(slot));
        }
        for(int i=0;i<4;i++)
        {
            var t=Find("Select"+(i+1)); if(t==null) continue;
            selects[i]=t.GetComponent<Image>(); int slot=i; EnsureButton(t.gameObject,()=>AnswerObject(slot));
        }
        if(yesButton!=null){yesButton.onClick.RemoveAllListeners();yesButton.onClick.AddListener(()=>AnswerYesNo(true));}
        if(noButton!=null){noButton.onClick.RemoveAllListeners();noButton.onClick.AddListener(()=>AnswerYesNo(false));}
        if(speaker!=null){speaker.onClick.RemoveAllListeners();speaker.onClick.AddListener(Replay);}
        questionLabel=FindQuestionText();
    }

    private void CenterSinglePerson()
    {
        if (setting.PersonCount != 1 || personRoots[0] == null) return;
        if (personRoots[0] is RectTransform rect)
        {
            rect.anchorMin = new Vector2(.5f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(.5f, rect.anchorMax.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }
    }

    private static Image FindChildImage(Transform root,string n)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(t.name==n) return t.GetComponent<Image>();
        return null;
    }
    private static void EnsureButton(GameObject go,UnityEngine.Events.UnityAction action)
    {
        var b=go.GetComponent<Button>(); if(b==null)b=go.AddComponent<Button>();
        b.transition=Selectable.Transition.None; b.onClick.RemoveAllListeners(); b.onClick.AddListener(action);
    }
    private TMP_Text FindQuestionText()
    {
        foreach(var panel in new[]{checkPanel,whoPanel,notPanel,answerPanel})
            if(panel!=null){var t=panel.GetComponentInChildren<TMP_Text>(true);if(t!=null)return t;}
        return null;
    }

    private void BuildQuestionPlan()
    {
        questionPlan.Clear();
        var allowed=new List<Stage4QuestionType>();
        if(setting.QuestionTypes!=null) foreach(var q in setting.QuestionTypes) if(!allowed.Contains(q)) allowed.Add(q);
        if(allowed.Count==0) allowed.Add(Stage4QuestionType.What);
        foreach(var q in allowed) if(questionPlan.Count<QuestionCount) questionPlan.Add(q);
        while(questionPlan.Count<QuestionCount) questionPlan.Add(allowed[random.Next(allowed.Count)]);
        Shuffle(questionPlan);
    }

    private void BeginQuestion()
    {
        accepting=false;
        if(checkMark!=null)checkMark.SetActive(false);
        SetPanels(false,false,false,false);
        held.Clear();
        var pPool=new List<Stage4PersonEntry>(people); Shuffle(pPool);
        var oPool=new List<Stage4ObjectEntry>(objects); Shuffle(oPool);
        int oi=0;
        for(int i=0;i<3;i++)
        {
            bool active=i<setting.PersonCount;
            if(personRoots[i]!=null) personRoots[i].gameObject.SetActive(active);
            if(!active) continue;
            var p=pPool[i]; personImages[i].enabled=true; personImages[i].sprite=p.FrontSprite; personImages[i].preserveAspect=true;
            if(backs[i]!=null)
            {
                backs[i].SetActive(false);
                var backImage=backs[i].GetComponent<Image>();
                if(backImage!=null){backImage.sprite=p.BackSprite;backImage.preserveAspect=true;}
            }
            int count=random.Next(setting.MinObjectsPerPerson,setting.MaxObjectsPerPerson+1);
            var list=new List<Stage4ObjectEntry>();
            for(int j=0;j<2;j++)
            {
                bool show=j<count && oi<oPool.Count;
                if(heldImages[i,j]!=null){heldImages[i,j].gameObject.SetActive(show);if(show){heldImages[i,j].sprite=oPool[oi].Sprite;heldImages[i,j].preserveAspect=true;}}
                if(show) list.Add(oPool[oi++]);
            }
            held.Add(list);
        }
        currentType=questionPlan[questionIndex];
        StartCoroutine(MemorizeThenAsk(pPool));
    }

    private IEnumerator MemorizeThenAsk(List<Stage4PersonEntry> pPool)
    {
        // 声かけ終了後にも、設定した秒数だけ持ち物を見る時間を確保する。
        if(data.MemorizePromptVoice!=null)
        {
            for(int i=0;i<setting.PersonCount;i++)
            {
                yield return Play(pPool[i].NameVoice);
                yield return Play(data.MemorizePromptVoice);
            }
        }
        yield return new WaitForSecondsRealtime(Mathf.Max(.1f,data.MemorizeSeconds));
        for(int i=0;i<setting.PersonCount;i++)
        {
            if(backs[i]!=null)
            {
                backs[i].SetActive(true);
                // 親オブジェクトは有効のまま、正面の画像だけ隠す。
                if(personImages[i]!=null) personImages[i].enabled=false;
            }
            for(int j=0;j<2;j++) if(heldImages[i,j]!=null) heldImages[i,j].gameObject.SetActive(false);
        }
        PrepareQuestion(pPool);
        answerStarted=Time.realtimeSinceStartupAsDouble;
        recorder.BeginQuestion(questionIndex+1,targetObject!=null?targetObject.ObjectId:"",targetPerson!=null?targetPerson.PersonId:"",answerStarted);
        speaking=StartCoroutine(SpeakQuestion());
    }

    private void PrepareQuestion(List<Stage4PersonEntry> pPool)
    {
        int pi=random.Next(setting.PersonCount);
        targetPerson=pPool[pi];
        var owned=held[pi];
        targetObject=owned[random.Next(owned.Count)];
        expectedYes=true;
        string text="";
        if(currentType==Stage4QuestionType.Who)
        {
            text=data.WhoQuestionText.Replace("{Object}",targetObject.DisplayName);
            SetPanels(true,false,false,false);
        }
        else if(currentType==Stage4QuestionType.What)
        {
            text=data.WhatQuestionText.Replace("{Person}",targetPerson.DisplayName);
            SetPanels(false,true,false,false); FillObjectChoices(targetObject,false,owned);
        }
        else if(currentType==Stage4QuestionType.Not)
        {
            var notOwned=PickNotOwned(owned); targetObject=notOwned;
            text=data.NotQuestionText.Replace("{Person}",targetPerson.DisplayName);
            SetPanels(false,true,false,false); FillObjectChoices(targetObject,true,owned);
        }
        else
        {
            expectedYes=random.Next(2)==0;
            if(!expectedYes) targetObject=PickNotOwned(owned);
            checkPatternA=random.Next(2)==0;
            text=(checkPatternA?data.CheckPatternAText:data.CheckPatternBText).Replace("{Person}",targetPerson.DisplayName).Replace("{Object}",targetObject.DisplayName);
            SetPanels(false,false,false,true);
        }
        SetQuestionText(text);
    }

    private Stage4ObjectEntry PickNotOwned(List<Stage4ObjectEntry> owned)
    {
        var pool=objects.FindAll(x=>!owned.Contains(x)); return pool[random.Next(pool.Count)];
    }

    private readonly List<Stage4ObjectEntry> choiceObjects=new();
    private void FillObjectChoices(Stage4ObjectEntry correct,bool isNot,List<Stage4ObjectEntry> owned)
    {
        choiceObjects.Clear(); choiceObjects.Add(correct);
        var pool=new List<Stage4ObjectEntry>(objects); pool.Remove(correct); Shuffle(pool);
        for(int i=0;i<pool.Count && choiceObjects.Count<4;i++)
        {
            if(!isNot) choiceObjects.Add(pool[i]);
            else if(owned.Contains(pool[i])) choiceObjects.Add(pool[i]);
        }
        // Not は「持っていなかった物」が正解1つだけになるよう、残りは実際に持っていた物で埋める。
        int repeat=0;
        while(choiceObjects.Count<4 && owned.Count>0) choiceObjects.Add(owned[repeat++ % owned.Count]);
        Shuffle(choiceObjects);
        for(int i=0;i<4;i++) if(selects[i]!=null){selects[i].sprite=choiceObjects[i].Sprite;selects[i].preserveAspect=true;}
    }

    private void SetPanels(bool who,bool what,bool not,bool check)
    {
        if(answerPanel!=null)answerPanel.SetActive(true);
        if(whoPanel!=null)whoPanel.SetActive(who);
        if(whatPanel!=null)whatPanel.SetActive(what);
        if(notPanel!=null)notPanel.SetActive(not);
        if(checkPanel!=null)checkPanel.SetActive(check);
    }
    private void SetQuestionText(string value)
    {
        if(questionLabel!=null) questionLabel.text=value;
        TMP_Text active=null;
        foreach(var p in new[]{whoPanel,notPanel,checkPanel}) if(p!=null&&p.activeSelf){active=p.GetComponentInChildren<TMP_Text>(true);if(active!=null)break;}
        if(active!=null)active.text=value;
    }

    private IEnumerator Play(AudioClip clip)
    {
        if(clip==null)yield break; voice.clip=clip;voice.Play();while(voice.isPlaying)yield return null;
    }
    private IEnumerator SpeakQuestion()
    {
        accepting=false;
        if(currentType==Stage4QuestionType.Who)
        {
            yield return Play(targetObject.NameVoice);
            yield return Play(data.WhoQuestionVoice);
        }
        else if(currentType==Stage4QuestionType.What || currentType==Stage4QuestionType.Not)
        {
            yield return Play(targetPerson.NameVoice);
            yield return Play(currentType==Stage4QuestionType.What ? data.WhatQuestionVoice : data.NotQuestionVoice);
        }
        else
        {
            yield return Play(targetPerson.NameVoice);
            if(checkPatternA){yield return Play(data.CheckHadItemWasVoice);yield return Play(targetObject.NameVoice);yield return Play(data.CheckIsItVoice);}
            else {yield return Play(data.CheckPersonWaVoice);yield return Play(targetObject.NameVoice);yield return Play(data.CheckHadItemQuestionVoice);}
        }
        speaking=null; accepting=true;
    }
    private void Replay()
    {
        if(!accepting||finished||speaking!=null)return;
        recorder.RecordReplay(); speaking=StartCoroutine(SpeakQuestion());
    }

    private void AnswerPerson(int slot)
    {
        if(!accepting||currentType!=Stage4QuestionType.Who||slot>=setting.PersonCount)return;
        var shown=personImages[slot].sprite;
        int correct=-1;
        for(int i=0;i<setting.PersonCount;i++) if(held[i].Contains(targetObject)){correct=i;break;}
        Resolve(slot==correct);
    }
    private void AnswerObject(int slot)
    {
        if(!accepting||(currentType!=Stage4QuestionType.What&&currentType!=Stage4QuestionType.Not)||slot>=choiceObjects.Count)return;
        Resolve(choiceObjects[slot]==targetObject);
    }
    private void AnswerYesNo(bool yes)
    {
        if(!accepting||currentType!=Stage4QuestionType.Check)return;
        Resolve(yes==expectedYes);
    }
    private void Resolve(bool correct)
    {
        if(!correct){recorder.RecordWrongAnswer();return;}
        accepting=false; recorder.RecordCorrect(Time.realtimeSinceStartupAsDouble);
        if(checkMark!=null){checkMark.SetActive(true);if(checkAudio!=null)checkAudio.Play();}
        StartCoroutine(Continue());
    }
    private IEnumerator Continue()
    {
        yield return new WaitForSecondsRealtime(3f);
        questionIndex++;
        if(questionIndex<QuestionCount)BeginQuestion();
        else{finished=true;StageLevelMenu.MarkCompleted(4,level);StageResultScreenDisplay.OpenAfterGame(4,level);}
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var r=new System.Random();
        for(int i=list.Count-1;i>0;i--){int j=r.Next(i+1);(list[i],list[j])=(list[j],list[i]);}
    }
}
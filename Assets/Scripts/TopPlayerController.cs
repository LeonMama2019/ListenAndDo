using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopPlayerController : MonoBehaviour
{
    [SerializeField] private GameObject playerPanel;
    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private TopSceneManager sceneManager;
    private PlayerProfileStore.SaveData data;
    private TMP_InputField nickname, age;
    private Button registerButton, newPlayerButton, selectButton;
    private readonly Transform[] slots = new Transform[2];

    private void Awake()
    {
        data = PlayerProfileStore.Load();
        nickname = registrationPanel.transform.Find("name").GetComponent<TMP_InputField>();
        age = registrationPanel.transform.Find("age").GetComponent<TMP_InputField>();
        nickname.richText = false;
        nickname.characterLimit = 10;
        age.contentType = TMP_InputField.ContentType.Custom;
        age.lineType = TMP_InputField.LineType.SingleLine;
        age.keyboardType = TouchScreenKeyboardType.NumberPad;
        age.onValidateInput = (_, __, c) => c >= '0' && c <= '9' ? c : '\0';
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            slots[i] = playerPanel.transform.Find("player" + (i + 1));
            slots[i].GetComponent<Button>().onClick.AddListener(() => Select(index));
            slots[i].Find("name").GetComponent<TMP_Text>().richText = false;
        }
        newPlayerButton = playerPanel.transform.Find("newplayer").GetComponent<Button>();
        newPlayerButton.onClick.AddListener(OpenRegistration);
        selectButton = playerPanel.transform.Find("selectbutton").GetComponent<Button>();
        selectButton.onClick.AddListener(StartSelected);
        var target = registrationPanel.transform.Find("Startbutton").gameObject;
        registerButton = target.GetComponent<Button>();
        if (registerButton == null) registerButton = target.AddComponent<Button>();
        registerButton.targetGraphic = target.GetComponent<Image>();
        registerButton.onClick.AddListener(Register);
        nickname.onValueChanged.AddListener(_ => UpdateRegisterButton());
        age.onValueChanged.AddListener(_ => UpdateRegisterButton());
        playerPanel.transform.Find("selectbutton/name").GetComponent<TMP_Text>().richText = false;
        Refresh();
        UpdateRegisterButton();
    }
    private bool Exists(int i) => data.players[i] != null &&
        !string.IsNullOrWhiteSpace(data.players[i].nickname);
    private int EmptySlot()
    {
        for (int i = 0; i < 2; i++) if (!Exists(i)) return i;
        return -1;
    }
    public void OpenPlayers()
    {
        registrationPanel.SetActive(false);
        Refresh();
        playerPanel.SetActive(true);
    }
    private void OpenRegistration()
    {
        if (EmptySlot() < 0) return;
        nickname.text = "";
        age.text = "";
        registrationPanel.transform.Find("Toggle Group/noselect").GetComponent<Toggle>().isOn = true;
        registrationPanel.SetActive(true);
        UpdateRegisterButton();
        nickname.ActivateInputField();
    }
    private bool ValidRegistration()
    {
        if (string.IsNullOrWhiteSpace(nickname.text) || nickname.text.Length > 10 ||
            string.IsNullOrEmpty(age.text)) return false;
        foreach (char c in age.text)
            if (c < '0' || c > '9') return false;
        return int.TryParse(age.text, out int value) && value >= 0;
    }
    private void UpdateRegisterButton()
    {
        registerButton.interactable = EmptySlot() >= 0 && ValidRegistration();
    }
    private void Register()
    {
        int index = EmptySlot();
        string name = nickname.text.Trim();
        if (index < 0 || !ValidRegistration()) return;
        string gender = "noselect";
        foreach (string value in new[] { "man", "woman", "noselect" })
            if (registrationPanel.transform.Find("Toggle Group/" + value).GetComponent<Toggle>().isOn)
                gender = value;
        data.players[index] = new PlayerProfileStore.Profile { nickname = name, age = age.text.Trim(), gender = gender,
            userId = index == 0 ? PlayerProfileStore.LegacyUserId : Guid.NewGuid().ToString("N") };
        data.selected = index;
        Save();
        registrationPanel.SetActive(false);
        playerPanel.SetActive(true);
        Refresh();
    }
    private void Select(int index)
    {
        if (!Exists(index)) return;
        data.selected = index;
        Save();
        Refresh();
    }
    private void Refresh()
    {
        if (!Exists(data.selected))
            for (int i = 0; i < 2; i++) if (Exists(i)) { data.selected = i; break; }
        for (int i = 0; i < 2; i++)
        {
            bool exists = Exists(i);
            slots[i].Find("name").GetComponent<TMP_Text>().text = exists ? data.players[i].nickname : "";
            // Preserve the icon artwork already assigned to each slot.
            foreach (Transform child in slots[i])
                if (child.name.Trim() == "icon") child.gameObject.SetActive(exists);
            slots[i].GetComponent<Button>().interactable = exists;
        }
        newPlayerButton.gameObject.SetActive(EmptySlot() >= 0);
        bool selected = Exists(data.selected);
        playerPanel.transform.Find("selectbutton/name").GetComponent<TMP_Text>().text =
            selected ? data.players[data.selected].nickname : "";
        selectButton.interactable = selected;
    }
    private void Save()
    {
        PlayerProfileStore.Save(data);
    }
    private void StartSelected()
    {
        if (Exists(data.selected)) sceneManager.OpenLevel("StageTop");
    }
}


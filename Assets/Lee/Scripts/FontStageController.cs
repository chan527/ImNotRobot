using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FontStageController : MonoBehaviour
{
    private enum FontMode
    {
        Default,
        Clear
    }

    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TMP_Text ambiguousInputText;
    [SerializeField] private TMP_Text resultText;

    [SerializeField] private TMP_FontAsset ambiguousFont;
    [SerializeField] private TMP_FontAsset readableFont;
    [SerializeField] private GameObject fontSettingsPanel;
    [SerializeField] private Image ambiguousFontOptionBackground;
    [SerializeField] private Image readableFontOptionBackground;

    [SerializeField] private List<string> answers;

    private FontMode currentFontMode;
    private int currentAnswerIndex;

    private void OnEnable()
    {
        ambiguousInputText.raycastTarget = false;

        answerInput.onValueChanged.AddListener(UpdateInputText);
        InitStage();
    }

    private void OnDisable()
    {
        answerInput.onValueChanged.RemoveListener(UpdateInputText);
    }

    /// <summary>
    /// 스테이지 시작 값 설정 함수
    /// </summary>
    private void InitStage()
    {
        if (answers == null || answers.Count == 0)
        {
            Debug.LogError("정답 목록이 비어 있습니다.", this);
            return;
        }

        currentAnswerIndex = Random.Range(0, answers.Count);

        answerInput.gameObject.SetActive(true);
        answerInput.SetTextWithoutNotify(string.Empty);
        resultText.text = string.Empty;
        fontSettingsPanel.SetActive(false);

        SetFontMode(FontMode.Default);
    }

    private void SetFontMode(FontMode fontMode)
    {
        currentFontMode = fontMode;

        switch (fontMode)
        {
            case FontMode.Default:
                questionText.font = ambiguousFont;
                questionText.text = CreateAmbiguousText(answers[currentAnswerIndex]);

                ambiguousInputText.font = ambiguousFont;
                ambiguousInputText.gameObject.SetActive(true);

                SetInputTextVisible(false);
                UpdateInputText(answerInput.text);
                break;

            case FontMode.Clear:
                questionText.font = readableFont;
                questionText.text = answers[currentAnswerIndex];

                ambiguousInputText.gameObject.SetActive(false);

                answerInput.textComponent.font = readableFont;
                SetInputTextVisible(true);
                break;
        }

        UpdateFontOptionSelection();
    }

    private void UpdateFontOptionSelection()
    {
        Color32 selectedColor = new Color32(26, 132, 128, 255);
        Color32 normalColor = new Color32(67, 78, 87, 255);
        bool isDefault = currentFontMode == FontMode.Default;

        ambiguousFontOptionBackground.color = isDefault ? selectedColor : normalColor;
        readableFontOptionBackground.color = isDefault ? normalColor : selectedColor;
    }

    private void UpdateInputText(string input)
    {
        if (currentFontMode != FontMode.Default)
            return;

        ambiguousInputText.text = CreateAmbiguousText(input);
    }

    private void SetInputTextVisible(bool isVisible)
    {
        answerInput.textComponent.color = isVisible ? Color.black : Color.clear;
    }

    private string CreateAmbiguousText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = text.Replace("vv", "w");

        text = text
            .Replace('1', 'l')
            .Replace('I', 'l')
            .Replace('O', '0')
            .Replace('5', 'S');

        return text;
    }

    public void ToggleFontSettings()
    {
        fontSettingsPanel.SetActive(!fontSettingsPanel.activeSelf);
    }

    public void ChangeToDefaultMode()
    {
        SetFontMode(FontMode.Default);
        fontSettingsPanel.SetActive(false);
    }

    public void ChangeToClearMode()
    {
        SetFontMode(FontMode.Clear);
        fontSettingsPanel.SetActive(false);
    }

    public bool CheckAnswer()
    {
        if (answers == null || answers.Count == 0)
            return false;

        if (currentFontMode != FontMode.Clear)
            return false;

        return answerInput.text == answers[currentAnswerIndex];
    }

    public void SubmitAnswer()
    {
        bool isCorrect = CheckAnswer();

        resultText.color = isCorrect
            ? new Color32(25, 135, 84, 255)
            : new Color32(190, 55, 55, 255);
        resultText.text = isCorrect
            ? "Verification complete."
            : "Verification failed.";
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoSentenceStageController : MonoBehaviour
{
    [Serializable]
    public class SentenceVariation
    {
        [TextArea(1, 2)] public string sentenceToCopy;
        public string trigger;
        [TextArea(3, 8)] public string autoCompleteSentence;
    }

    [Header("UI Reference")]
    [SerializeField] private TMP_Text sentenceDisplay;
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject autoCompletePanel;
    [SerializeField] private TMP_Text autoCompleteStatusText;

    [SerializeField, Min(0.1f)]
    private float autoCompleteDuration = 1f;
    [SerializeField, Min(0f)] private float panelHideDelay = 0.8f;

    [Header("Sentence Variations")]
    [SerializeField] private List<SentenceVariation> variations =
        new List<SentenceVariation>
        {
            new SentenceVariation
            {
                sentenceToCopy = "햇빛이 강하니 파라솔을 펼쳐 주세요.",
                trigger = "파라",
                autoCompleteSentence =
                    "파라파라나 춰야겠다 오즈잼으로 오이데 오이데 마떼루요"
            },
            new SentenceVariation
            {
                sentenceToCopy = "오늘 만든 간식은 정말 맛있다.",
                trigger = "맛있",
                autoCompleteSentence =
                    "맛있다! 마트 다녀오셨어요?아니.. 영기 엄마가 텃밭에서 고구마 호박을.. 호박 고구마요^^ 그래! 호굼..!..아니... 호구마요? 호~박~고~... 호박 고구마...호박 고구마!!!"
            },
            new SentenceVariation
            {
                sentenceToCopy = "쉽게 포기하지 않는 마음을 순정이라 부른다.",
                trigger = "순정",
                autoCompleteSentence =
                    "(나도 )순정이 있다. 니가 이런 식으로 내 순정을 짓밟으면은, 마 그때는 깡패가 되는 거야! 내가 널 깡패처럼 납치라도 하랴? 앉어!"
            },
            new SentenceVariation
            {
                sentenceToCopy = "오늘의 마지막 인사는 안녕.",
                trigger = "안녕",
                autoCompleteSentence =
                    "안녕OZ야너를처음본순간부터좋아했어방학전에고백하고싶었는데바보같이그땐용기가없더라지금은이수많은사람들앞에서오로지너만사랑한다고말하고싶어서큰마음먹고용기내어봐매일매일버스에서너볼때마다두근댔고동아리랑과활동에서도너만보이고너생각만나고지난3월부터계속그랬어니가남자친구랑헤어지고니맘이아파울때내마음도너무아팠지만내심좋은맘두있었어이런내맘을어떻게말할지고민하다가정말인생에서제일크게용기내어세상에서제일멋지게많은사람들앞에서너한테고백해주고싶었어사랑하는OZ님내연인이되줄래?아니나만의태양이되어줄래?난너의달님이될게내일3시반에너수업마치고학관앞에서기다리고있을게너를사랑하는..."
            }
        };

    private SentenceVariation currentVariation;
    private Coroutine autoCompleteCoroutine;
    private bool hasTriggered;
    private bool isAutoCompleting;
    private string generatedText = string.Empty;

    private void OnEnable()
    {
        if (playerInputField != null)
            playerInputField.onValueChanged.AddListener(OnInputValueChanged);

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitButtonClicked);

        InitializeStage();
    }

    private void OnDisable()
    {
        if (playerInputField != null)
            playerInputField.onValueChanged.RemoveListener(OnInputValueChanged);

        if (submitButton != null)
            submitButton.onClick.RemoveListener(OnSubmitButtonClicked);

        StopAutoComplete();
    }

    private void OnValidate()
    {
        autoCompleteDuration = Mathf.Max(0.1f, autoCompleteDuration);
        panelHideDelay = Mathf.Max(0f, panelHideDelay);
    }

    private void InitializeStage()
    {
        StopAutoComplete();
        hasTriggered = false;

        if (autoCompletePanel != null)
            autoCompletePanel.SetActive(false);

        if (variations == null || variations.Count == 0)
        {
            Debug.LogError("[AutoSentenceStage] 등록된 문장 배리에이션이 없습니다.", this);
            SetInputAvailable(false);
            return;
        }

        currentVariation = variations[UnityEngine.Random.Range(0, variations.Count)];

        if (!IsVariationValid(currentVariation))
        {
            Debug.LogError("[AutoSentenceStage] 배리에이션 데이터가 비어 있습니다.", this);
            SetInputAvailable(false);
            return;
        }

        if (sentenceDisplay != null)
            sentenceDisplay.text = currentVariation.sentenceToCopy;

        SetInputAvailable(true);

        if (playerInputField != null)
        {
            playerInputField.readOnly = false;
            playerInputField.characterLimit = 0;
            playerInputField.textComponent.overflowMode =
                TextOverflowModes.Overflow;

            playerInputField.SetTextWithoutNotify(string.Empty);
            playerInputField.ActivateInputField();
        }
    }

    private static bool IsVariationValid(SentenceVariation variation)
    {
        return variation != null &&
               !string.IsNullOrEmpty(variation.sentenceToCopy) &&
               !string.IsNullOrEmpty(variation.trigger) &&
               !string.IsNullOrEmpty(variation.autoCompleteSentence);
    }

    private void OnInputValueChanged(string input)
    {
        if (hasTriggered || isAutoCompleting || currentVariation == null)
            return;

        int triggerIndex = input.IndexOf(
            currentVariation.trigger,
            StringComparison.Ordinal);

        if (triggerIndex < 0)
            return;

        hasTriggered = true;
        autoCompleteCoroutine = StartCoroutine(PlayAutoComplete(input));
    }

    private IEnumerator PlayAutoComplete(string inputAtTrigger)
    {
        isAutoCompleting = true;

        if (submitButton != null)
            submitButton.interactable = false;

        if (autoCompleteStatusText != null)
            autoCompleteStatusText.text = "지능형 자동 완성 적용 중...";

        if (autoCompletePanel != null)
            autoCompletePanel.SetActive(true);

        int triggerIndex = currentVariation.autoCompleteSentence.IndexOf(
            currentVariation.trigger,
            StringComparison.Ordinal);
        int continuationIndex = triggerIndex >= 0
            ? triggerIndex + currentVariation.trigger.Length
            : 0;
        string continuation = currentVariation.autoCompleteSentence.Substring(
            continuationIndex);

        generatedText = inputAtTrigger;

        float elapsed = 0f;
        int previousVisibleCount = 0;

        while (elapsed < autoCompleteDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / autoCompleteDuration);

            int visibleCount = Mathf.FloorToInt(
                continuation.Length * progress);

            if (visibleCount != previousVisibleCount)
            {
                int previousLength = playerInputField.text.Length;
                int anchorPosition =
                    playerInputField.selectionAnchorPosition;
                int focusPosition =
                    playerInputField.selectionFocusPosition;

                bool followEnd =
                    anchorPosition == previousLength &&
                    focusPosition == previousLength;

                generatedText =
                    inputAtTrigger +
                    continuation.Substring(0, visibleCount);

                SetInputText(
                    generatedText,
                    anchorPosition,
                    focusPosition,
                    followEnd);

                previousVisibleCount = visibleCount;
            }

            yield return null;
        }

        isAutoCompleting = false;

        if (submitButton != null)
            submitButton.interactable = true;

        if (autoCompleteStatusText != null)
            autoCompleteStatusText.text = "자동 완성됨 · 내용을 직접 수정하세요.";

        if (panelHideDelay > 0f)
            yield return new WaitForSeconds(panelHideDelay);

        if (autoCompletePanel != null)
            autoCompletePanel.SetActive(false);

        if (playerInputField != null)
            playerInputField.ActivateInputField();

        autoCompleteCoroutine = null;
    }

    private void SetInputText(
        string value,
        int anchorPosition,
        int focusPosition,
        bool followEnd)
    {
        if (playerInputField == null)
            return;

        playerInputField.SetTextWithoutNotify(value);

        if (followEnd)
        {
            playerInputField.selectionAnchorPosition = value.Length;
            playerInputField.selectionFocusPosition = value.Length;
        }
        else
        {
            playerInputField.selectionAnchorPosition =
                Mathf.Clamp(anchorPosition, 0, value.Length);

            playerInputField.selectionFocusPosition =
                Mathf.Clamp(focusPosition, 0, value.Length);
        }

        // 반드시 커서 위치를 넣은 다음 호출합니다.
        playerInputField.ForceLabelUpdate();
    }

    private void OnSubmitButtonClicked()
    {
        if (isAutoCompleting || playerInputField == null || currentVariation == null)
            return;

        SetInputAvailable(false);

        bool isCorrect = string.Equals(
            playerInputField.text.Trim(),
            currentVariation.sentenceToCopy,
            StringComparison.Ordinal);

        if (isCorrect)
        {
            Debug.Log("[AutoSentenceStage] 스테이지 클리어", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageClear();
        }
        else
        {
            Debug.Log("[AutoSentenceStage] 스테이지 실패", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageFailed();
        }
    }

    private void StopAutoComplete()
    {
        if (autoCompleteCoroutine != null)
        {
            StopCoroutine(autoCompleteCoroutine);
            autoCompleteCoroutine = null;
        }

        isAutoCompleting = false;

        if (playerInputField != null)
            playerInputField.readOnly = false;
    }

    private void SetInputAvailable(bool available)
    {
        if (playerInputField != null)
            playerInputField.interactable = available;

        if (submitButton != null)
            submitButton.interactable = available;
    }
}

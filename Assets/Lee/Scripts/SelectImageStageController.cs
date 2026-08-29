using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectImageStageController : MonoBehaviour
{
    [Serializable]
    public class CaptchaVariation
    {
        public Sprite image;
        [TextArea(1, 2)] public string instruction;
        public List<int> answerTileIndices = new List<int>();
    }

    [Header("UI Reference")]
    [SerializeField] private Image captchaImage;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Button verifyButton;

    [Header("Captcha Variations")]
    [SerializeField] private List<CaptchaVariation> variations =
        new List<CaptchaVariation>();

    [Header("Verification")]
    [SerializeField] private float verificationDelay = 0.5f;

    private Toggle[] tiles = Array.Empty<Toggle>();
    private readonly HashSet<int> currentAnswerIndices = new HashSet<int>();
    private TMP_Text verifyButtonText;
    private string defaultVerifyButtonText = "Verify";
    private bool hasCachedDefaultVerifyButtonText;
    private bool isVerifying;
    private Coroutine verificationCoroutine;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (verifyButton != null)
            verifyButton.onClick.AddListener(OnVerifyClicked);

        InitializeStage();
    }

    private void OnDisable()
    {
        if (verifyButton != null)
            verifyButton.onClick.RemoveListener(OnVerifyClicked);

        if (verificationCoroutine != null)
        {
            StopCoroutine(verificationCoroutine);
            verificationCoroutine = null;
        }
    }

    private void CacheReferences()
    {
        if (tileRoot != null)
        {
            tiles = tileRoot
                .GetComponentsInChildren<Toggle>(true)
                .OrderBy(toggle => toggle.transform.GetSiblingIndex())
                .ToArray();
        }

        if (verifyButton == null)
            return;

        verifyButtonText = verifyButton.GetComponentInChildren<TMP_Text>(true);

        if (verifyButtonText != null && !hasCachedDefaultVerifyButtonText)
        {
            defaultVerifyButtonText = verifyButtonText.text;
            hasCachedDefaultVerifyButtonText = true;
        }
    }

    private void InitializeStage()
    {
        CacheReferences();

        isVerifying = false;
        currentAnswerIndices.Clear();

        foreach (Toggle tile in tiles)
        {
            tile.SetIsOnWithoutNotify(false);
            tile.interactable = true;
        }

        if (verifyButton != null)
            verifyButton.interactable = true;

        if (verifyButtonText != null)
            verifyButtonText.text = defaultVerifyButtonText;

        SelectRandomVariation();
    }

    private void SelectRandomVariation()
    {
        if (variations == null || variations.Count == 0)
        {
            Debug.LogError("[SelectImageStage] 등록된 캡챠 배리에이션이 없습니다.", this);
            SetVerificationAvailable(false);
            return;
        }

        int variationIndex = UnityEngine.Random.Range(0, variations.Count);
        CaptchaVariation variation = variations[variationIndex];

        if (variation == null)
        {
            Debug.LogError("[SelectImageStage] 비어 있는 캡챠 배리에이션입니다.", this);
            SetVerificationAvailable(false);
            return;
        }

        if (captchaImage != null)
            captchaImage.sprite = variation.image;

        if (instructionText != null)
            instructionText.text = variation.instruction;

        if (variation.image == null)
        {
            Debug.LogError("[SelectImageStage] 배리에이션 이미지가 비어 있습니다.", this);
            SetVerificationAvailable(false);
            return;
        }

        if (variation.answerTileIndices == null ||
            variation.answerTileIndices.Count == 0)
        {
            Debug.LogError("[SelectImageStage] 정답 타일 인덱스가 비어 있습니다.", this);
            SetVerificationAvailable(false);
            return;
        }

        foreach (int index in variation.answerTileIndices)
        {
            if (index < 0 || index >= tiles.Length)
            {
                Debug.LogWarning(
                    $"[SelectImageStage] 타일 인덱스 {index}가 범위를 벗어났습니다.",
                    this);
                continue;
            }

            currentAnswerIndices.Add(index);
        }

        if (currentAnswerIndices.Count == 0)
        {
            Debug.LogError("[SelectImageStage] 사용할 수 있는 정답 인덱스가 없습니다.", this);
            SetVerificationAvailable(false);
        }
    }

    private void OnVerifyClicked()
    {
        if (isVerifying)
            return;

        isVerifying = true;
        SetVerificationAvailable(false);

        if (verifyButtonText != null)
            verifyButtonText.text = "Verifying...";

        bool isCorrect = IsExactSelection();
        verificationCoroutine = StartCoroutine(CompleteVerification(isCorrect));
    }

    private bool IsExactSelection()
    {
        for (int index = 0; index < tiles.Length; index++)
        {
            bool shouldBeSelected = currentAnswerIndices.Contains(index);

            if (tiles[index].isOn != shouldBeSelected)
                return false;
        }

        return true;
    }

    private IEnumerator CompleteVerification(bool isCorrect)
    {
        yield return new WaitForSeconds(verificationDelay);
        verificationCoroutine = null;

        if (isCorrect)
        {
            Debug.Log("[SelectImageStage] 스테이지 클리어", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageClear();
        }
        else
        {
            Debug.Log("[SelectImageStage] 스테이지 실패", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageFailed();
        }
    }

    private void SetVerificationAvailable(bool available)
    {
        if (verifyButton != null)
            verifyButton.interactable = available;

        foreach (Toggle tile in tiles)
            tile.interactable = available;
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class CarStageController : MonoBehaviour
{
    [SerializeField] private RectTransform gridArea;   // GridMask
    [SerializeField] private RectTransform carRect;    // CarImage
    [SerializeField] private GameObject redLight;
    [SerializeField] private GameObject greenLight;

    [SerializeField] private Transform tileRoot;
    [SerializeField] private int[] carTileIndices;
    private Toggle[] tiles;           // Tile_00 ~ Tile_24

    [SerializeField] private Button verifyButton;

    [SerializeField] private Vector2 startPos = new Vector2(33f, -82f);
    [SerializeField] private Vector2 endPos = new Vector2(100f, -22f);
    [SerializeField] private float departureDelay = 0.5f;
    [SerializeField] private float driveDuration = 1f;
    [SerializeField] private float verificationDelay = 1f;

    private Canvas rootCanvas;
    private TMP_Text verifyButtonText;
    private string defaultVerifyButtonText;
    private bool wasPointerInsideGrid;
    private bool hasDepartureStarted;
    private bool isDepartureRunning;
    private bool hasCarFinishedMoving;
    private bool isVerifying;
    private Coroutine departureCoroutine;

    private void Awake()
    {
        tiles = tileRoot
                .GetComponentsInChildren<Toggle>(true)
                .OrderBy(toggle => toggle.transform.GetSiblingIndex())
                .ToArray();

        rootCanvas = gridArea.GetComponentInParent<Canvas>();
        verifyButtonText = verifyButton.GetComponentInChildren<TMP_Text>(true);
        defaultVerifyButtonText = verifyButtonText.text;
    }

    private void OnEnable()
    {
        verifyButton.onClick.AddListener(OnVerifyClicked);
        InitStage();
    }

    private void OnDisable()
    {
        verifyButton.onClick.RemoveListener(OnVerifyClicked);

        if (departureCoroutine != null)
        {
            StopCoroutine(departureCoroutine);
            departureCoroutine = null;
        }
    }

    private void Update()
    {
        if (hasDepartureStarted)
            return;

        bool isPointerInsideGrid = IsPointerInsideGrid();

        if (wasPointerInsideGrid && !isPointerInsideGrid && IsExactCarSelection())
        {
            hasDepartureStarted = true;
            StartCarDeparture();
        }

        wasPointerInsideGrid = isPointerInsideGrid;
    }

    private bool IsPointerInsideGrid()
    {
        if (Pointer.current == null)
            return false;

        Camera eventCamera = rootCanvas != null &&
                             rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        Vector2 pointerPosition = Pointer.current.position.ReadValue();

        return RectTransformUtility.RectangleContainsScreenPoint(
            gridArea,
            pointerPosition,
            eventCamera);
    }

    private bool IsExactCarSelection()
    {
        if (carTileIndices == null || carTileIndices.Length == 0)
            return false;

        int selectedCount = tiles.Count(tile => tile.isOn);

        if (selectedCount != carTileIndices.Length)
            return false;

        return carTileIndices.All(index =>
            index >= 0 && index < tiles.Length && tiles[index].isOn);
    }

    private void InitStage()
    {
        carRect.gameObject.SetActive(true);
        carRect.anchoredPosition = startPos;
        carRect.localScale = new Vector3(-1f, 1f, 1f);

        foreach (Toggle tile in tiles)
        {
            tile.SetIsOnWithoutNotify(false);
            tile.interactable = true;
        }

        TurnOnRedLight();

        verifyButton.interactable = true;
        verifyButtonText.text = defaultVerifyButtonText;

        wasPointerInsideGrid = IsPointerInsideGrid();
        hasDepartureStarted = false;
        isDepartureRunning = false;
        hasCarFinishedMoving = false;
        isVerifying = false;
        departureCoroutine = null;
    }

    private void TurnOnRedLight()
    {
        redLight.SetActive(true);
        greenLight.SetActive(false);
    }

    private void TurnOnGreenLight()
    {
        redLight.SetActive(false);
        greenLight.SetActive(true);
    }

    private void StartCarDeparture()
    {
        TurnOnGreenLight();
        isDepartureRunning = true;
        departureCoroutine = StartCoroutine(DriveCarAway());
    }

    private IEnumerator DriveCarAway()
    {
        yield return new WaitForSeconds(departureDelay);

        Vector2 movementStartPosition = carRect.anchoredPosition;
        Vector3 movementStartScale = carRect.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < driveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / driveDuration);

            carRect.anchoredPosition = Vector2.Lerp(
                movementStartPosition,
                endPos,
                progress);
            carRect.localScale = Vector3.Lerp(
                movementStartScale,
                Vector3.zero,
                progress);

            yield return null;
        }

        carRect.anchoredPosition = endPos;
        carRect.localScale = Vector3.zero;

        isDepartureRunning = false;
        hasCarFinishedMoving = true;
        departureCoroutine = null;

        if (isVerifying)
            ValidateCurrentSelection();
    }

    private void OnVerifyClicked()
    {
        if (isVerifying)
            return;

        isVerifying = true;
        verifyButton.interactable = false;
        verifyButtonText.text = "Verifying...";
        SetTilesInteractable(false);

        if (!hasDepartureStarted)
        {
            StartCoroutine(CompleteVerification(false));
            return;
        }

        if (isDepartureRunning)
            return;

        if (hasCarFinishedMoving)
            ValidateCurrentSelection();
    }

    private void ValidateCurrentSelection()
    {
        bool hasNoSelectedTiles = tiles.All(tile => !tile.isOn);
        StartCoroutine(CompleteVerification(hasNoSelectedTiles));
    }

    private IEnumerator CompleteVerification(bool isCorrect)
    {
        yield return new WaitForSeconds(verificationDelay);

        if (isCorrect)
        {
            Debug.Log("자동차 스테이지 클리어!");
            CGameManager.Instance.StageClear();
        }
        else
        {
            Debug.Log("자동차 스테이지 실패");
            CGameManager.Instance.StageFailed();
        }
    }

    private void SetTilesInteractable(bool interactable)
    {
        foreach (Toggle tile in tiles)
            tile.interactable = interactable;
    }
}

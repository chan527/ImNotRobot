using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FallingImageStageController : MonoBehaviour
{
    public enum CaptchaCategory
    {
        Fruit,
        Animal
    }

    public enum CaptchaItemType
    {
        Apple,
        Banana,
        Orange,
        Strawberry,
        Cat,
        Dog,
        Rabbit,
        Bird
    }

    [Serializable]
    public class CaptchaItemImages
    {
        [TextArea(1, 2)] public string instruction;
        public List<Sprite> images = new List<Sprite>();

        public CaptchaItemImages()
        {
        }

        public CaptchaItemImages(string instruction)
        {
            this.instruction = instruction;
        }
    }

    private sealed class RuntimeItemDefinition
    {
        public CaptchaCategory Category { get; }
        public CaptchaItemType Type { get; }
        public CaptchaItemImages Data { get; }

        public RuntimeItemDefinition(
            CaptchaCategory category,
            CaptchaItemType type,
            CaptchaItemImages data)
        {
            Category = category;
            Type = type;
            Data = data;
        }
    }

    private sealed class TileMotion
    {
        public Toggle Toggle { get; }
        public RectTransform RectTransform { get; }
        public Vector2 Velocity;
        public float AngularVelocity;
        public float AngularAcceleration;
        public bool IsMoving;
        public bool IsQueued;
        public bool HasEnteredBounds;
        public float FlightTime;

        public TileMotion(Toggle toggle)
        {
            Toggle = toggle;
            RectTransform = toggle.transform as RectTransform;
        }
    }

    [Header("UI Reference")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Button verifyButton;
    [SerializeField] private List<Image> tileImages = new List<Image>();

    [Header("Grid Motion Reference")]
    [SerializeField] private RectTransform movementBounds;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Fruit Images")]
    [SerializeField] private CaptchaItemImages apple =
        new CaptchaItemImages("Select every square containing an apple.");
    [SerializeField] private CaptchaItemImages banana =
        new CaptchaItemImages("Select every square containing a banana.");
    [SerializeField] private CaptchaItemImages orange =
        new CaptchaItemImages("Select every square containing an orange.");
    [SerializeField] private CaptchaItemImages strawberry =
        new CaptchaItemImages("Select every square containing a strawberry.");

    [Header("Animal Images")]
    [SerializeField] private CaptchaItemImages cat =
        new CaptchaItemImages("Select every square containing a cat.");
    [SerializeField] private CaptchaItemImages dog =
        new CaptchaItemImages("Select every square containing a dog.");
    [SerializeField] private CaptchaItemImages rabbit =
        new CaptchaItemImages("Select every square containing a rabbit.");
    [SerializeField] private CaptchaItemImages bird =
        new CaptchaItemImages("Select every square containing a bird.");

    [Header("Generation")]
    [SerializeField, Min(1)] private int minimumAnswerCount = 3;
    [SerializeField, Min(1)] private int maximumAnswerCount = 6;

    [Header("Grid Motion")]
    [SerializeField, Min(0f)] private float minimumCollapseDelay = 1f;
    [SerializeField, Min(0f)] private float maximumCollapseDelay = 2f;
    [SerializeField] private float gravity = -450f;
    [SerializeField] private Vector2 collapseHorizontalSpeed = new Vector2(20f, 70f);
    [SerializeField] private Vector2 collapseVerticalSpeed = new Vector2(-20f, 40f);
    [SerializeField] private Vector2 launchTravelTime = new Vector2(1.3f, 1.7f);
    [SerializeField] private Vector2 centerTargetSpread = new Vector2(70f, 50f);
    [SerializeField] private Vector2 angularSpeed = new Vector2(50f, 140f);
    [SerializeField] private Vector2 angularAcceleration = new Vector2(10f, 35f);
    [SerializeField, Min(0.01f)] private float launchInterval = 0.45f;
    [SerializeField, Min(0f)] private float spawnPadding = 12f;

    [Header("Verification")]
    [SerializeField] private float verificationDelay = 0.5f;

    private Toggle[] tiles = Array.Empty<Toggle>();
    private readonly HashSet<int> currentAnswerIndices = new HashSet<int>();
    private readonly List<TileMotion> movingTiles = new List<TileMotion>();
    private readonly Queue<TileMotion> outsideTileQueue = new Queue<TileMotion>();
    private TMP_Text verifyButtonText;
    private string defaultVerifyButtonText = "Verify";
    private bool hasCachedDefaultVerifyButtonText;
    private bool isVerifying;
    private Coroutine verificationCoroutine;
    private Coroutine motionCoroutine;

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

        StopTileMotion();
        RestoreGridLayout();

        if (verificationCoroutine != null)
        {
            StopCoroutine(verificationCoroutine);
            verificationCoroutine = null;
        }
    }

    private void OnValidate()
    {
        minimumAnswerCount = Mathf.Max(1, minimumAnswerCount);
        maximumAnswerCount = Mathf.Max(minimumAnswerCount, maximumAnswerCount);
        minimumCollapseDelay = Mathf.Max(0f, minimumCollapseDelay);
        maximumCollapseDelay = Mathf.Max(
            minimumCollapseDelay,
            maximumCollapseDelay);
        launchTravelTime.x = Mathf.Max(0.1f, launchTravelTime.x);
        launchTravelTime.y = Mathf.Max(launchTravelTime.x, launchTravelTime.y);
        centerTargetSpread.x = Mathf.Max(0f, centerTargetSpread.x);
        centerTargetSpread.y = Mathf.Max(0f, centerTargetSpread.y);
        launchInterval = Mathf.Max(0.01f, launchInterval);
        spawnPadding = Mathf.Max(0f, spawnPadding);
    }

    private void CacheReferences()
    {
        if (tileRoot != null)
        {
            tiles = tileRoot
                .GetComponentsInChildren<Toggle>(true)
                .OrderBy(toggle => toggle.transform.GetSiblingIndex())
                .ToArray();

            if (movementBounds == null)
                movementBounds = tileRoot as RectTransform;

            if (gridLayoutGroup == null)
                gridLayoutGroup = tileRoot.GetComponent<GridLayoutGroup>();
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
        StopTileMotion();
        RestoreGridLayout();

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

        if (GenerateRandomCaptcha())
            motionCoroutine = StartCoroutine(RunTileMotion());
    }

    private bool GenerateRandomCaptcha()
    {
        if (!HasValidTileReferences())
        {
            SetVerificationAvailable(false);
            return false;
        }

        List<RuntimeItemDefinition> configuredItems = GetConfiguredItems();
        List<RuntimeItemDefinition> fruitItems = configuredItems
            .Where(item => item.Category == CaptchaCategory.Fruit)
            .ToList();
        List<RuntimeItemDefinition> animalItems = configuredItems
            .Where(item => item.Category == CaptchaCategory.Animal)
            .ToList();

        List<List<RuntimeItemDefinition>> availableCategories =
            new List<List<RuntimeItemDefinition>>();

        if (fruitItems.Count >= 2)
            availableCategories.Add(fruitItems);

        if (animalItems.Count >= 2)
            availableCategories.Add(animalItems);

        if (availableCategories.Count == 0)
        {
            Debug.LogError(
                "[FallingImageStage] 이미지가 등록된 종류가 2개 이상인 카테고리가 필요합니다.",
                this);
            SetVerificationAvailable(false);
            return false;
        }

        List<RuntimeItemDefinition> selectedCategory =
            availableCategories[UnityEngine.Random.Range(0, availableCategories.Count)];
        RuntimeItemDefinition targetItem =
            selectedCategory[UnityEngine.Random.Range(0, selectedCategory.Count)];

        int maximumAllowedAnswers = Mathf.Max(1, tiles.Length - 1);
        int minimumAnswers = Mathf.Clamp(
            minimumAnswerCount,
            1,
            maximumAllowedAnswers);
        int maximumAnswers = Mathf.Clamp(
            maximumAnswerCount,
            minimumAnswers,
            maximumAllowedAnswers);
        int answerCount = UnityEngine.Random.Range(
            minimumAnswers,
            maximumAnswers + 1);

        List<RuntimeItemDefinition> placements =
            new List<RuntimeItemDefinition>(tiles.Length);

        for (int index = 0; index < answerCount; index++)
            placements.Add(targetItem);

        List<RuntimeItemDefinition> distractors = selectedCategory
            .Where(item => item.Type != targetItem.Type)
            .ToList();

        while (placements.Count < tiles.Length)
        {
            placements.Add(
                distractors[UnityEngine.Random.Range(0, distractors.Count)]);
        }

        Shuffle(placements);
        currentAnswerIndices.Clear();

        for (int index = 0; index < tiles.Length; index++)
        {
            RuntimeItemDefinition item = placements[index];
            tileImages[index].sprite = GetRandomImage(item.Data.images);
            tileImages[index].enabled = true;

            if (item.Type == targetItem.Type)
                currentAnswerIndices.Add(index);
        }

        if (instructionText != null)
        {
            instructionText.text = string.IsNullOrWhiteSpace(targetItem.Data.instruction)
                ? GetDefaultInstruction(targetItem.Type)
                : targetItem.Data.instruction;
        }

        SetVerificationAvailable(true);
        return true;
    }

    private bool HasValidTileReferences()
    {
        if (tiles.Length == 0)
        {
            Debug.LogError("[FallingImageStage] 타일 토글을 찾을 수 없습니다.", this);
            return false;
        }

        if (tileImages == null ||
            tileImages.Count != tiles.Length ||
            tileImages.Any(image => image == null))
        {
            Debug.LogError(
                $"[FallingImageStage] Tile Images에 타일 수와 동일한 {tiles.Length}개의 Image를 연결해야 합니다.",
                this);
            return false;
        }

        return true;
    }

    private List<RuntimeItemDefinition> GetConfiguredItems()
    {
        List<RuntimeItemDefinition> items = new List<RuntimeItemDefinition>();

        AddConfiguredItem(items, CaptchaCategory.Fruit, CaptchaItemType.Apple, apple);
        AddConfiguredItem(items, CaptchaCategory.Fruit, CaptchaItemType.Banana, banana);
        AddConfiguredItem(items, CaptchaCategory.Fruit, CaptchaItemType.Orange, orange);
        AddConfiguredItem(items, CaptchaCategory.Fruit, CaptchaItemType.Strawberry, strawberry);
        AddConfiguredItem(items, CaptchaCategory.Animal, CaptchaItemType.Cat, cat);
        AddConfiguredItem(items, CaptchaCategory.Animal, CaptchaItemType.Dog, dog);
        AddConfiguredItem(items, CaptchaCategory.Animal, CaptchaItemType.Rabbit, rabbit);
        AddConfiguredItem(items, CaptchaCategory.Animal, CaptchaItemType.Bird, bird);

        return items;
    }

    private static void AddConfiguredItem(
        ICollection<RuntimeItemDefinition> items,
        CaptchaCategory category,
        CaptchaItemType type,
        CaptchaItemImages data)
    {
        if (data == null ||
            data.images == null ||
            !data.images.Any(image => image != null))
        {
            return;
        }

        items.Add(new RuntimeItemDefinition(category, type, data));
    }

    private static Sprite GetRandomImage(IReadOnlyList<Sprite> images)
    {
        List<Sprite> availableImages = images
            .Where(image => image != null)
            .ToList();

        return availableImages[
            UnityEngine.Random.Range(0, availableImages.Count)];
    }

    private static string GetDefaultInstruction(CaptchaItemType type)
    {
        switch (type)
        {
            case CaptchaItemType.Apple:
                return "Select every square containing an apple.";
            case CaptchaItemType.Banana:
                return "Select every square containing a banana.";
            case CaptchaItemType.Orange:
                return "Select every square containing an orange.";
            case CaptchaItemType.Strawberry:
                return "Select every square containing a strawberry.";
            case CaptchaItemType.Cat:
                return "Select every square containing a cat.";
            case CaptchaItemType.Dog:
                return "Select every square containing a dog.";
            case CaptchaItemType.Rabbit:
                return "Select every square containing a rabbit.";
            case CaptchaItemType.Bird:
                return "Select every square containing a bird.";
            default:
                return "Select every matching square.";
        }
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            T value = values[index];
            values[index] = values[swapIndex];
            values[swapIndex] = value;
        }
    }

    private IEnumerator RunTileMotion()
    {
        float collapseDelay = UnityEngine.Random.Range(
            minimumCollapseDelay,
            maximumCollapseDelay);
        yield return new WaitForSeconds(collapseDelay);

        if (isVerifying || !PrepareTileMotion())
        {
            motionCoroutine = null;
            yield break;
        }

        gridLayoutGroup.enabled = false;

        foreach (TileMotion tile in movingTiles)
            StartCollapse(tile);

        float launchTimer = launchInterval;

        while (!isVerifying)
        {
            float deltaTime = Time.deltaTime;

            foreach (TileMotion tile in movingTiles)
            {
                if (tile.IsMoving)
                    UpdateTileMotion(tile, deltaTime);
            }

            launchTimer -= deltaTime;

            if (launchTimer <= 0f && outsideTileQueue.Count > 0)
            {
                LaunchNextTile();
                launchTimer = launchInterval;
            }

            yield return null;
        }

        motionCoroutine = null;
    }

    private bool PrepareTileMotion()
    {
        if (movementBounds == null || gridLayoutGroup == null)
        {
            Debug.LogError(
                "[FallingImageStage] Movement Bounds와 Grid Layout Group을 연결해야 합니다.",
                this);
            return false;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            gridLayoutGroup.transform as RectTransform);

        movingTiles.Clear();
        outsideTileQueue.Clear();

        foreach (Toggle toggle in tiles)
        {
            TileMotion tile = new TileMotion(toggle);

            if (tile.RectTransform == null)
            {
                Debug.LogError(
                    "[FallingImageStage] 타일에 RectTransform이 필요합니다.",
                    toggle);
                return false;
            }

            tile.RectTransform.localRotation = Quaternion.identity;
            movingTiles.Add(tile);
        }

        return true;
    }

    private void StartCollapse(TileMotion tile)
    {
        float horizontalDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        tile.Velocity = new Vector2(
            horizontalDirection * RandomFromRange(collapseHorizontalSpeed),
            RandomFromRange(collapseVerticalSpeed));
        tile.AngularVelocity = RandomSignedFromRange(angularSpeed);
        tile.AngularAcceleration = RandomSignedFromRange(angularAcceleration);
        tile.IsMoving = true;
        tile.IsQueued = false;
        tile.HasEnteredBounds = true;
        tile.FlightTime = 0f;
    }

    private void UpdateTileMotion(TileMotion tile, float deltaTime)
    {
        tile.FlightTime += deltaTime;
        tile.Velocity.y += gravity * deltaTime;
        tile.AngularVelocity += tile.AngularAcceleration * deltaTime;

        tile.RectTransform.anchoredPosition += tile.Velocity * deltaTime;
        tile.RectTransform.Rotate(
            0f,
            0f,
            tile.AngularVelocity * deltaTime);

        bool overlapsBounds = IsOverlappingMovementBounds(tile.RectTransform);

        if (overlapsBounds)
        {
            tile.HasEnteredBounds = true;
            tile.Toggle.interactable = !isVerifying;
            return;
        }

        tile.Toggle.interactable = false;

        if (tile.HasEnteredBounds || tile.FlightTime >= 3f)
            QueueOutsideTile(tile);
    }

    private void QueueOutsideTile(TileMotion tile)
    {
        if (tile.IsQueued)
            return;

        tile.IsMoving = false;
        tile.IsQueued = true;
        outsideTileQueue.Enqueue(tile);
    }

    private void LaunchNextTile()
    {
        TileMotion tile = outsideTileQueue.Dequeue();
        Rect bounds = movementBounds.rect;
        RectTransform tileRect = tile.RectTransform;
        float halfWidth = tileRect.rect.width * Mathf.Abs(tileRect.localScale.x) * 0.5f;
        float halfHeight = tileRect.rect.height * Mathf.Abs(tileRect.localScale.y) * 0.5f;
        int spawnSide = UnityEngine.Random.Range(0, 3);
        Vector2 spawnPosition;

        switch (spawnSide)
        {
            case 0:
                spawnPosition = new Vector2(
                    bounds.xMin - halfWidth - spawnPadding,
                    UnityEngine.Random.Range(bounds.yMin, bounds.yMax));
                break;

            case 1:
                spawnPosition = new Vector2(
                    bounds.xMax + halfWidth + spawnPadding,
                    UnityEngine.Random.Range(bounds.yMin, bounds.yMax));
                break;

            default:
                spawnPosition = new Vector2(
                    UnityEngine.Random.Range(bounds.xMin, bounds.xMax),
                    bounds.yMin - halfHeight - spawnPadding);
                break;
        }

        float targetSpreadX = Mathf.Min(
            centerTargetSpread.x,
            bounds.width * 0.3f);
        float targetSpreadY = Mathf.Min(
            centerTargetSpread.y,
            bounds.height * 0.3f);
        Vector2 targetPosition = new Vector2(
            UnityEngine.Random.Range(-targetSpreadX, targetSpreadX),
            UnityEngine.Random.Range(-targetSpreadY, targetSpreadY));
        float travelTime = RandomFromRange(launchTravelTime);
        Vector2 displacement = targetPosition - spawnPosition;

        tile.Velocity = new Vector2(
            displacement.x / travelTime,
            (displacement.y - 0.5f * gravity * travelTime * travelTime) /
            travelTime);

        tileRect.position = movementBounds.TransformPoint(spawnPosition);
        tile.AngularVelocity = RandomSignedFromRange(angularSpeed);
        tile.AngularAcceleration = RandomSignedFromRange(angularAcceleration);
        tile.IsMoving = true;
        tile.IsQueued = false;
        tile.HasEnteredBounds = false;
        tile.FlightTime = 0f;
    }

    private bool IsOverlappingMovementBounds(RectTransform tile)
    {
        Bounds tileBounds = RectTransformUtility
            .CalculateRelativeRectTransformBounds(movementBounds, tile);
        Rect bounds = movementBounds.rect;

        return tileBounds.max.x >= bounds.xMin &&
               tileBounds.min.x <= bounds.xMax &&
               tileBounds.max.y >= bounds.yMin &&
               tileBounds.min.y <= bounds.yMax;
    }

    private static float RandomFromRange(Vector2 range)
    {
        return UnityEngine.Random.Range(
            Mathf.Min(range.x, range.y),
            Mathf.Max(range.x, range.y));
    }

    private static float RandomSignedFromRange(Vector2 range)
    {
        float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        return direction * RandomFromRange(range);
    }

    private void StopTileMotion()
    {
        if (motionCoroutine != null)
        {
            StopCoroutine(motionCoroutine);
            motionCoroutine = null;
        }

        movingTiles.Clear();
        outsideTileQueue.Clear();
    }

    private void RestoreGridLayout()
    {
        if (gridLayoutGroup != null)
            gridLayoutGroup.enabled = true;

        foreach (Toggle tile in tiles)
        {
            RectTransform tileRect = tile.transform as RectTransform;

            if (tileRect != null)
                tileRect.localRotation = Quaternion.identity;
        }

        if (gridLayoutGroup != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(
                gridLayoutGroup.transform as RectTransform);
        }
    }

    private void OnVerifyClicked()
    {
        if (isVerifying)
            return;

        isVerifying = true;
        StopTileMotion();
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
            Debug.Log("[FallingImageStage] 스테이지 클리어", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageClear();
        }
        else
        {
            Debug.Log("[FallingImageStage] 스테이지 실패", this);

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

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SniperStageController : MonoBehaviour
{
    private enum StageState
    {
        Intro,
        Ready,
        Scoped,
        Reviewing,
        Complete
    }

    [Header("UI Reference")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform noZoomView;
    [SerializeField] private RectTransform reviewTarget;
    [SerializeField] private GameObject scopeView;
    [SerializeField] private RectTransform scopeBack;
    [SerializeField] private RectTransform scopeOverlay;
    [SerializeField] private RectTransform scopeTarget;
    [SerializeField] private RectTransform notRobotArea;

    [Header("Zoom Settings")]
    [SerializeField] private float targetStartScale = 30f;
    [SerializeField] private float noZoomStartScale = 10f;
    [SerializeField] private float recedeConstantSpeed = 6f;
    [SerializeField] private float recedeAccelerationSwitchScale = 4f;
    [SerializeField] private float recedeLateAcceleration = 5f;
    [SerializeField] private float recedeNoZoomEndSpeed = 4f;

    [Header("Review Settings")]
    [SerializeField] private float reviewNoZoomStartSpeed = 18f;
    [SerializeField] private float reviewNoZoomEndSpeed = 12f;
    [SerializeField] private float reviewTargetFastSpeed = 20f;
    [SerializeField] private float reviewSlowdownScale = 3.5f;
    [SerializeField, Range(0.05f, 1f)]
    private float reviewSpeedDropMultiplier = 0.45f;
    [SerializeField] private float reviewTargetEndSpeed = 4f;
    [SerializeField] private float reviewTargetDeceleration = 1.5f;

    [Header("Scope Settings")]
    [SerializeField] private float aimSensitivity = 0.35f;
    [SerializeField] private float breathAmplitude = 6f;
    [SerializeField] private float breathFrequency = 0.35f;
    [SerializeField, Range(0f, 1f)] private float randomAimStartMinRadius = 0.25f;
    [SerializeField, Range(0f, 1f)] private float randomAimStartMaxRadius = 0.7f;

    [Header("Impact Marker")]
    [SerializeField] private RectTransform bulletHoleMarker;
    [SerializeField] private float bulletHoleSize = 1.2f;

    private StageState currentState;
    private Vector2 scopeBackStartPosition;
    private Vector2 reviewTargetStartPosition;
    private Vector2 aimOffset;
    private Vector2 hitPointOnReviewTarget;
    private bool hitTarget;
    private bool initialized;
    private bool cursorStateCaptured;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;
    private Coroutine stageCoroutine;

    private void Awake()
    {
        ResolveReferences();

        if (scopeBack != null)
            scopeBackStartPosition = scopeBack.anchoredPosition;

        if (reviewTarget != null)
            reviewTargetStartPosition = reviewTarget.anchoredPosition;
    }

    private void Start()
    {
        initialized = true;
        CaptureAndLockCursor();
        InitializeStage();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            CaptureAndLockCursor();
            InitializeStage();
        }
    }

    private void OnDisable()
    {
        StopStageCoroutine();
    }

    private void OnDestroy()
    {
        RestoreCursorState();
    }

    private void CaptureAndLockCursor()
    {
        if (!cursorStateCaptured)
        {
            previousCursorVisible = Cursor.visible;
            previousCursorLockMode = Cursor.lockState;
            cursorStateCaptured = true;
        }

        LockCursor();
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void RestoreCursorState()
    {
        if (!cursorStateCaptured)
            return;

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        cursorStateCaptured = false;
    }

    private void Update()
    {
        if (currentState != StageState.Complete)
            LockCursor();

        if (Mouse.current == null)
            return;

        if (currentState == StageState.Ready &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            EnterScopeView();
            return;
        }

        if (currentState != StageState.Scoped)
            return;

        UpdateScopeAim();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Fire();
    }

    private void ResolveReferences()
    {
        if (viewport == null)
        {
            Transform foundViewport = transform.Find("CaptchaPanel/SniperScopeUI/ScopeViewport");
            viewport = foundViewport as RectTransform;
        }

        if (viewport == null)
            return;

        if (noZoomView == null)
            noZoomView = viewport.Find("NoZoomView") as RectTransform;

        if (reviewTarget == null)
            reviewTarget = viewport.Find("Target") as RectTransform;

        if (scopeView == null)
        {
            Transform foundScopeView = viewport.Find("ScopeView");
            scopeView = foundScopeView != null ? foundScopeView.gameObject : null;
        }

        if (scopeView == null)
            return;

        Transform scopeViewTransform = scopeView.transform;

        if (scopeBack == null)
            scopeBack = scopeViewTransform.Find("ScopeBack") as RectTransform;

        if (scopeOverlay == null)
            scopeOverlay = scopeViewTransform.Find("ScopeOverlay") as RectTransform;

        if (scopeTarget == null && scopeBack != null)
            scopeTarget = scopeBack.Find("Target") as RectTransform;

        if (notRobotArea == null && reviewTarget != null)
            notRobotArea = reviewTarget.Find("NotRobotArea") as RectTransform;
    }

    private bool HasRequiredReferences()
    {
        return viewport != null &&
               noZoomView != null &&
               reviewTarget != null &&
               scopeView != null &&
               scopeBack != null &&
               scopeOverlay != null &&
               scopeTarget != null &&
               notRobotArea != null;
    }

    private void InitializeStage()
    {
        StopStageCoroutine();
        ResolveReferences();

        if (!HasRequiredReferences())
        {
            Debug.LogError("[SniperStage] 필요한 UI 오브젝트를 찾지 못했습니다.", this);
            RestoreCursorState();
            enabled = false;
            return;
        }

        currentState = StageState.Intro;
        scopeView.SetActive(false);
        noZoomView.gameObject.SetActive(true);
        reviewTarget.gameObject.SetActive(true);

        SetUniformScale(reviewTarget, targetStartScale);
        SetUniformScale(noZoomView, noZoomStartScale);
        reviewTarget.anchoredPosition = reviewTargetStartPosition;

        aimOffset = Vector2.zero;
        scopeBack.anchoredPosition = scopeBackStartPosition;
        hitTarget = false;

        if (bulletHoleMarker != null)
            bulletHoleMarker.gameObject.SetActive(false);

        stageCoroutine = StartCoroutine(MoveTargetAway());
    }

    private IEnumerator MoveTargetAway()
    {
        float targetScale = targetStartScale;
        float speed = Mathf.Max(0.01f, recedeConstantSpeed);
        float accelerationSwitchScale = Mathf.Clamp(
            recedeAccelerationSwitchScale,
            0f,
            targetStartScale);

        while (targetScale > accelerationSwitchScale)
        {
            targetScale = Mathf.MoveTowards(
                targetScale,
                accelerationSwitchScale,
                speed * Time.deltaTime);
            SetUniformScale(reviewTarget, targetScale);
            yield return null;
        }

        while (targetScale > 0f)
        {
            speed += Mathf.Max(0f, recedeLateAcceleration) * Time.deltaTime;
            targetScale = Mathf.MoveTowards(
                targetScale,
                0f,
                speed * Time.deltaTime);
            SetUniformScale(reviewTarget, targetScale);
            yield return null;
        }

        float noZoomScale = noZoomStartScale;
        float inheritedSpeed = speed;

        while (noZoomScale > 1f)
        {
            float progress = Mathf.InverseLerp(noZoomStartScale, 1f, noZoomScale);
            speed = Mathf.Lerp(
                inheritedSpeed,
                recedeNoZoomEndSpeed,
                progress);
            speed = Mathf.Max(0.01f, speed);

            noZoomScale = Mathf.MoveTowards(noZoomScale, 1f, speed * Time.deltaTime);
            SetUniformScale(noZoomView, noZoomScale);
            yield return null;
        }

        SetUniformScale(reviewTarget, 0f);
        SetUniformScale(noZoomView, 1f);
        currentState = StageState.Ready;
        stageCoroutine = null;
    }

    private void EnterScopeView()
    {
        currentState = StageState.Scoped;
        scopeView.SetActive(true);
        aimOffset = GetRandomAimStartOffset();
        scopeBack.anchoredPosition = scopeBackStartPosition + aimOffset;
    }

    private Vector2 GetRandomAimStartOffset()
    {
        Vector2 movementLimit = GetScopeMovementLimit();
        float minRadius = Mathf.Clamp01(randomAimStartMinRadius);
        float maxRadius = Mathf.Max(
            minRadius,
            Mathf.Clamp01(randomAimStartMaxRadius));
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minRadius, maxRadius);

        return new Vector2(
            Mathf.Cos(angle) * movementLimit.x * radius,
            Mathf.Sin(angle) * movementLimit.y * radius);
    }

    private void UpdateScopeAim()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        aimOffset -= mouseDelta * aimSensitivity;

        float breathOffset = Mathf.Sin(
            Time.time * breathFrequency * Mathf.PI * 2f) * breathAmplitude;

        Vector2 desiredPosition = scopeBackStartPosition + aimOffset;
        desiredPosition.y += breathOffset;

        Vector2 clampedPosition = ClampScopeBackPosition(desiredPosition);
        scopeBack.anchoredPosition = clampedPosition;

        aimOffset = clampedPosition - scopeBackStartPosition;
        aimOffset.y -= breathOffset;
    }

    private Vector2 ClampScopeBackPosition(Vector2 position)
    {
        Vector2 maxOffset = GetScopeMovementLimit();
        Vector2 offset = position - scopeBackStartPosition;
        offset.x = Mathf.Clamp(offset.x, -maxOffset.x, maxOffset.x);
        offset.y = Mathf.Clamp(offset.y, -maxOffset.y, maxOffset.y);
        return scopeBackStartPosition + offset;
    }

    private Vector2 GetScopeMovementLimit()
    {
        Vector2 backScale = new Vector2(
            Mathf.Abs(scopeBack.localScale.x),
            Mathf.Abs(scopeBack.localScale.y));
        Vector2 backSize = Vector2.Scale(scopeBack.rect.size, backScale);
        Vector2 viewportSize = viewport.rect.size;
        return new Vector2(
            Mathf.Max(0f, (backSize.x - viewportSize.x) * 0.5f),
            Mathf.Max(0f, (backSize.y - viewportSize.y) * 0.5f));
    }

    private void Fire()
    {
        currentState = StageState.Reviewing;
        CaptureHitPoint();
        ShowBulletHole();
        scopeView.SetActive(false);
        stageCoroutine = StartCoroutine(ReviewTarget());
    }

    private void CaptureHitPoint()
    {
        Vector3 crosshairWorldPosition = scopeOverlay.TransformPoint(scopeOverlay.rect.center);
        Vector3 scopeLocalPosition3D = scopeTarget.InverseTransformPoint(crosshairWorldPosition);
        Vector2 scopeLocalPosition = new Vector2(
            scopeLocalPosition3D.x,
            scopeLocalPosition3D.y);

        Rect scopeRect = scopeTarget.rect;
        Vector2 normalizedHitPoint = new Vector2(
            Mathf.InverseLerp(scopeRect.xMin, scopeRect.xMax, scopeLocalPosition.x),
            Mathf.InverseLerp(scopeRect.yMin, scopeRect.yMax, scopeLocalPosition.y));

        Rect reviewRect = reviewTarget.rect;
        hitPointOnReviewTarget = new Vector2(
            Mathf.Lerp(reviewRect.xMin, reviewRect.xMax, normalizedHitPoint.x),
            Mathf.Lerp(reviewRect.yMin, reviewRect.yMax, normalizedHitPoint.y));

        hitTarget = IsInsideCircle(scopeTarget, scopeLocalPosition);
    }

    private void ShowBulletHole()
    {
        if (!hitTarget)
        {
            if (bulletHoleMarker != null)
                bulletHoleMarker.gameObject.SetActive(false);

            return;
        }

        if (bulletHoleMarker == null)
            bulletHoleMarker = CreateBulletHoleMarker();

        bulletHoleMarker.anchoredPosition = hitPointOnReviewTarget;
        bulletHoleMarker.gameObject.SetActive(true);
        bulletHoleMarker.SetAsLastSibling();
    }

    private RectTransform CreateBulletHoleMarker()
    {
        GameObject markerObject = new GameObject(
            "BulletHole",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        markerObject.layer = reviewTarget.gameObject.layer;

        RectTransform marker = markerObject.GetComponent<RectTransform>();
        marker.SetParent(reviewTarget, false);
        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = Vector2.one * bulletHoleSize;

        Image markerImage = markerObject.GetComponent<Image>();
        Image areaImage = notRobotArea.GetComponent<Image>();
        markerImage.sprite = areaImage != null ? areaImage.sprite : null;
        markerImage.color = new Color(0.04f, 0.035f, 0.03f, 1f);
        markerImage.raycastTarget = false;

        GameObject coreObject = new GameObject(
            "Core",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        coreObject.layer = markerObject.layer;

        RectTransform core = coreObject.GetComponent<RectTransform>();
        core.SetParent(marker, false);
        core.anchorMin = new Vector2(0.5f, 0.5f);
        core.anchorMax = new Vector2(0.5f, 0.5f);
        core.pivot = new Vector2(0.5f, 0.5f);
        core.anchoredPosition = Vector2.zero;
        core.sizeDelta = Vector2.one * bulletHoleSize * 0.38f;

        Image coreImage = coreObject.GetComponent<Image>();
        coreImage.sprite = markerImage.sprite;
        coreImage.color = new Color(0.28f, 0.25f, 0.22f, 1f);
        coreImage.raycastTarget = false;

        return marker;
    }

    private IEnumerator ReviewTarget()
    {
        SetUniformScale(noZoomView, 1f);
        UpdateReviewTargetTransform(0f);

        float noZoomScale = 1f;

        while (noZoomScale < noZoomStartScale)
        {
            float progress = Mathf.InverseLerp(1f, noZoomStartScale, noZoomScale);
            float speed = Mathf.Lerp(
                reviewNoZoomStartSpeed,
                reviewNoZoomEndSpeed,
                progress);
            speed = Mathf.Max(0.01f, speed);

            noZoomScale = Mathf.MoveTowards(
                noZoomScale,
                noZoomStartScale,
                speed * Time.deltaTime);
            SetUniformScale(noZoomView, noZoomScale);

            yield return null;
        }

        float slowdownScale = Mathf.Clamp(
            reviewSlowdownScale,
            0f,
            targetStartScale);
        float targetScale = 0f;
        float fastSpeed = Mathf.Max(0.01f, reviewTargetFastSpeed);

        while (targetScale < slowdownScale)
        {
            targetScale = Mathf.MoveTowards(
                targetScale,
                slowdownScale,
                fastSpeed * Time.deltaTime);
            UpdateReviewTargetTransform(targetScale);

            yield return null;
        }

        float targetSpeed = Mathf.Max(
            0.01f,
            fastSpeed * reviewSpeedDropMultiplier);
        float endSpeed = Mathf.Max(0.01f, reviewTargetEndSpeed);

        while (targetScale < targetStartScale)
        {
            targetSpeed = Mathf.MoveTowards(
                targetSpeed,
                endSpeed,
                Mathf.Max(0f, reviewTargetDeceleration) * Time.deltaTime);
            targetScale = Mathf.MoveTowards(
                targetScale,
                targetStartScale,
                targetSpeed * Time.deltaTime);
            UpdateReviewTargetTransform(targetScale);

            yield return null;
        }

        SetUniformScale(noZoomView, noZoomStartScale);
        UpdateReviewTargetTransform(targetStartScale);
        stageCoroutine = null;

        CompleteStage(IsSuccessfulHit());
    }

    private void UpdateReviewTargetTransform(float scale)
    {
        SetUniformScale(reviewTarget, scale);

        if (!hitTarget)
        {
            reviewTarget.anchoredPosition = reviewTargetStartPosition;
            return;
        }

        reviewTarget.anchoredPosition = reviewTargetStartPosition -
                                        hitPointOnReviewTarget * scale;
    }

    private bool IsSuccessfulHit()
    {
        if (!hitTarget)
            return false;

        Vector3 hitWorldPosition = reviewTarget.TransformPoint(hitPointOnReviewTarget);
        Vector3 localPosition3D = notRobotArea.InverseTransformPoint(hitWorldPosition);
        Vector2 localPosition = new Vector2(localPosition3D.x, localPosition3D.y);
        return IsInsideCircle(notRobotArea, localPosition);
    }

    private static bool IsInsideCircle(RectTransform area, Vector2 localPoint)
    {
        Rect rect = area.rect;
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;

        if (halfWidth <= 0f || halfHeight <= 0f)
            return false;

        Vector2 offset = localPoint - rect.center;
        float normalizedX = offset.x / halfWidth;
        float normalizedY = offset.y / halfHeight;
        return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
    }

    private void CompleteStage(bool isCorrect)
    {
        currentState = StageState.Complete;
        RestoreCursorState();

        if (isCorrect)
        {
            Debug.Log("[SniperStage] 스테이지 클리어", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageClear();
        }
        else
        {
            Debug.Log("[SniperStage] 스테이지 실패", this);

            if (CGameManager.Instance != null)
                CGameManager.Instance.StageFailed();
        }
    }

    private void StopStageCoroutine()
    {
        if (stageCoroutine == null)
            return;

        StopCoroutine(stageCoroutine);
        stageCoroutine = null;
    }

    private static void SetUniformScale(RectTransform target, float scale)
    {
        target.localScale = Vector3.one * scale;
    }
}

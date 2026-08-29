using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CMoleCaptcha : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Transform tileGridParent;  // 5x5 Grid Panel
    [SerializeField] private TextMeshProUGUI scoreText; // TMP 텍스트 컴포넌트

    [Header("Mole Sprites")]
    [SerializeField] private Sprite normalMoleSprite;   // 기본 두더지 이미지
    [SerializeField] private Sprite hitMoleSprite;      // 타격당한 두더지 이미지

    [Header("Mole Settings")]
    [SerializeField] private int targetHitCount = 10;     // 목표 10회
    [SerializeField] private float popUpDuration = 1.2f;   // 기본 솟아있는 시간 (초)
    [SerializeField] private float spawnInterval = 0.5f;   // 기본 출몰 간격 (초)
    [SerializeField] private float moveSpeed = 1500f;      // 솟아오르는 속도
    [SerializeField] private float hitEffectDelay = 0.15f; // 타격 이미지 유지 시간 (초)

    [Header("Mole Position Offset")]
    [SerializeField] private float hideY = -80f; // 땅속 숨은 Y 위치
    [SerializeField] private float showY = 5f;    // 땅위 솟은 Y 위치

    private List<Button> _tileButtons = new List<Button>();
    private List<RectTransform> _moleTransforms = new List<RectTransform>();
    private List<Image> _moleImages = new List<Image>();

    private int _currentHits = 0;
    private int _activeMoleIndex = -1;

    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (tileGridParent != null)
        {
            int index = 0;
            foreach (Transform child in tileGridParent)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    int tileIndex = index;
                    btn.onClick.AddListener(() => OnTileClicked(tileIndex));
                    _tileButtons.Add(btn);

                    Transform moleTransform = child.Find("Mole");
                    if (moleTransform != null)
                    {
                        _moleTransforms.Add(moleTransform.GetComponent<RectTransform>());

                        Image moleImg = moleTransform.GetComponent<Image>();
                        if (moleImg != null)
                        {
                            _moleImages.Add(moleImg);
                        }
                    }

                    index++;
                }
            }
        }
    }

    private void OnEnable()
    {
        StartMoleCaptcha();
    }

    private void OnDisable()
    {
        CancelTask();
    }

    public void StartMoleCaptcha()
    {
        CancelTask();
        _cts = new CancellationTokenSource();

        _currentHits = 0;
        _activeMoleIndex = -1;

        UpdateDifficulty(_currentHits);
        UpdateScoreUI();
        ResetAllMoles();

        MoleSpawnLoopAsync(_cts.Token).Forget();
    }

    private void CancelTask()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void UpdateDifficulty(int hitCount)
    {
        if (hitCount >= 7)
        {
            popUpDuration = 0.6f;
            spawnInterval = 0.05f; // 0f 대신 최소 간격을 두어 Delay 멈춤 현상 방지
        }
        else if (hitCount >= 3)
        {
            popUpDuration = 0.8f;
            spawnInterval = 0.15f;
        }
        else
        {
            popUpDuration = 1.2f;
            spawnInterval = 0.5f;
        }
    }

    private async UniTask MoleSpawnLoopAsync(CancellationToken token)
    {
        try
        {
            while (_currentHits < targetHitCount)
            {
                // 이미 활성화된 두더지가 있으면 숨기기
                if (_activeMoleIndex != -1)
                {
                    int oldIndex = _activeMoleIndex;
                    _activeMoleIndex = -1;
                    AnimateMoleAsync(oldIndex, hideY, token).Forget();
                }

                // 출몰 간격 대기 (spawnInterval이 항상 0보다 크도록 보장)
                if (spawnInterval > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: token);
                }

                if (_tileButtons.Count > 0)
                {
                    _activeMoleIndex = UnityEngine.Random.Range(0, _tileButtons.Count);

                    if (_activeMoleIndex < _moleImages.Count && normalMoleSprite != null)
                    {
                        _moleImages[_activeMoleIndex].sprite = normalMoleSprite;
                    }

                    await AnimateMoleAsync(_activeMoleIndex, showY, token);
                }

                // 지정된 시간 동안 솟아오른 상태 유지 (클릭 시 _activeMoleIndex가 -1이 되어 대기 탈출)
                float timer = 0f;
                while (timer < popUpDuration && _activeMoleIndex != -1)
                {
                    timer += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 루프 정상 종료
        }
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (clickedIndex == _activeMoleIndex)
        {
            int targetIndex = _activeMoleIndex;
            _activeMoleIndex = -1; // 즉시 active 해제

            _currentHits++;
            UpdateScoreUI();
            UpdateDifficulty(_currentHits);

            HitAndHideAsync(targetIndex, this.GetCancellationTokenOnDestroy()).Forget();

            if (_currentHits >= targetHitCount)
            {
                CancelTask(); // 성공 시 스폰 루프 취소

                Debug.Log("[MoleCaptcha] 성공!");
                if (CGameManager.Instance != null)
                {
                    CGameManager.Instance.StageClear();
                }
            }
        }
    }

    private async UniTask HitAndHideAsync(int index, CancellationToken token)
    {
        if (index >= 0 && index < _moleImages.Count && hitMoleSprite != null)
        {
            _moleImages[index].sprite = hitMoleSprite;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(hitEffectDelay), cancellationToken: token);

        await AnimateMoleAsync(index, hideY, token);

        if (index >= 0 && index < _moleImages.Count && normalMoleSprite != null)
        {
            _moleImages[index].sprite = normalMoleSprite;
        }
    }

    private async UniTask AnimateMoleAsync(int index, float targetY, CancellationToken token)
    {
        if (index < 0 || index >= _moleTransforms.Count) return;

        RectTransform mole = _moleTransforms[index];
        mole.gameObject.SetActive(true);

        Vector2 targetPos = new Vector2(mole.anchoredPosition.x, targetY);

        while (Vector2.Distance(mole.anchoredPosition, targetPos) > 0.1f)
        {
            mole.anchoredPosition = Vector2.MoveTowards(mole.anchoredPosition, targetPos, moveSpeed * Time.deltaTime);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        mole.anchoredPosition = targetPos;

        if (Mathf.Approximately(targetY, hideY))
        {
            mole.gameObject.SetActive(false);
        }
    }

    private void ResetAllMoles()
    {
        for (int i = 0; i < _moleTransforms.Count; i++)
        {
            if (_moleTransforms[i] != null)
            {
                _moleTransforms[i].anchoredPosition = new Vector2(_moleTransforms[i].anchoredPosition.x, hideY);
                _moleTransforms[i].gameObject.SetActive(false);

                if (i < _moleImages.Count && normalMoleSprite != null)
                {
                    _moleImages[i].sprite = normalMoleSprite;
                }
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"두더지를 잡으세요! {_currentHits}/{targetHitCount}";
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CMoleCaptcha : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Transform tileGridParent;  // 5x5 Grid Panel
    [SerializeField] private TextMeshProUGUI scoreText; // 팀원이 만든 TMP 텍스트 컴포넌트

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
    private bool _isGameEnded = false;

    private Coroutine _moleSpawnRoutine;

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
        _isGameEnded = true;
        StopAllCoroutines();
    }

    public void StartMoleCaptcha()
    {
        _currentHits = 0;
        _activeMoleIndex = -1;
        _isGameEnded = false;

        UpdateDifficulty(_currentHits);
        UpdateScoreUI();
        ResetAllMoles();

        if (_moleSpawnRoutine != null) StopCoroutine(_moleSpawnRoutine);
        _moleSpawnRoutine = StartCoroutine(MoleSpawnRoutine());
    }

    private void UpdateDifficulty(int hitCount)
    {
        if (hitCount >= 7)
        {
            popUpDuration = 0.5f;
            spawnInterval = 0.05f; // 0초 대기 대신 아주 짧은 프레임 대기 부여 (무한루프 방지)
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

    private IEnumerator MoleSpawnRoutine()
    {
        while (_currentHits < targetHitCount && !_isGameEnded)
        {
            // 기존 두더지 강제 숨기기
            if (_activeMoleIndex != -1)
            {
                int oldIndex = _activeMoleIndex;
                _activeMoleIndex = -1;
                StartCoroutine(AnimateMole(oldIndex, hideY));
            }

            // 출몰 간격 대기
            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }

            if (_isGameEnded) yield break;

            // 랜덤 타일 선택 (이전 타일과 중복 방지 조건 포함)
            if (_tileButtons.Count > 0)
            {
                _activeMoleIndex = Random.Range(0, _tileButtons.Count);

                if (_activeMoleIndex < _moleImages.Count && normalMoleSprite != null)
                {
                    _moleImages[_activeMoleIndex].sprite = normalMoleSprite;
                }

                yield return StartCoroutine(AnimateMole(_activeMoleIndex, showY));
            }

            // 두더지가 나와서 머무는 시간 대기
            float timer = 0f;
            while (timer < popUpDuration && _activeMoleIndex != -1 && !_isGameEnded)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (_isGameEnded) return;

        // 현재 등장한 두더지를 클릭한 경우
        if (clickedIndex == _activeMoleIndex)
        {
            int targetIndex = _activeMoleIndex;
            _activeMoleIndex = -1; // 즉시 active 해제하여 중복 클릭 방지

            _currentHits++;
            UpdateScoreUI();
            UpdateDifficulty(_currentHits);

            // 타격 효과 및 들어가기 코루틴 실행
            StartCoroutine(HitAndHideRoutine(targetIndex));

            // 목표 달성 확인
            if (_currentHits >= targetHitCount)
            {
                _isGameEnded = true;

                if (_moleSpawnRoutine != null)
                {
                    StopCoroutine(_moleSpawnRoutine);
                }

                Debug.Log("[MoleCaptcha] 성공!");
                if (CGameManager.Instance != null)
                {
                    CGameManager.Instance.StageClear();
                }
            }
        }
    }

    private IEnumerator HitAndHideRoutine(int index)
    {
        if (index >= 0 && index < _moleImages.Count && hitMoleSprite != null)
        {
            _moleImages[index].sprite = hitMoleSprite;
        }

        yield return new WaitForSeconds(hitEffectDelay);

        yield return StartCoroutine(AnimateMole(index, hideY));

        if (index >= 0 && index < _moleImages.Count && normalMoleSprite != null)
        {
            _moleImages[index].sprite = normalMoleSprite;
        }
    }

    private IEnumerator AnimateMole(int index, float targetY)
    {
        if (index < 0 || index >= _moleTransforms.Count) yield break;

        RectTransform mole = _moleTransforms[index];
        mole.gameObject.SetActive(true);

        Vector2 targetPos = new Vector2(mole.anchoredPosition.x, targetY);

        while (Vector2.Distance(mole.anchoredPosition, targetPos) > 0.1f)
        {
            mole.anchoredPosition = Vector2.MoveTowards(mole.anchoredPosition, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
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
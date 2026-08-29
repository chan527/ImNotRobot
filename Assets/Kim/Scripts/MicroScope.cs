using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEditor;
using System.Collections;

public class Magnifier_Ctrl : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button magnifier_Btn;
    [SerializeField] private RectTransform magnifier_Rect;

    [Header("확대할 Text")]
    [SerializeField] private TMP_Text[] targetTexts;

    [Header("설정")]
    [SerializeField] private float detectDistance = 100f;
    [SerializeField] private float magnification = 1.5f;

    //[SerializeField] Image[] images;
    [SerializeField] TMP_Text input_text;
    [SerializeField] TMP_Text warning_text;

    int life = 3;

    string password = "0830";

    private bool isMagnifierActive = false;


    // 좌표 → 해당 위치의 Text
    private Dictionary<Vector2, TMP_Text> textDictionary
        = new Dictionary<Vector2, TMP_Text>();

    // Text → 원래 크기
    private Dictionary<TMP_Text, Vector3> originalScaleDictionary
        = new Dictionary<TMP_Text, Vector3>();

    private void OnEnable()
    {
        life = 3;
        inputed = null;
        warning_text.gameObject.SetActive(false);
    }
    private void Start()
    {
        magnifier_Rect.gameObject.SetActive(false);

        // Text 정보 저장
        SaveTextData();

        magnifier_Btn.onClick.AddListener(ToggleMagnifier);

        // 돋보기 이미지가 뒤쪽 UI 클릭을 막지 않도록
        Image image = magnifier_Rect.GetComponent<Image>();

        if (image != null)
        {
            image.raycastTarget = false;
        }
    }


    private void Update()
    {
        if (!isMagnifierActive)
            return;

        FollowMouse();

        CheckTextDistance();
    }


    // ─────────────────────────────
    // Text 좌표 / 크기 저장
    // ─────────────────────────────

    private void SaveTextData()
    {
        textDictionary.Clear();
        originalScaleDictionary.Clear();

        for (int i = 0; i < targetTexts.Length; i++)
        {
            TMP_Text text = targetTexts[i];

            if (text == null)
                continue;


            // Text 위치를 Canvas 기준 좌표로 변환
            Vector2 position =
                GetCanvasLocalPosition(text.rectTransform);


            // 좌표 → Text
            textDictionary.Add(position, text);


            // Text → 원래 Scale
            originalScaleDictionary.Add(
                text,
                text.rectTransform.localScale
            );
        }
    }


    // ─────────────────────────────
    // 돋보기 ON / OFF
    // ─────────────────────────────

    private void ToggleMagnifier()
    {
        isMagnifierActive = !isMagnifierActive;

        magnifier_Rect.gameObject.SetActive(
            isMagnifierActive
        );


        // 돋보기를 끄면 모든 Text 복구
        if (!isMagnifierActive)
        {
            RestoreAllTexts();
        }
    }


    // ─────────────────────────────
    // 마우스 따라 이동
    // ─────────────────────────────

    private void FollowMouse()
    {
        if (Mouse.current == null)
            return;


        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        Camera uiCamera = null;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }


        RectTransform parentRect =
            magnifier_Rect.parent as RectTransform;


        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            mousePosition,
            uiCamera,
            out Vector2 localPoint))
        {
            magnifier_Rect.anchoredPosition = localPoint;
        }
    }


    // ─────────────────────────────
    // 돋보기와 Text 거리 검사
    // ─────────────────────────────

    private void CheckTextDistance()
    {
        // 현재 돋보기 위치
        Vector2 magnifierPosition =
            GetCanvasLocalPosition(magnifier_Rect);


        foreach (var data in textDictionary)
        {
            Vector2 textPosition = data.Key;
            TMP_Text text = data.Value;


            float distance =
                Vector2.Distance(
                    magnifierPosition,
                    textPosition
                );


            // 가까움
            if (distance <= detectDistance)
            {
                MagnifyText(text);
            }

            // 멀어짐
            else
            {
                RestoreText(text);
            }
        }
    }


    // ─────────────────────────────
    // Text 확대
    // ─────────────────────────────

    private void MagnifyText(TMP_Text text)
    {
        if (!originalScaleDictionary.ContainsKey(text))
            return;


        text.rectTransform.localScale =
            originalScaleDictionary[text]
            * magnification;
    }


    // ─────────────────────────────
    // Text 원래 크기
    // ─────────────────────────────

    private void RestoreText(TMP_Text text)
    {
        if (!originalScaleDictionary.ContainsKey(text))
            return;


        text.rectTransform.localScale =
            originalScaleDictionary[text];
    }


    private void RestoreAllTexts()
    {
        foreach (var data in originalScaleDictionary)
        {
            TMP_Text text = data.Key;
            Vector3 originalScale = data.Value;

            text.rectTransform.localScale =
                originalScale;
        }
    }


    // ─────────────────────────────
    // UI 위치를 Canvas 기준 좌표로 변환
    // ─────────────────────────────

    private Vector2 GetCanvasLocalPosition(
        RectTransform target)
    {
        RectTransform canvasRect =
            canvas.transform as RectTransform;


        Camera uiCamera = null;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }


        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                target.position
            );


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            uiCamera,
            out Vector2 localPosition
        );


        return localPosition;
    }

    string inputed;
    public void InputPWD(int pwd)
    {
        //Debug.Log("Clicked");
        inputed += pwd.ToString();
        //Debug.Log(inputed);
        input_text.text = inputed;
        if (inputed.Length >= 4)
        {
            if (password == inputed)
            {
                CGameManager.Instance.StageClear();
            }
            else
            {
                inputed = null;
                //life--;
                //images[life].gameObject.SetActive(false);

                //if(life <= 0)
                //   CGameManager.Instance.StageFailed();
                warning_text.gameObject.SetActive(true);
   
                StartCoroutine(DeleteNumber());
            }
        }
        

    }

    IEnumerator DeleteNumber()
    {
        yield return new WaitForSeconds(2.5f);

        warning_text.gameObject.SetActive(false);
        input_text.text = inputed;
    }
}
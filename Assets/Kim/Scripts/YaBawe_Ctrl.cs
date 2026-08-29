using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class YaBawe_Ctrl : MonoBehaviour
{
    [SerializeField] private Button yes_Btn;
    [SerializeField] private Button[] no_Btns;
    [SerializeField] private RawImage action_BG;
    [SerializeField] private GameObject[] no_Object;
    [SerializeField] private RectTransform[] no_ObjRect;
    [SerializeField] TextMeshProUGUI[] yN_Text;
    [SerializeField] TextMeshProUGUI coin_Text;

    int coin = 3;

    #region 유니티 라이프사이클
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        yes_Btn.onClick.AddListener(() =>
        {
            //성공
        });

        for (int i = 0; i < no_Btns.Length; i++)
        {
            int j = i;
            no_Btns[i].onClick.AddListener(() =>
            {
                //실패
                Debug.Log(j);
                StartCoroutine(CupClick(j));
            });
        }
    }

    private void OnEnable()
    {
        coin = 3;
        coin_Text.text = "Coin : " + coin.ToString();
        DiceReady();
        StartCoroutine(GameStart());
        ReadyToPlay();
    }
    #endregion
    void DiceReady()
    {
        int rand = Random.Range(0, 3);

        yN_Text[rand].text = "Y";
    }
    IEnumerator CupClick(int num)
    {
        no_Btns[num].gameObject.SetActive(false);
        coin--;
        coin_Text.text = "Coin : " + coin.ToString();

        if(coin <= 0)
        {
            //Game Over
            yield break;
        }

        for (int i = 0; i < no_Btns.Length; i++)
        {
            no_Btns[i].interactable = false;
        }
        yield return new WaitForSeconds(0.5f);
        DiceReady();
        for(int i = 0; i < no_Btns.Length; i++)
        {
            no_Btns[i].gameObject.SetActive(false);
        }
    }
    void ReadyToPlay()
    {
        for(int i =0; i < no_Btns.Length; i++)
        {
            no_Btns[i].gameObject.SetActive(!no_Btns[i].gameObject.activeSelf);
        }
    }

    private IEnumerator GameStart()
    {

        yield return new WaitForSecondsRealtime(2.5f);

        ReadyToPlay();

        for(int i =0; i < yN_Text.Length; i++)
        {
            yN_Text[i].text = "N";
        }
        int rand = Random.Range(5, 11);

        for (int i = 0; i < rand; i++)
        {
            yield return StartCoroutine(SwapUI(rand/15f));
        }
    }

    private IEnumerator SwapUI(float duration = 0.5f)
    {
        int rand1 = Random.Range(0, 3);
        int rand2 = Random.Range(0, 3);

        while (rand1 == rand2)
            rand2 = Random.Range(0, 3);

        RectTransform uiA = no_ObjRect[rand1];
        RectTransform uiB = no_ObjRect[rand2];

        Vector2 startA = uiA.anchoredPosition;
        Vector2 startB = uiB.anchoredPosition;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            Vector2 offset = new Vector2(0, 100);

            uiA.anchoredPosition =
                Vector2.Lerp(startA, startB, t)
                + offset * Mathf.Sin(t * Mathf.PI);

            uiB.anchoredPosition =
                Vector2.Lerp(startB, startA, t)
                - offset * Mathf.Sin(t * Mathf.PI);

            yield return null;
        }

        uiA.anchoredPosition = startB;
        uiB.anchoredPosition = startA;
    }
}
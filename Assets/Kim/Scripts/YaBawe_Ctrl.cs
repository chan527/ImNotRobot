using JetBrains.Annotations;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class YaBawe_Ctrl : MonoBehaviour
{
    [SerializeField] Button yes_Btn;
    [SerializeField] Button[] no_Btns;
    [SerializeField] RawImage action_BG;
    [SerializeField] GameObject[] no_Object;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        yes_Btn.onClick.AddListener(() =>
        {
            //성공
        });

        for (int i = 0; i < no_Btns.Length; i++)
        {
            no_Btns[i].onClick.AddListener(() =>
            {
                //실패
            });
        }



    }

    private void OnEnable()
    {
        
    }

    IEnumerator GameStart()
    {
        yield return new WaitForSecondsRealtime(2.5f);

        for (int i = 0; i < no_Btns.Length; i++)
        {
            no_Btns[i].gameObject.SetActive(true);
        }


        for (int i = 0; i < 10; i++)
        {
            int rand1 = Random.Range(0, 2);
            int rand2 = Random.Range(0, 2);

            while (rand1 == rand2)
                rand2 = Random.Range(0, 2);

            Vector3 pos1 = no_Object[rand1].transform.position;
            Vector3 pos2 = no_Object[rand2].transform.position;

            
        }
        

        
    }
}

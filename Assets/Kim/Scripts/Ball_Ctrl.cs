using UnityEngine;
using TMPro;
public class Ball_Ctrl : MonoBehaviour
{
    Vector3 startPos;

    int life = 5;

    [SerializeField] TextMeshProUGUI life_Text;
    private void Start()
    {
        startPos = this.transform.position;
        life_Text.text = "Life" + life.ToString();
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if(collision.gameObject.tag == "maze")
    //    {
    //        life--;
    //        life_Text.text = "Life : " + life.ToString();
    //        GotoStartPos();
            
    //        if(life <= 0 )
    //        {
    //            //ÆÐ¹è;
    //        }
    //    }

    //    if(collision.gameObject.tag == "Goal")
    //    {
    //        Debug.Log("Goal");
    //    }
    //}

    public void GotoStartPos()
    {
        this.transform.position = startPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Goal"))
        {
            CGameManager.Instance.StageClear();
        }

    }
}

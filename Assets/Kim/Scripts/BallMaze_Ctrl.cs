using UnityEngine;
using UnityEngine.UI;
public class BallMaze_Ctrl : MonoBehaviour
{
    [SerializeField] Slider ctrl_Slider;
    [SerializeField] GameObject maze;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        maze.transform.rotation = Quaternion.identity;
    }
}

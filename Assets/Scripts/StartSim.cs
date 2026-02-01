using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartSim : MonoBehaviour
{
	public void TaskOnClick()
    {
        Debug.Log("Test");
        SceneManager.LoadScene("City");
    }
}

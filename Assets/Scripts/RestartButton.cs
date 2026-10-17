
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    public void RestartLevel()
    {
        SceneManager.LoadScene(
            gameObject.scene.name
        );
    }
}

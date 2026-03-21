using UnityEngine;

public class UIMainMenuController : MonoBehaviour
{
    private void Start()
    {

    }

    public void StartGame()
    {
        SceneLoaderService.LoadGame();
    }

    public void QuitGame()
    {
#if (UNITY_EDITOR)
        UnityEditor.EditorApplication.isPlaying = false;
#elif (UNITY_STANDALONE) 
    Application.Quit();
#elif (UNITY_WEBGL)
    Application.OpenURL("about:blank");
#endif
    }
}
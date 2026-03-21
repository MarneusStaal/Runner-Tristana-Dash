using UnityEngine;

public class UIGameOverController : MonoBehaviour
{
    public void LoadMainMenu()
    {
        SceneLoaderService.LoadMainMenu();
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

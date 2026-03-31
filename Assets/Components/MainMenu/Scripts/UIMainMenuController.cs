using TMPro;
using UnityEngine;

public class UIMainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private TMP_Text _livesText;

    private void Start()
    {
        SaveData save = SaveService.Load();

        _bestScoreText.text = $"Best Score : {save.BestScore}";
        _livesText.text = $"Lives : {save.Lives}";
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
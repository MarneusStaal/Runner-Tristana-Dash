using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoaderService
{
    public static void LoadGame()
    {
        SceneManager.LoadScene("Level", LoadSceneMode.Single);
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    public static void LoadGameOver()
    {
        SceneManager.LoadScene("GameOver", LoadSceneMode.Additive);
    }
}

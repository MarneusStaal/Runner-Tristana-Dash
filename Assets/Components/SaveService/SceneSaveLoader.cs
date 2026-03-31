using UnityEngine;

public class SceneSaveLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SaveData save = SaveService.Load();
        RunnerEventSystem.OnSaveLoaded?.Invoke(save);
    }
}

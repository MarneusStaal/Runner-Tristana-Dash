using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================

    [SerializeField] private Slider _flyBar;

    public void UpdateFlyBar(int flyUnit)
    {
        _flyBar.value = flyUnit * 0.1f;
    }
}

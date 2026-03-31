using TMPro;
using UnityEngine;

public class UIResourceController : MonoBehaviour
{
    [SerializeField] private TMP_Text _redBottleText;
    [SerializeField] private TMP_Text _greenBottleText;
    [SerializeField] private TMP_Text _blueBottleText;
    [SerializeField] private TMP_Text _candleText;

    private void Awake()
    {
        //RunnerEventSystem.OnCollectablePickUp += HandleCollectablePickUp;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        //RunnerEventSystem.OnCollectablePickUp -= HandleCollectablePickUp;
    }
}

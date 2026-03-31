using UnityEngine;

public class CollectableSpawnController : MonoBehaviour
{
    [SerializeField] private GameObject _redBottle;
    [SerializeField] private GameObject _greenBottle;
    [SerializeField] private GameObject _blueBottle;
    [SerializeField] private GameObject _candle;
    [SerializeField] private GameObject _fuel;

    [Header("Spawn Chances")]
    [SerializeField] private int _resourceSpawnChance = 20;
    [SerializeField] private int _fuelSpawnChance = 50;
    [SerializeField] private int _redBottleSpawnChance = 70;
    [SerializeField] private int _greenBottleSpawnChance = 50;
    [SerializeField] private int _blueBottleSpawnChance = 20;
    [SerializeField] private int _candleSpawnChance = 10;

    private void Start()
    {
        int randomNumber = Random.Range(0, 100);

        if (randomNumber > _fuelSpawnChance)
        {
            return;
        }

        if (randomNumber > _resourceSpawnChance)
        {
            InitCollectable(_fuel);
            return;
        }

        randomNumber = Random.Range(0, 100);

        if (randomNumber <= _candleSpawnChance) InitCollectable(_candle);
        else if (randomNumber <= _blueBottleSpawnChance) InitCollectable(_blueBottle);
        else if (randomNumber <= _greenBottleSpawnChance) InitCollectable(_greenBottle);
        else if (randomNumber <= _redBottleSpawnChance) InitCollectable(_redBottle);
    }

    private void InitCollectable(GameObject collectable)
    {
        GameObject temp = Instantiate(collectable, transform.position, Quaternion.identity);
        temp.transform.parent = transform;
    }
}

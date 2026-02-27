using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [SerializeField] private int _maxFuel = 10;
    [SerializeField] private int _fuel;
    public int Fuel
    {  get
       {
            return _fuel;
       }
       set
       {
            if (value >= 0 &&  value <= _maxFuel) _fuel = value;
       }
    }
}

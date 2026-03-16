using UnityEngine;

public abstract class BaseObstacle : MonoBehaviour, IObstacle
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Start()
    {
        RandomDisapearance();
    }

    private void RandomDisapearance()
    {
        //int randomNumber = Random.Range(0, 100);
        //
        //if (randomNumber > 70)
        //{
        //    Destroy(gameObject);
        //}
    }
    public virtual void ObstacleEffect(bool isFlying)
    {
        throw new System.NotImplementedException();
    }
}

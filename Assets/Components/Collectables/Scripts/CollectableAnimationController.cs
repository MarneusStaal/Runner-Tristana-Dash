using UnityEngine;

public class CollectableAnimationController : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 5f;

    void Update()
    {
        Vector3 rotation = transform.rotation.eulerAngles;
        rotation += new Vector3(0, Time.deltaTime * _rotationSpeed, 0);

        rotation.y = rotation.y >= 360 ? 0 : rotation.y;

        transform.rotation = Quaternion.Euler(rotation);
    }
}

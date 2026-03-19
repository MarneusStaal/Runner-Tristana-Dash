using UnityEngine;

public class SegmentController : MonoBehaviour
{
    [SerializeField] private Transform _endOfSegment;
    public Transform EndOfSegment => _endOfSegment;
}

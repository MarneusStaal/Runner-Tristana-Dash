using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class PineParallaxController : MonoBehaviour
{
    [SerializeField] private SegmentController _pines;
    [SerializeField] private float _speed;
    [SerializeField] private Transform _leftLane;
    [SerializeField] private Transform _rightLane;
    [SerializeField] private int _segmentNumbers = 7;

    [Header("Destory Segment")]
    [SerializeField] float _destroyPositionZ = -40f;
    private SegmentController _toRemoveSegment;

    private List<SegmentController> _instanciatedLeftSegments = new List<SegmentController>();
    private List<SegmentController> _instanciatedRightSegments = new List<SegmentController>();

    void Start()
    {
        GenerateBaseSegments();
    }

    void Update()
    {
        MoveTiles(_instanciatedLeftSegments);
        MoveTiles(_instanciatedRightSegments);
    }

    private void MoveTiles(List<SegmentController> segments)
    {
        SegmentController segment = null;

        for (int i = 0; i < segments.Count; i++)
        {
            segment = segments[i];

            segment.transform.position += Vector3.back * Time.deltaTime * _speed;

            if (segment.transform.position.z < _destroyPositionZ)
            {
                _toRemoveSegment = segment;
                AddNewSegment(segments);
            }
        }

        if (_toRemoveSegment != null)
        {
            segments.Remove(_toRemoveSegment);
            Destroy(_toRemoveSegment.gameObject);

            _toRemoveSegment = null;
        }
    }

    private void GenerateBaseSegments()
    {
        SegmentController segment = Instantiate(_pines, new Vector3(_leftLane.position.x, 0, -10), Quaternion.identity);
        segment.transform.GetChild(0).transform.rotation = RandomRotation();
        _instanciatedLeftSegments.Add(segment);

        segment = Instantiate(_pines, new Vector3(_rightLane.position.x, 0, -10), Quaternion.identity);
        segment.transform.GetChild(0).transform.rotation = RandomRotation();
        _instanciatedRightSegments.Add(segment);

        for (int i = 1; i < _segmentNumbers; i++)
        {
            AddNewSegment(_instanciatedLeftSegments);
            AddNewSegment(_instanciatedRightSegments);
        }
    }

    private Quaternion RandomRotation()
    {
        int random = (int)Random.Range(0, 4);
        Quaternion rotation = Quaternion.identity;

        switch (random)
        {
            case 0: rotation = Quaternion.identity; break;
            case 1: rotation = Quaternion.Euler(new Vector3(0, 90, 0)); break;
            case 2: rotation = Quaternion.Euler(new Vector3(0, -90, 0)); break;
            case 3: rotation = Quaternion.Euler(new Vector3(0, 180, 0)); break;
        }

        return rotation;
    }

    private void AddNewSegment(List<SegmentController> segments)
    {
        SegmentController segment = Instantiate(_pines, LastSegment(segments).EndOfSegment.position, Quaternion.identity);
        segment.transform.GetChild(0).transform.rotation = RandomRotation();
        segments.Add(segment);
    }

    private SegmentController LastSegment(List<SegmentController> segments)
    {
        return segments[segments.Count - 1];
    }
}

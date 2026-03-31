using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

// Creates a parallax pine tree effect on both sides of the track by scrolling two independent
// chains of segments (left lane and right lane) at a fixed speed. Segments are recycled
// as they pass off-screen, producing an endless background loop.
// Each segment's child mesh is given a random Y rotation on spawn to add visual variety.
public class PineParallaxController : MonoBehaviour
{
    // The pine segment prefab instantiated for both the left and right lanes
    [SerializeField] private SegmentController _pines;
    // Scroll speed of the pine segments in the -Z direction (should match or offset the game speed for parallax effect)
    [SerializeField] private float _speed;
    // World-space anchors that define the X position for each lane's segment chain
    [SerializeField] private Transform _leftLane;
    [SerializeField] private Transform _rightLane;
    // Total number of segments kept alive per lane at any one time
    [SerializeField] private int _segmentNumbers = 7;

    [Header("Destory Segment")]
    // Z position threshold below which a segment is considered off-screen and recycled
    [SerializeField] float _destroyPositionZ = -40f;
    // Holds the segment flagged for removal this frame; shared across both lane calls in Update
    private SegmentController _toRemoveSegment;

    // Independent segment chains for the left and right sides of the track
    private List<SegmentController> _instanciatedLeftSegments = new List<SegmentController>();
    private List<SegmentController> _instanciatedRightSegments = new List<SegmentController>();

    void Start()
    {
        GenerateBaseSegments();
    }

    void Update()
    {
        // Scroll and recycle both lanes independently each frame
        MoveTiles(_instanciatedLeftSegments);
        MoveTiles(_instanciatedRightSegments);
    }

    // Scrolls every segment in the given lane towards the camera and recycles any
    // that pass the destroy threshold. Removal is deferred outside the loop to avoid
    // modifying the list while it is being iterated.
    private void MoveTiles(List<SegmentController> segments)
    {
        SegmentController segment = null;

        for (int i = 0; i < segments.Count; i++)
        {
            segment = segments[i];

            segment.transform.position += Vector3.back * Time.deltaTime * _speed;

            // Flag the segment for removal and immediately spawn a replacement at the chain's end
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

    // Spawns the first segment for each lane at a fixed starting Z offset, then fills
    // the remainder of each chain end-to-end using AddNewSegment
    private void GenerateBaseSegments()
    {
        // Spawn the first left-lane segment; Z = -10 offsets it slightly behind the start line
        SegmentController segment = Instantiate(_pines, new Vector3(_leftLane.position.x, 0, -10), Quaternion.identity);
        segment.transform.GetChild(0).transform.rotation = RandomRotation(); // randomise tree facing
        _instanciatedLeftSegments.Add(segment);

        // Spawn the first right-lane segment at the mirrored X position
        segment = Instantiate(_pines, new Vector3(_rightLane.position.x, 0, -10), Quaternion.identity);
        segment.transform.GetChild(0).transform.rotation = RandomRotation();
        _instanciatedRightSegments.Add(segment);

        // Fill both chains starting at index 1 (index 0 is the manually placed first segment)
        for (int i = 1; i < _segmentNumbers; i++)
        {
            AddNewSegment(_instanciatedLeftSegments);
            AddNewSegment(_instanciatedRightSegments);
        }
    }

    // Returns one of four 90° Y-axis rotations at random, giving each pine tree
    // a different facing direction to break up visual repetition
    private Quaternion RandomRotation()
    {
        int random = (int)Random.Range(0, 4);
        Quaternion rotation = Quaternion.identity;

        switch (random)
        {
            case 0: rotation = Quaternion.identity; break; //   0°
            case 1: rotation = Quaternion.Euler(new Vector3(0, 90, 0)); break; //  90°
            case 2: rotation = Quaternion.Euler(new Vector3(0, -90, 0)); break; // -90°
            case 3: rotation = Quaternion.Euler(new Vector3(0, 180, 0)); break; // 180°
        }

        return rotation;
    }

    // Spawns a new pine segment flush against the end of the current last segment in the given lane,
    // then applies a random rotation to the child mesh for visual variety
    private void AddNewSegment(List<SegmentController> segments)
    {
        // Place the new segment at the EndOfSegment anchor of the last segment to keep the chain seamless
        SegmentController segment = Instantiate(_pines, LastSegment(segments).EndOfSegment.position, Quaternion.identity);
        segment.transform.GetChild(0).transform.rotation = RandomRotation();
        segments.Add(segment);
    }

    // Returns the most recently added segment in the given lane, used as the attachment point for the next one
    private SegmentController LastSegment(List<SegmentController> segments)
    {
        return segments[segments.Count - 1];
    }
}
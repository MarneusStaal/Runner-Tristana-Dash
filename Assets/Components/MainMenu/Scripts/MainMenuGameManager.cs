using System.Collections.Generic;
using UnityEngine;

// Drives the main menu background scene: spawns an endless stream of level segments
// that scroll towards the camera, recycling them as they pass out of view.
public class MainMenuGameManager : MonoBehaviour
{
    // ==========================================

    [Header("Segments")]
    // The fixed first segment, always spawned at the origin to open the sequence
    [SerializeField] private SegmentController _initialSegment;
    // Pool of segment prefabs to randomly draw from when adding new segments; in this case contains only one emty segment
    [SerializeField] private SegmentController[] _segmentPool;
    // Total number of segments to keep alive at any one time (initial + subsequent)
    [SerializeField] private int _segmentNumbers;

    [Header("Movement Speed")]
    // Units per second at which all segments scroll in the -Z direction
    [SerializeField] private float _speed = 0f;

    [Header("Destory Segment")]
    // Z position threshold below which a segment is considered off-screen and destroyed
    [SerializeField] float _destroyPositionZ = -40f;

    [Header("Debug")]
    // Live list of all currently active segments; visible in the Inspector for debugging
    [SerializeField] private List<SegmentController> _instanciatedSegments = new List<SegmentController>();
    // Holds a reference to the segment flagged for removal this frame, processed after the loop
    private SegmentController _toRemoveSegment;

    // ==========================================

    private void Start()
    {
        Init();
    }

    // Entry point for scene setup; separated from Start to allow future pre-init steps
    private void Init()
    {
        GenerateBaseSegments();
    }

    private void Update()
    {
        SegmentController segment = null;

        // Scroll every active segment towards the camera and check if any have gone off-screen
        for (int i = 0; i < _instanciatedSegments.Count; i++)
        {
            segment = _instanciatedSegments[i];

            // Move the segment in the -Z direction at the configured scroll speed
            segment.transform.position += Vector3.back * Time.deltaTime * _speed;

            // If the segment has passed the destroy threshold, flag it and immediately
            // spawn a replacement at the end of the current chain
            if (segment.transform.position.z < _destroyPositionZ)
            {
                _toRemoveSegment = segment;
                AddNewSegment();
            }
        }

        // Remove and destroy the flagged segment outside the loop to avoid modifying
        // the list while it is being iterated
        if (_toRemoveSegment != null)
        {
            _instanciatedSegments.Remove(_toRemoveSegment);
            Destroy(_toRemoveSegment.gameObject);

            _toRemoveSegment = null;
        }
    }

    // Spawns the initial segment at the origin then fills the rest of the chain
    // with randomly selected segments placed end-to-end
    private void GenerateBaseSegments()
    {
        // Always start with the designated initial segment at world origin
        SegmentController segment = Instantiate(_initialSegment, Vector3.zero, Quaternion.identity);
        _instanciatedSegments.Add(segment);

        // Fill the remaining slots starting at index 1 (the initial segment occupies index 0)
        for (int i = 1; i < _segmentNumbers; i++)
        {
            AddNewSegment();
        }
    }

    // Picks a random segment from the pool and places it flush against the end of the last segment
    private void AddNewSegment()
    {
        int random = 0;

        random = Random.Range(0, _segmentPool.Length);

        // Spawn at the EndOfSegment anchor of the current last segment to keep the chain seamless
        SegmentController segment = Instantiate(_segmentPool[random], LastSegment().EndOfSegment.position, Quaternion.identity);
        _instanciatedSegments.Add(segment);
    }

    // Returns the most recently added segment, used as the attachment point for the next one
    private SegmentController LastSegment()
    {
        return _instanciatedSegments[_instanciatedSegments.Count - 1];
    }
}
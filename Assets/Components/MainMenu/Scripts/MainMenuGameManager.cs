using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MainMenuGameManager : MonoBehaviour
{
    public static MainMenuGameManager Instance {  get; private set; }

    // ==========================================

    [Header("Segments")]
    [SerializeField] private SegmentController _initialSegment;
    [SerializeField] private SegmentController[] _segmentPool;
    [SerializeField] private int _segmentNumbers;

    [Header("Movement Speed")]
    [SerializeField] private float _speed = 0f;

    [Header("Destory Segment")]
    [SerializeField] float _destroyPositionZ = -40f;

    [Header("Debug")]
    [SerializeField] private List<SegmentController> _instanciatedSegments = new List<SegmentController>();
    private SegmentController _toRemoveSegment;

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

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        GenerateBaseSegments();
        RunnerEventSystem.OnSpeedStateChange?.Invoke(SpeedState.Run);
        RunnerEventSystem.OnStartRunning?.Invoke();
    }

    private void Update()
    {
        SegmentController segment = null;

        for (int i = 0; i < _instanciatedSegments.Count; i++)
        {
            segment = _instanciatedSegments[i];

            segment.transform.position += Vector3.back * Time.deltaTime * _speed;

            if (segment.transform.position.z < _destroyPositionZ)
            {
                _toRemoveSegment = segment;
                AddNewSegment();
            }
        }

        if (_toRemoveSegment != null)
        {
            _instanciatedSegments.Remove(_toRemoveSegment);
            Destroy(_toRemoveSegment.gameObject);

            _toRemoveSegment = null;
        }
    }

    private void GenerateBaseSegments()
    {
        SegmentController segment = Instantiate(_initialSegment, Vector3.zero, Quaternion.identity);
        _instanciatedSegments.Add(segment);

        for (int i = 1; i < _segmentNumbers; i++)
        {
            AddNewSegment();
        }
    }

    private void AddNewSegment()
    {
        int random = 0;

        random = Random.Range(0, _segmentPool.Length);

        SegmentController segment = Instantiate(_segmentPool[random], LastSegment().EndOfSegment.position, Quaternion.identity);
        _instanciatedSegments.Add(segment);
    }

    private SegmentController LastSegment()
    {
        return _instanciatedSegments[_instanciatedSegments.Count - 1];
    }
}

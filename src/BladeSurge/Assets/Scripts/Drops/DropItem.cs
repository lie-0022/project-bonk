using UnityEngine;

/// <summary>
/// 적 처치 시 바닥에 드롭되는 즉발 효과 아이템.
/// 플레이어가 _collectRadius 안에 들어오면 효과 발동 + 소멸.
/// _autoDespawnTime 경과 시 자동 소멸.
/// </summary>
public class DropItem : MonoBehaviour
{
    [SerializeField] private DropItemType _type;
    [SerializeField] private float _collectRadius = 1f;
    [SerializeField] private float _autoDespawnTime = 30f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _bobHeight = 0.15f;

    public DropItemType Type => _type;

    private Transform _player;
    private float _aliveTimer;
    private Vector3 _spawnPosition;
    private float _bobOffset;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player")?.transform;
        _aliveTimer = _autoDespawnTime;
        _spawnPosition = transform.position;
        _bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // bobbing
        float y = _spawnPosition.y + Mathf.Sin(Time.time * _bobSpeed + _bobOffset) * _bobHeight;
        transform.position = new Vector3(_spawnPosition.x, y, _spawnPosition.z);

        // collect
        if (_player != null)
        {
            float sqr = (transform.position - _player.position).sqrMagnitude;
            if (sqr <= _collectRadius * _collectRadius)
            {
                if (DropItemEffects.Instance != null) DropItemEffects.Instance.Apply(_type);
                Destroy(gameObject);
                return;
            }
        }

        // auto despawn
        _aliveTimer -= Time.deltaTime;
        if (_aliveTimer <= 0f) Destroy(gameObject);
    }
}

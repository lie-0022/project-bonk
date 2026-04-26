using UnityEngine;

/// <summary>
/// XP 오브 동작. 플레이어가 attractRadius 안에 들어오면 날아가서 수집된다.
/// </summary>
public class XPOrb : MonoBehaviour, IPoolable
{
    [SerializeField] private float _attractRadius = 5f;
    [SerializeField] private float _flySpeed = 8f;
    [SerializeField] private float _collectRadius = 0.5f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _bobHeight = 0.15f;

    private float _xpAmount;
    private Transform _playerTransform;
    private bool _isAttracting;
    private float _bobOffset;
    private Vector3 _spawnPosition;

    /// <summary>XPSystem이 스폰 후 호출해 XP량과 플레이어 참조를 주입한다.</summary>
    public void Setup(float xpAmount, Transform playerTransform)
    {
        _xpAmount = xpAmount;
        _playerTransform = playerTransform;
        _spawnPosition = transform.position;
        _bobOffset = Random.Range(0f, Mathf.PI * 2f); // 오브마다 다른 페이즈
    }

    public void OnSpawn()
    {
        _isAttracting = false;
        _xpAmount = 0f;
        _playerTransform = null;
    }

    public void OnDespawn() { }

    private void Update()
    {
        if (_playerTransform == null) return;

        if (_isAttracting)
        {
            // 플레이어 방향으로 이동
            transform.position = Vector3.MoveTowards(
                transform.position,
                _playerTransform.position,
                _flySpeed * Time.deltaTime);

            // 수집 판정
            if (Vector3.Distance(transform.position, _playerTransform.position) <= _collectRadius)
                Collect();
        }
        else
        {
            // 제자리 봅 모션
            float y = _spawnPosition.y + Mathf.Sin(Time.time * _bobSpeed + _bobOffset) * _bobHeight;
            transform.position = new Vector3(_spawnPosition.x, y, _spawnPosition.z);

            // 플레이어 거리 체크
            float sqrDist = (transform.position - _playerTransform.position).sqrMagnitude;
            if (sqrDist <= _attractRadius * _attractRadius)
                _isAttracting = true;
        }
    }

    private void Collect()
    {
        XPSystem.Instance.AddXP(_xpAmount);
        PickupPool.Instance.ReturnXPOrb(gameObject);
    }
}

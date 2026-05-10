using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 월드에 배치된 깨지는 항아리. 플레이어가 InteractRadius 안에 들어오면 활성, E 키로 파괴.
/// 파괴 시 Visual_Normal 비활성, Visual_Cracked 활성 + Gold/XP 오브 다수 스폰.
/// 가장 가까운 1개만 입력을 받도록 Chest와 동일하게 정적 캐싱·중재.
/// </summary>
public class Jar : MonoBehaviour
{
    [SerializeField] private float _interactRadius = 3.0f;
    [SerializeField] private float _breakDelay = 1.0f;

    [Header("Drop")]
    [Tooltip("골드 오브 스폰 개수.")]
    [SerializeField] private int _goldOrbs = 3;
    [Tooltip("골드 오브 1개당 골드량.")]
    [SerializeField] private int _goldPerOrb = 5;
    [Tooltip("XP 오브 스폰 개수.")]
    [SerializeField] private int _xpOrbs = 2;
    [Tooltip("XP 오브 1개당 XP량.")]
    [SerializeField] private float _xpPerOrb = 8f;
    [Tooltip("드롭 오브 스폰 시 횡방향 산포 반경(m).")]
    [SerializeField] private float _dropSpread = 0.6f;

    /// <summary>가장 가까운 항아리가 바뀔 때 발동. (jar 또는 null)</summary>
    public static event Action<Jar> OnNearestChanged;

    private static readonly List<Jar> s_active = new();
    private static Jar s_nearest;

    public bool IsPlayerInRange { get; private set; }
    public float InteractRadius => _interactRadius;

    private Transform _player;
    private bool _broken;

    private void OnEnable()
    {
        if (!s_active.Contains(this)) s_active.Add(this);
    }

    private void OnDisable()
    {
        s_active.Remove(this);
        if (s_nearest == this)
        {
            s_nearest = null;
            OnNearestChanged?.Invoke(null);
        }
    }

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_broken || _player == null) return;

        float dist = Vector3.Distance(_player.position, transform.position);
        IsPlayerInRange = dist <= _interactRadius;

        UpdateNearest();

        if (s_nearest == this && IsPlayerInRange)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
                Break();
        }
    }

    private void UpdateNearest()
    {
        Jar nearest = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < s_active.Count; i++)
        {
            var c = s_active[i];
            if (c == null || !c.IsPlayerInRange || c._broken) continue;
            float d = Vector3.Distance(c.transform.position, _player.position);
            if (d < bestDist) { bestDist = d; nearest = c; }
        }

        if (nearest != s_nearest)
        {
            s_nearest = nearest;
            OnNearestChanged?.Invoke(s_nearest);
        }
    }

    private void Break()
    {
        if (_broken) return;
        _broken = true;

        // 시각: Normal → Cracked
        var visNormal = transform.Find("Visual_Normal");
        var visCracked = transform.Find("Visual_Cracked");
        if (visNormal != null) visNormal.gameObject.SetActive(false);
        if (visCracked != null) visCracked.gameObject.SetActive(true);

        // 드롭 — Gold orbs
        var pool = PickupPool.Instance;
        var playerHealth = _player != null ? _player.GetComponent<HealthComponent>() : null;
        var playerTr = _player;
        if (pool != null)
        {
            for (int i = 0; i < _goldOrbs; i++)
            {
                var go = pool.GetGoldOrb();
                if (go == null) continue;
                go.transform.position = transform.position + RandomOffset();
                go.GetComponent<GoldOrb>()?.Setup(_goldPerOrb, playerTr);
            }
            for (int i = 0; i < _xpOrbs; i++)
            {
                var go = pool.GetXPOrb();
                if (go == null) continue;
                go.transform.position = transform.position + RandomOffset();
                go.GetComponent<XPOrb>()?.Setup(_xpPerOrb, playerTr);
            }
        }

        // nearest 통지 즉시 해제
        s_active.Remove(this);
        if (s_nearest == this)
        {
            s_nearest = null;
            OnNearestChanged?.Invoke(null);
        }

        Destroy(gameObject, _breakDelay);
    }

    private Vector3 RandomOffset()
    {
        Vector2 v = UnityEngine.Random.insideUnitCircle * _dropSpread;
        return new Vector3(v.x, 0.3f, v.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 월드에 배치된 상자. 플레이어가 InteractRadius 안에 들어오면 활성, E 키로 구매.
/// 가장 가까운 상자 1개만 입력을 받도록 ChestPrompt가 중재한다 (정적 추적).
/// 활성 상자 목록은 OnEnable/OnDisable 라이프사이클에 정적 캐싱 (FindObjectsByType 호출 회피).
/// </summary>
public class Chest : MonoBehaviour
{
    [SerializeField] private float _interactRadius = 2.0f;

    /// <summary>가장 가까운 상자가 바뀔 때 발동. (chest 또는 null, cost)</summary>
    public static event Action<Chest, int> OnNearestChanged;

    private static readonly List<Chest> s_active = new();
    private static Chest s_nearest;

    public bool IsPlayerInRange { get; private set; }
    public float InteractRadius => _interactRadius;

    private Transform _player;

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
            OnNearestChanged?.Invoke(null, 0);
        }
    }

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(_player.position, transform.position);
        IsPlayerInRange = dist <= _interactRadius;

        UpdateNearest();

        if (s_nearest == this && IsPlayerInRange)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
                TryOpen();
        }
    }

    private void UpdateNearest()
    {
        Chest nearest = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < s_active.Count; i++)
        {
            var c = s_active[i];
            if (c == null || !c.IsPlayerInRange) continue;
            float d = Vector3.Distance(c.transform.position, _player.position);
            if (d < bestDist) { bestDist = d; nearest = c; }
        }

        if (nearest != s_nearest)
        {
            s_nearest = nearest;
            int cost = ChestSystem.Instance != null ? ChestSystem.Instance.GetNextCost() : 0;
            OnNearestChanged?.Invoke(s_nearest, cost);
        }
    }

    private void TryOpen()
    {
        var system = ChestSystem.Instance;
        if (system == null) return;

        if (system.TryPurchase())
            Destroy(gameObject);
        // OnDisable이 s_active에서 제거 + nearest null 통지 처리
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}

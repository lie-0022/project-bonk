using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사망 시 handslot.l / handslot.r 의 무기를 분리하여 Rigidbody를 부여하고 떨어뜨린다.
/// 떨어진 무기는 _despawnAfter 초 후 자동 제거.
///
/// HealthComponent.OnDeath 를 구독한다.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class WeaponDropOnDeath : MonoBehaviour
{
    [Tooltip("무기를 찾을 자식 본 이름들 (handslot.r/l 등).")]
    [SerializeField] private string[] _slotNames = { "handslot.r", "handslot.l" };
    [Tooltip("떨어뜨릴 때 위쪽으로 가하는 임펄스.")]
    [SerializeField] private float _upwardImpulse = 2.5f;
    [Tooltip("랜덤 횡방향 임펄스 최대.")]
    [SerializeField] private float _horizontalImpulse = 1.5f;
    [Tooltip("회전 토크.")]
    [SerializeField] private float _torque = 5f;
    [Tooltip("떨어뜨린 무기 자동 제거 시간(초). 0 이면 영구.")]
    [SerializeField] private float _despawnAfter = 5f;

    private HealthComponent _health;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        _health.OnDeath += OnDeath;
    }

    private void OnDisable()
    {
        _health.OnDeath -= OnDeath;
    }

    private void OnDeath(float xpReward)
    {
        var weapons = new List<GameObject>();
        foreach (var slotName in _slotNames)
        {
            var slot = FindChildByName(transform, slotName);
            if (slot == null) continue;
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var w = slot.GetChild(i).gameObject;
                weapons.Add(w);
                w.transform.SetParent(null, worldPositionStays: true);
            }
        }

        foreach (var w in weapons)
        {
            var rb = w.GetComponent<Rigidbody>();
            if (rb == null) rb = w.AddComponent<Rigidbody>();
            rb.useGravity = true;

            // 무기 메시에 Collider가 없으면 단순 BoxCollider 추가 (없으면 바닥 통과)
            if (w.GetComponentInChildren<Collider>() == null)
                w.AddComponent<BoxCollider>();

            Vector3 impulse = new Vector3(
                Random.Range(-_horizontalImpulse, _horizontalImpulse),
                _upwardImpulse,
                Random.Range(-_horizontalImpulse, _horizontalImpulse));
            rb.AddForce(impulse, ForceMode.Impulse);
            rb.AddTorque(new Vector3(
                Random.Range(-_torque, _torque),
                Random.Range(-_torque, _torque),
                Random.Range(-_torque, _torque)), ForceMode.Impulse);

            if (_despawnAfter > 0f) Destroy(w, _despawnAfter);
        }
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildByName(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}

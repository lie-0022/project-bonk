using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 UI에 추적 오브젝트(MinimapTracker)들의 위치를 매 프레임 마커로 표시.
/// 카메라 렌더 없이 UI Image만 사용 — 가볍고 색상/모양 자유로움.
/// 플레이어 중심으로 표시되며 회전은 고정(북쪽 위).
/// </summary>
public class MinimapManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _markerLayer;

    [Header("Tunables")]
    [Tooltip("미니맵에 표시되는 월드 반경(미터).")]
    [SerializeField] private float _worldViewRadius = 25f;
    [Tooltip("범위 밖 마커를 가장자리에 클램프할지(true) 숨길지(false).")]
    [SerializeField] private bool _clampToEdge = false;

    [Header("Marker Style")]
    [SerializeField] private float _playerSize = 14f;
    [SerializeField] private float _enemySize  = 6f;
    [SerializeField] private float _bossSize   = 16f;
    [SerializeField] private float _chestSize  = 10f;
    [SerializeField] private float _coinSize   = 4f;

    [SerializeField] private Color _playerColor = new Color(0.4f, 0.85f, 1f);
    [SerializeField] private Color _enemyColor  = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color _bossColor   = new Color(1f, 0.1f, 0.1f);
    [SerializeField] private Color _chestColor  = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _coinColor   = new Color(1f, 0.95f, 0.4f);

    [Tooltip("마커가 사용할 단색 흰 sprite. 비우면 Default-UI 사각형 사용.")]
    [SerializeField] private Sprite _markerSprite;

    public static MinimapManager Instance { get; private set; }
    private static readonly List<MinimapTracker> s_pending = new();

    private readonly Dictionary<MinimapTracker, Image> _markers = new();
    private MinimapTracker _player;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < s_pending.Count; i++) AddInternal(s_pending[i]);
        s_pending.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Register(MinimapTracker t)
    {
        if (Instance != null) Instance.AddInternal(t);
        else s_pending.Add(t);
    }

    public static void Unregister(MinimapTracker t)
    {
        if (Instance != null) Instance.RemoveInternal(t);
        else s_pending.Remove(t);
    }

    private void AddInternal(MinimapTracker t)
    {
        if (t == null || _markers.ContainsKey(t)) return;
        if (t.Type == MinimapTracker.MarkerType.Player) _player = t;

        var go = new GameObject("Marker_" + t.Type, typeof(RectTransform));
        go.transform.SetParent(_markerLayer, false);
        var img = go.AddComponent<Image>();
        img.sprite = _markerSprite;
        img.color = ColorOf(t.Type);
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        float sz = SizeOf(t.Type);
        rt.sizeDelta = new Vector2(sz, sz);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        _markers[t] = img;
    }

    private void RemoveInternal(MinimapTracker t)
    {
        if (t == null) return;
        if (_markers.TryGetValue(t, out var img))
        {
            if (img != null) Destroy(img.gameObject);
            _markers.Remove(t);
        }
        if (_player == t) _player = null;
    }

    private void LateUpdate()
    {
        if (_markerLayer == null || _player == null) return;

        Vector2 layerSize = _markerLayer.rect.size;
        float halfX = layerSize.x * 0.5f;
        float halfY = layerSize.y * 0.5f;
        float radiusToPixels = Mathf.Min(halfX, halfY) / Mathf.Max(_worldViewRadius, 0.01f);

        Vector3 center = _player.transform.position;

        foreach (var kv in _markers)
        {
            var t = kv.Key;
            var img = kv.Value;
            if (t == null || img == null) continue;
            var rt = (RectTransform)img.transform;

            if (t == _player)
            {
                rt.anchoredPosition = Vector2.zero;
                continue;
            }

            Vector3 d = t.transform.position - center;
            Vector2 ui = new Vector2(d.x, d.z) * radiusToPixels;

            if (!_clampToEdge && (Mathf.Abs(ui.x) > halfX || Mathf.Abs(ui.y) > halfY))
            {
                if (img.enabled) img.enabled = false;
                continue;
            }
            if (!img.enabled) img.enabled = true;

            if (_clampToEdge)
            {
                ui.x = Mathf.Clamp(ui.x, -halfX, halfX);
                ui.y = Mathf.Clamp(ui.y, -halfY, halfY);
            }
            rt.anchoredPosition = ui;
        }
    }

    private float SizeOf(MinimapTracker.MarkerType t)
    {
        switch (t)
        {
            case MinimapTracker.MarkerType.Player: return _playerSize;
            case MinimapTracker.MarkerType.Boss:   return _bossSize;
            case MinimapTracker.MarkerType.Chest:  return _chestSize;
            case MinimapTracker.MarkerType.Coin:   return _coinSize;
            default: return _enemySize;
        }
    }

    private Color ColorOf(MinimapTracker.MarkerType t)
    {
        switch (t)
        {
            case MinimapTracker.MarkerType.Player: return _playerColor;
            case MinimapTracker.MarkerType.Boss:   return _bossColor;
            case MinimapTracker.MarkerType.Chest:  return _chestColor;
            case MinimapTracker.MarkerType.Coin:   return _coinColor;
            default: return _enemyColor;
        }
    }
}

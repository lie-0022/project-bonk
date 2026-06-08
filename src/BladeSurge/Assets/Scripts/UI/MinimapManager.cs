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
    [SerializeField] private float _jarSize    = 6f;

    [SerializeField] private Color _playerColor = new Color(0.4f, 0.85f, 1f);
    [SerializeField] private Color _enemyColor  = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color _bossColor   = new Color(1f, 0.1f, 0.1f);
    [SerializeField] private Color _chestColor  = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _coinColor   = new Color(1f, 0.95f, 0.4f);
    [SerializeField] private Color _jarColor    = new Color(0.3f, 0.6f, 1f);

    [Header("Objective (보스 후 이동 유도)")]
    [SerializeField] private Color _gateColor = new Color(0.3f, 1f, 0.45f);
    [SerializeField] private float _gateSize = 13f;
    [SerializeField] private Color _arrowColor = new Color(0.3f, 1f, 0.45f);
    [Tooltip("플레이어 중심 유도 화살표 크기(가로,세로).")]
    [SerializeField] private Vector2 _arrowSize = new Vector2(9f, 26f);

    [Tooltip("마커가 사용할 단색 흰 sprite. 비우면 Default-UI 사각형 사용.")]
    [SerializeField] private Sprite _markerSprite;

    [Tooltip("유도 화살표 전용 sprite(위를 향한 화살표 권장). 비우면 _markerSprite(막대) 사용 — PM이 교체 가능.")]
    [SerializeField] private Sprite _arrowSprite;

    public static MinimapManager Instance { get; private set; }
    private static readonly List<MinimapTracker> s_pending = new();

    private readonly Dictionary<MinimapTracker, Image> _markers = new();
    private MinimapTracker _player;

    private Image _objMarker;       // 게이트(목표) 마커 — 가장자리 클램프
    private RectTransform _objMarkerRT;
    private Image _objArrow;        // 플레이어 중심 유도 화살표
    private RectTransform _objArrowRT;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < s_pending.Count; i++) AddInternal(s_pending[i]);
        s_pending.Clear();
        CreateObjectiveUI();
    }

    private void CreateObjectiveUI()
    {
        if (_markerLayer == null) return;

        var m = new GameObject("Marker_Objective", typeof(RectTransform));
        m.transform.SetParent(_markerLayer, false);
        _objMarker = m.AddComponent<Image>();
        _objMarker.sprite = _markerSprite;
        _objMarker.color = _gateColor;
        _objMarker.raycastTarget = false;
        _objMarkerRT = (RectTransform)m.transform;
        _objMarkerRT.sizeDelta = new Vector2(_gateSize, _gateSize);
        _objMarkerRT.anchorMin = _objMarkerRT.anchorMax = new Vector2(0.5f, 0.5f);
        _objMarkerRT.pivot = new Vector2(0.5f, 0.5f);
        _objMarker.enabled = false;

        var a = new GameObject("Objective_Arrow", typeof(RectTransform));
        a.transform.SetParent(_markerLayer, false);
        _objArrow = a.AddComponent<Image>();
        _objArrow.sprite = _arrowSprite != null ? _arrowSprite : _markerSprite;
        _objArrow.color = _arrowColor;
        _objArrow.raycastTarget = false;
        _objArrowRT = (RectTransform)a.transform;
        _objArrowRT.sizeDelta = _arrowSize;
        _objArrowRT.anchorMin = _objArrowRT.anchorMax = new Vector2(0.5f, 0.5f);
        _objArrowRT.pivot = new Vector2(0.5f, 0f); // 바닥 피벗 — 플레이어 중심에서 바깥으로 뻗음
        _objArrow.enabled = false;
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

        UpdateObjective(center, radiusToPixels, halfX, halfY);
    }

    // 보스 후 유도: 게이트 마커(가장자리 클램프) + 플레이어 중심 화살표(목표 방향 회전).
    private void UpdateObjective(Vector3 center, float radiusToPixels, float halfX, float halfY)
    {
        var tgt = MinimapObjective.Target;
        bool show = tgt != null;
        if (_objMarker != null) _objMarker.enabled = show;
        if (_objArrow != null) _objArrow.enabled = show;
        if (!show) return;

        Vector3 d = tgt.position - center;
        Vector2 ui = new Vector2(d.x, d.z) * radiusToPixels;

        if (_objMarkerRT != null)
            _objMarkerRT.anchoredPosition = new Vector2(
                Mathf.Clamp(ui.x, -halfX, halfX),
                Mathf.Clamp(ui.y, -halfY, halfY));

        if (_objArrowRT != null)
        {
            _objArrowRT.anchoredPosition = Vector2.zero;
            if (ui.sqrMagnitude > 0.0001f)
            {
                // 위(+Y)를 향한 화살표를 목표 방향으로 회전 (북쪽 위 고정 미니맵)
                float z = Mathf.Atan2(ui.y, ui.x) * Mathf.Rad2Deg - 90f;
                _objArrowRT.localRotation = Quaternion.Euler(0f, 0f, z);
            }
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
            case MinimapTracker.MarkerType.Jar:    return _jarSize;
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
            case MinimapTracker.MarkerType.Jar:    return _jarColor;
            default: return _enemyColor;
        }
    }
}

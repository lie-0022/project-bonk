using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 시 무기 3택 선택지를 생성·중개하는 싱글턴.
/// XPSystem.OnLevelUp 수신 → 선택지 빌드 → UI 표시 요청 → 선택 결과 WeaponSystem 적용.
/// 출처: design/gdd/levelup-selection.md
/// </summary>
public class LevelupWeaponSelection : MonoBehaviour
{
    public static LevelupWeaponSelection Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int _choiceCount = 3;

    /// <summary>한 카드 선택지. UI는 이 정보로 카드를 그린다.</summary>
    public readonly struct WeaponChoice
    {
        public readonly WeaponType WeaponType;
        public readonly CardGrade Grade;
        public readonly bool IsNew;
        public readonly int CurrentLevel;

        public WeaponChoice(WeaponType type, CardGrade grade, bool isNew, int currentLevel)
        {
            WeaponType = type;
            Grade = grade;
            IsNew = isNew;
            CurrentLevel = currentLevel;
        }
    }

    /// <summary>선택지 표시 요청. WeaponSelectionUI가 구독한다.</summary>
    public static event Action<IReadOnlyList<WeaponChoice>> OnSelectionRequired;

    /// <summary>선택 완료 후 발생. (전투 재개 시점 동기화용)</summary>
    public static event Action OnSelectionComplete;

    private readonly Queue<int> _pendingLevelUps = new();
    private bool _isSelecting;
    private float _previousTimeScale = 1f;

    /// <summary>UI가 열려 선택 대기 중인지. 다른 디버그 입력이 1/2/3 키 충돌을 피할 때 참조.</summary>
    public bool IsSelecting => _isSelecting;

    private static readonly WeaponType[] AllWeapons =
    {
        WeaponType.Sword, WeaponType.Gun, WeaponType.Magic,
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        XPSystem.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        XPSystem.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int level)
    {
        _pendingLevelUps.Enqueue(level);
        TryStartNext();
    }

    private void TryStartNext()
    {
        if (_isSelecting) return;
        if (_pendingLevelUps.Count == 0) return;

        _pendingLevelUps.Dequeue();

        var choices = BuildChoices();
        if (choices.Count == 0)
        {
            // 모든 무기 최대 레벨 또는 시스템 미준비 → 즉시 다음 큐 처리
            TryStartNext();
            return;
        }

        _isSelecting = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        OnSelectionRequired?.Invoke(choices);
    }

    /// <summary>UI에서 카드 선택 시 호출.</summary>
    public void ChooseWeapon(WeaponChoice choice)
    {
        if (!_isSelecting) return;

        var ws = WeaponSystem.Instance;
        if (ws != null)
        {
            if (ws.HasWeapon(choice.WeaponType))
                ws.UpgradeWeapon(choice.WeaponType, choice.Grade);
            else
                ws.AddWeapon(choice.WeaponType, choice.Grade);
        }

        Time.timeScale = _previousTimeScale;
        _isSelecting = false;
        OnSelectionComplete?.Invoke();

        // 큐잉된 다음 레벨업 즉시 처리
        TryStartNext();
    }

    private List<WeaponChoice> BuildChoices()
    {
        var pool = new List<WeaponChoice>();
        var ws = WeaponSystem.Instance;
        if (ws == null) return pool;

        foreach (var type in AllWeapons)
        {
            bool has = ws.HasWeapon(type);
            int level = ws.GetWeaponLevel(type);

            if (!has)
            {
                pool.Add(new WeaponChoice(type, CardGradeRoller.Roll(), true, 0));
            }
            else if (level > 0 && level < 15)
            {
                pool.Add(new WeaponChoice(type, CardGradeRoller.Roll(), false, level));
            }
        }

        Shuffle(pool);

        int take = Mathf.Min(_choiceCount, pool.Count);
        return pool.GetRange(0, take);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

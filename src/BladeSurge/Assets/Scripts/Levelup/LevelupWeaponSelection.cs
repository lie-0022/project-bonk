using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 선택지 종류.
/// </summary>
public enum LevelupChoiceKind { Weapon, Passive }

/// <summary>
/// 한 장의 카드 선택지 (무기 또는 패시브).
/// 출처: design/gdd/levelup-selection.md, design/gdd/spec-system.md
/// </summary>
public readonly struct LevelupChoice
{
    public readonly LevelupChoiceKind Kind;
    public readonly WeaponType WeaponType;     // Kind == Weapon 일 때 유효
    public readonly PassiveType PassiveType;   // Kind == Passive 일 때 유효
    public readonly CardGrade Grade;
    public readonly bool IsNew;
    public readonly int CurrentLevel;

    public LevelupChoice(WeaponType weapon, CardGrade grade, bool isNew, int currentLevel)
    {
        Kind = LevelupChoiceKind.Weapon;
        WeaponType = weapon;
        PassiveType = default;
        Grade = grade;
        IsNew = isNew;
        CurrentLevel = currentLevel;
    }

    public LevelupChoice(PassiveType passive, CardGrade grade, bool isNew, int currentLevel)
    {
        Kind = LevelupChoiceKind.Passive;
        WeaponType = default;
        PassiveType = passive;
        Grade = grade;
        IsNew = isNew;
        CurrentLevel = currentLevel;
    }
}

/// <summary>
/// 레벨업 시 무기·패시브 혼합 3택 선택지를 생성·중개하는 싱글턴.
/// XPSystem.OnLevelUp 수신 → 선택지 빌드 → UI 표시 요청 → 선택 결과 적용.
/// 출처: design/gdd/levelup-selection.md, design/gdd/spec-system.md
/// </summary>
public class LevelupWeaponSelection : MonoBehaviour
{
    public static LevelupWeaponSelection Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int _choiceCount = 3;

    /// <summary>패시브 슬롯 상한. spec-system.md:21,87,159 — 3종 빌드 정체성.</summary>
    private const int MaxPassiveSlots = 3;

    /// <summary>선택지 표시 요청. LevelupSelectionUI(WeaponSelectionUI)가 구독한다.</summary>
    public static event Action<IReadOnlyList<LevelupChoice>> OnSelectionRequired;

    /// <summary>선택 완료 후 발생. (전투 재개 시점 동기화용)</summary>
    public static event Action OnSelectionComplete;

    private readonly Queue<int> _pendingLevelUps = new();
    private bool _isSelecting;
    private float _previousTimeScale = 1f;

    /// <summary>UI가 열려 선택 대기 중인지. 디버그 키 충돌 회피용.</summary>
    public bool IsSelecting => _isSelecting;

    private static readonly WeaponType[] AllWeapons =
    {
        WeaponType.Sword, WeaponType.Gun, WeaponType.Magic,
    };

    /// <summary>레벨업 선택지에 노출할 패시브 12종. ProjectileSpeed는 AttackSpeed에 통합.</summary>
    private static readonly PassiveType[] AllPassives =
    {
        PassiveType.MaxHp, PassiveType.HpRegen, PassiveType.Dodge,
        PassiveType.AttackSpeed, PassiveType.CritChance, PassiveType.CritDamage,
        PassiveType.Lifesteal, PassiveType.ProjectileCount,
        PassiveType.MoveSpeed, PassiveType.ExtraJump,
        PassiveType.Luck, PassiveType.Difficulty,
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

    /// <summary>외부 시스템(상자 등)에서 카드 선택 1회 요청. 진행 중이면 큐에 누적.</summary>
    public void RequestSelection()
    {
        _pendingLevelUps.Enqueue(0);
        TryStartNext();
    }

    private void TryStartNext()
    {
        if (_isSelecting) return;
        if (_pendingLevelUps.Count == 0) return;

        _pendingLevelUps.Dequeue();

        // UI 미구독 시 timeScale=0 진입하면 영원히 멈추므로 차단 (개발 단계 보호용)
        if (OnSelectionRequired == null)
        {
            Debug.LogWarning("[LevelupWeaponSelection] 선택 UI 구독자 없음. 레벨업 선택 스킵.");
            TryStartNext();
            return;
        }

        var choices = BuildChoices();
        if (choices.Count == 0)
        {
            // 모든 항목 최대 레벨 → 즉시 다음 큐 처리
            TryStartNext();
            return;
        }

        _isSelecting = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        OnSelectionRequired.Invoke(choices);
    }

    /// <summary>UI에서 카드 선택 시 호출.</summary>
    public void Choose(LevelupChoice choice)
    {
        if (!_isSelecting) return;

        switch (choice.Kind)
        {
            case LevelupChoiceKind.Weapon:
                ApplyWeapon(choice);
                break;
            case LevelupChoiceKind.Passive:
                ApplyPassive(choice);
                break;
        }

        Time.timeScale = _previousTimeScale;
        _isSelecting = false;
        OnSelectionComplete?.Invoke();

        TryStartNext();
    }

    private void ApplyWeapon(LevelupChoice choice)
    {
        var ws = WeaponSystem.Instance;
        if (ws == null) return;
        if (ws.HasWeapon(choice.WeaponType))
            ws.UpgradeWeapon(choice.WeaponType, choice.Grade);
        else
            ws.AddWeapon(choice.WeaponType, choice.Grade);
    }

    private void ApplyPassive(LevelupChoice choice)
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        // 레벨은 항상 +1, 등급은 effective level 가중치로 수치 폭만 차등.
        stats.IncrementPassive(choice.PassiveType, choice.Grade);
    }

    private List<LevelupChoice> BuildChoices()
    {
        var pool = new List<LevelupChoice>();
        AppendWeaponChoices(pool);
        AppendPassiveChoices(pool);

        Shuffle(pool);

        int take = Mathf.Min(_choiceCount, pool.Count);
        return pool.GetRange(0, take);
    }

    private static void AppendWeaponChoices(List<LevelupChoice> pool)
    {
        var ws = WeaponSystem.Instance;
        if (ws == null) return;

        foreach (var type in AllWeapons)
        {
            bool has = ws.HasWeapon(type);
            int level = ws.GetWeaponLevel(type);

            if (!has)
            {
                pool.Add(new LevelupChoice(type, CardGradeRoller.Roll(), true, 0));
            }
            else if (level > 0 && level < 15)
            {
                pool.Add(new LevelupChoice(type, CardGradeRoller.Roll(), false, level));
            }
        }
    }

    private static void AppendPassiveChoices(List<LevelupChoice> pool)
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        int ownedCount = 0;
        foreach (var p in AllPassives)
            if (stats.GetPassiveLevel(p) > 0) ownedCount++;

        bool slotsFull = ownedCount >= MaxPassiveSlots;

        foreach (var passive in AllPassives)
        {
            int level = stats.GetPassiveLevel(passive);
            if (level >= PlayerStats.MaxPassiveLevel) continue;

            bool isNew = level == 0;
            if (isNew && slotsFull) continue;

            pool.Add(new LevelupChoice(passive, CardGradeRoller.Roll(), isNew, level));
        }
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

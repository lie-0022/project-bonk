/// <summary>
/// 사운드 이벤트 식별자. AudioCatalogSO의 매핑 키로 사용.
/// 신규 SFX 추가 시 이 enum에 항목 추가 후 카탈로그에서 클립 연결.
/// </summary>
public enum SfxEvent
{
    None = 0,

    // Player Attack
    PlayerAttackSword = 100,
    PlayerAttackGun,
    PlayerAttackMagic,

    // Player State
    PlayerHit = 200,
    PlayerDeath,
    PlayerFootstep,
    PlayerJump,

    // Enemy
    EnemyHit = 300,
    EnemyDeath,
    BossHit,
    BossDeath,
    BossSpawn,

    // Pickups
    PickupGold = 400,
    PickupXp,
    PickupMagnet,
    PickupSpeed,
    PickupTimeStop,

    // Containers
    ChestOpen = 500,
    ChestLocked,
    JarBreak,

    // Progression / UI
    LevelUp = 600,
    CardAppear,
    CardSelect,
    UiClick,
    UiHover,

    // Misc
    WeaponLevelUp = 700,
}

/// <summary>BGM 트랙 식별자.</summary>
public enum BgmTrack
{
    None = 0,
    MainMenu,
    GameplayStage1,
    GameplayStage3 = 4,
    BossBattle,
    BossBattleFinal,
    GameOver,
    GameClear,
}

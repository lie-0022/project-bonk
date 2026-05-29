/// <summary>
/// 씬 간 선택 상태 전달용 정적 홀더.
/// CharacterSelect 씬에서 고른 캐릭터/맵을 GamePlay 씬이 읽는다.
/// 정적이라 씬 전환에도 값이 유지되며(앱 재시작 시 기본값으로 초기화),
/// GamePlay를 단독 실행하면 기본값(전사/맵0)으로 동작한다.
/// </summary>
public static class GameSession
{
    /// <summary>선택한 캐릭터. 기본값 전사.</summary>
    public static CharacterType SelectedCharacter = CharacterType.Warrior;

    /// <summary>선택한 맵 인덱스(0/1/2). 기본값 0.</summary>
    public static int SelectedMapIndex = 0;

    /// <summary>선택 캐릭터에 대응하는 시작 무기. 전사→Sword, 마법사→Magic, 거너→Gun.</summary>
    public static WeaponType SelectedWeapon => SelectedCharacter switch
    {
        CharacterType.Warrior => WeaponType.Sword,
        CharacterType.Mage    => WeaponType.Magic,
        CharacterType.Gunner  => WeaponType.Gun,
        _                     => WeaponType.Sword
    };

    /// <summary>선택 맵에 대응하는 스테이지 적 종족. 맵0→Slime, 맵1→Goblin, 맵2→Skeleton.</summary>
    public static EnemyRace SelectedRace => SelectedMapIndex switch
    {
        0 => EnemyRace.Slime,
        1 => EnemyRace.Goblin,
        2 => EnemyRace.Skeleton,
        _ => EnemyRace.Slime
    };
}

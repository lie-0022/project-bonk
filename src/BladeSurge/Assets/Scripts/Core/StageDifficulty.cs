/// <summary>
/// 스테이지(맵)별 난이도 배율 홀더. DebugStageSelector가 맵 선택 시(Awake) 설정하고,
/// WaveSpawner(적/보스 체력)·GoldSystem/XPSystem(코인·경험치 보상)이 읽는다.
/// 기본 1.0(동작 변화 없음). PM이 DebugStageSelector 인스펙터에서 맵별로 플레이테스트하며 튜닝한다.
/// 정적이라 씬 리로드에도 유지되며, 매 스테이지 Awake에서 그 맵 값으로 다시 설정된다.
/// </summary>
public static class StageDifficulty
{
    /// <summary>잡몹·보스 최대 체력 배율.</summary>
    public static float EnemyHpMultiplier = 1f;

    /// <summary>적이 주는 데미지(접촉·공격·AOE) 배율. DamageDealer에서 적용.</summary>
    public static float EnemyDamageMultiplier = 1f;

    /// <summary>코인·경험치 획득량 배율.</summary>
    public static float RewardMultiplier = 1f;

    public static void Reset()
    {
        EnemyHpMultiplier = 1f;
        EnemyDamageMultiplier = 1f;
        RewardMultiplier = 1f;
    }
}

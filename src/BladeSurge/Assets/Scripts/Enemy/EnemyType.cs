/// <summary>
/// 적 분류 — 행동이 아닌 풀/스폰 식별용. 종족(Slime/Skeleton/Goblin)은 프리팹으로 구분.
/// </summary>
public enum EnemyType
{
    None = 0,
    Mob,    // 일반 잡몹 (단일 추적 행동)
    Boss,   // 보스 (1회성, 풀 미사용)
}

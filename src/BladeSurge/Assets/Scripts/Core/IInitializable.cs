/// <summary>
/// 씬 로드 시 명시적 초기화 순서가 필요한 싱글턴 시스템의 계약.
/// Unity Awake/Start 실행 순서에 의존하지 않고 GameManager가 순서를 제어한다.
/// 구현: XPSystem, GoldSystem, SkillSystem, ObjectPool
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// 시스템 초기화. GameManager.Start()에서 명시적 순서로 호출.
    /// </summary>
    void Initialize();
}

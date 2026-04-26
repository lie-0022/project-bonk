/// <summary>
/// 피격 시 시각·청각 피드백을 재생하는 컴포넌트의 계약.
/// HealthComponent의 OnDamaged 이벤트를 구독해 재생한다.
/// </summary>
public interface IHitFeedback
{
    /// <summary>
    /// 피격 피드백 재생 (흰색 플래시, 히트 사운드 등).
    /// 피해량에 따라 피드백 강도 조절 가능.
    /// </summary>
    void PlayHitFeedback(float amount);

    /// <summary>
    /// 사망 피드백 재생 (사망 이펙트, 사운드).
    /// </summary>
    void PlayDeathFeedback();
}

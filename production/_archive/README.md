# Production Archive

이 폴더는 Bonk(BladeSurge) 프로젝트의 **과거 작업 이력 아카이브**다.
참고 목적이며 현재 활성 작업이 아니다.

---

## 내용

### `sprints/`
- 2026-03-26 ~ 2026-04-05 진행한 sprint-01, sprint-02 계획 문서
- sprint-01: 3일 압축 MVP 시도 (2026-03-26 ~ 03-28)
- sprint-02: 카메라/적AI/웨이브 (2026-03-30 ~ 04-05)

---

## 왜 archive 됐는가

2026-04-26 부로 **B타입 워크플로우** 도입 결정:
- 1인 개발자 + AI 협업 모델
- 본인은 PM/결정자, AI가 풀스택 실무
- 고정 sprint 단위 대신 **continuous flow** (필요 시 작업 단위로 채팅)

자세한 내용: [`.claude/docs/workflow-b-type.md`](../../.claude/docs/workflow-b-type.md)

---

## 복원

향후 sprint 운영이 필요하면:

```bash
git mv production/_archive/sprints production/sprints
```

CCGS 프레임워크의 `/sprint-plan`, `/retrospective` 등 슬래시 커맨드는 그대로 유지되어 있다.

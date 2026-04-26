# Bonk

> 귀엽고 밝은 중세판타지 세계관에서 검사·마법사·거너 중 하나를 골라 몰려오는 몬스터를 쓸어버리는 **3D 뱀파이어 서바이벌라이크**

> ⚠️ 작업 코드명 "Bonk" 사용 중 (실제 폴더/코드는 BladeSurge, 일괄 치환 예정). MegaBonk에서 영감받은 학교 프로젝트.

---

## 🎮 About

**Bonk**는 MegaBonk에서 영감을 받은 3D 뱀파이어 서바이벌라이크입니다. 평면 2D가 주류인 서바이버 장르를 3D 공간으로 가져와 **이동·회피·점프**의 조작감을 더하고, 귀엽고 밝은 중세판타지 비주얼로 재해석합니다.

학교 프로젝트로 제작 중이며 비상업 용도입니다. 코어 루프 1-2주, 전체 10주 스코프를 목표로 합니다.

---

## ✨ Features

### 🎭 캐릭터 3종

- **마법사** (MVP 1순위) — 원거리 마법 볼트 (범위 마법)
- **검사** (MVP 2순위) — 전방 부채꼴 근접 공격, 생존력 강함
- **거너** (Vertical Slice) — 빠른 연사 투사체

### ⚔️ 핵심 메커니즘

- 자동 공격(가장 가까운 적) + 근접 공격(바라보는 방향)의 이중 전투
- 게임 중 무기 드롭·상자에서 새 무기 획득 → 빌드 완성
- 15-18분 / 3 스테이지 (**3분 + 5분 + 7분**)
- 스테이지 클리어 시 스펙 초기화 → 매 스테이지가 새로운 도전
- 4단계 등급 — **커먼**(회색) / **에픽**(보라) / **유니크**(노랑) / **레전드**(빨강)

### 🎨 비주얼

- SD/MD 스타일
- 귀엽고 밝은 중세판타지
- 프리렌(Frieren) 분위기 레퍼런스

---

## 🕹️ Controls

| 키 | 동작 |
|---|---|
| WASD | 이동 (카메라 기준) |
| 마우스 | 카메라 회전 |
| Shift | 대시 (무적 프레임) |
| Space | 점프 |
| E | 상호작용 (상자, 상인, 제단) |
| 자동 | 공격 (조건 충족 시 자동 발동) |

---

## 🛠️ Tech Stack

- **Engine**: Unity 6.3 LTS (6000.3.10f1)
- **Language**: C# (.NET Standard 2.1)
- **Render Pipeline**: URP (Universal Render Pipeline) — Render Graph API
- **Input**: New Input System
- **Physics**: Unity PhysX 3D
- **Platform**: PC (Windows 11 우선)
- **Development Tool**: [Claude Code](https://claude.ai) + [CCGS](https://github.com/Donchitos/Claude-Code-Game-Studios)

---

## 📁 Project Structure

```
project-bonk/
├── src/BladeSurge/         # Unity 프로젝트 (코드명 Bonk로 변경 예정)
│   └── Assets/Scripts/     # 게임 코드 (41 scripts, 10 categories)
│       ├── Combat/
│       ├── Core/
│       ├── Player/
│       ├── Enemy/
│       ├── Weapons/
│       ├── Skills/
│       ├── Progression/
│       ├── UI/
│       ├── Editor/
│       └── Util/
├── design/gdd/             # GDD (18개 문서)
├── docs/architecture/      # ADR (7개)
├── production/             # 프로덕션 문서 (sprints/는 _archive로 이동)
└── prototypes/             # 초기 프로토타입
```

---

## 🚧 Development Status

**현재 단계**: Pre-Production (Continuous flow, sprint 시스템 폐지)

| 영역 | 진행률 | 비고 |
|------|--------|------|
| Design (GDD) | ~58% | 18 docs (game-concept, systems-index, 16 시스템) |
| Architecture (ADR) | 7개 작성 | adr-0001 ~ adr-0007 |
| Source Code | ~17% | 41 scripts (10 카테고리) |
| Production | — | Sprint 시스템 retired (B타입 워크플로우, `production/_archive/` 참조) |
| Tests | 0% | Unity Test Framework 미설정 |

마지막 업데이트: 2026-04-26

---

## 🎯 Inspiration

| Reference | What We Take |
|-----------|--------------|
| **MegaBonk** (메인) | 3D 뱀서라이크 전체 구조, 상자·상인·제단 |
| Vampire Survivors | 웨이브 생존, 파워 에스컬레이션, 레벨업 선택 |
| Hades | 빠른 재시작, 타격감, 빌드 다양성 |
| 프리렌 (애니메이션) | 귀엽고 밝은 중세판타지 비주얼 레퍼런스 |

---

## 📝 License

- **Game**: TBD (학교 프로젝트, 비상업)
- **Framework**: MIT (CCGS — `LICENSE` 파일 참조)

---

*Built with [Claude Code](https://claude.ai) and [Claude Code Game Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)*

*🚧 Active development. Code name will be unified to "Bonk" in upcoming refactor.*

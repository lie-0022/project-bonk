# Naming Conventions — [TBD]

## 스크립트 (C#)

### 클래스 suffix 규칙

| suffix | 용도 | 예시 |
|--------|------|------|
| `Controller` | 입력·흐름 제어 | `PlayerController`, `CameraController` |
| `System` | 게임 규칙 처리 | `XPSystem`, `GoldSystem`, `SkillSystem` |
| `Component` | 데이터 컨테이너 | `HealthComponent` |
| `Manager` | 싱글턴 조율자 | `GameManager` |
| `UI` | UI 전용 스크립트 | `SkillSelectionUI`, `HUDController` |
| `SO` | ScriptableObject | `SkillDefinitionSO`, `EnemyStatsSO` |
| `Spawner` | 오브젝트 생성 | `WaveSpawner` |
| `Dealer` | 효과 적용자 | `DamageDealer` |

### 기타 규칙
- 클래스명: PascalCase
- private 필드: `_camelCase`
- public 프로퍼티: PascalCase
- 메서드: PascalCase
- 이벤트: `On` + PascalCase (예: `OnPlayerDied`, `OnGameStateChanged`)
- 네임스페이스: 사용 안 함

---

## 씬 (Scenes/)

| 씬 | 용도 |
|----|------|
| `MainMenu.unity` | 시작 화면, 캐릭터 선택 |
| `GamePlay.unity` | 실제 게임 (Stage 1~3 전부) |
| `GameOver.unity` | 결과 화면 (킬 수, 생존 시간) |

---

## 모델 (Art/Models/)

```
{대상}_{종류}_{변형}

Player_Swordsman
Enemy_Goblin
Enemy_Goblin_Elite
Environment_Rock
Environment_Tree
```

---

## 애니메이션 (Art/Animations/)

```
{대상}_{동작}

Player_Idle
Player_Run
Player_Dash
Player_Jump
Player_Attack
Enemy_Idle
Enemy_Run
Enemy_Attack
Enemy_Death
```

---

## VFX (Art/VFX/)

```
VFX_{효과명}

VFX_HitSpark
VFX_LevelUp
VFX_DashTrail
VFX_Death
VFX_ProjectileTrail
```

---

## 머티리얼 (Art/Materials/)

```
MAT_{대상}_{설명}

MAT_Player_Body
MAT_Enemy_Goblin
MAT_Environment_Ground
```

---

## UI 스프라이트·아이콘 (Art/UI/)

```
UI_{종류}_{이름}

UI_Icon_Skill_Slash
UI_Icon_Skill_Fireball
UI_BG_SkillPanel
UI_Button_Confirm
UI_Frame_Card_Common
UI_Frame_Card_Epic
UI_Frame_Card_Unique
UI_Frame_Card_Legend
```

---

## 프리팹 (Prefabs/)

```
{도메인}_{이름}

Player_Swordsman
Enemy_Goblin
Enemy_Goblin_Elite
Projectile_Arrow
Projectile_MagicBolt
VFX_HitSpark
VFX_LevelUp
UI_HUD
UI_SkillSelectionPanel
```

---

## 데이터 ScriptableObject (Data/)

```
{대상}_{이름}_Data

Player_Swordsman_Data       → Data/Player/
Enemy_Goblin_Data           → Data/Enemy/
Enemy_Goblin_Elite_Data     → Data/Enemy/
Skill_Slash_Data            → Data/Skills/
Skill_Fireball_Data         → Data/Skills/
```

---

## 오디오 (Audio/)

```
BGM_{스테이지or상황}       → Audio/BGM/
SFX_{대상}_{동작}          → Audio/SFX/

BGM_Stage1
BGM_Stage2
BGM_Stage3
SFX_Player_Dash
SFX_Player_Jump
SFX_Enemy_Hit
SFX_LevelUp
SFX_Chest_Open
```

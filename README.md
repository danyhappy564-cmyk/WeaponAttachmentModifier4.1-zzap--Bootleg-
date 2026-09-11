### ⚠️ IMPORTANT NOTICE / DISCLAIMER

**Original Author:** McOnie
**Original Repository:** WeaponAttachmentModifier
**Original Link:** https://github.com/McOnie/WeaponAttachmentModifier
**License:** MIT
**This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original author.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

# Weapon Attachment Modifier

무기 부착물의 **인체공학 / 반동 / 총열 내구도 소모**를 조절하는 모드. **SPT 4.1.5** 용으로 포팅했습니다.

> 원작: **McOnie** — https://github.com/McOnie/WeaponAttachmentModifier (SPT 4.0.3) · MIT
> 이 저장소는 위 원작의 4.1 포팅이고, **F12로 실시간 조절**이 가능하도록 BepInEx 클라이언트
> 플러그인을 새로 붙였습니다.

## 두 개로 나뉘어 있습니다

| | 위치 | 설정하는 곳 | 적용 시점 |
|---|---|---|---|
| **서버 모드** | `user\mods\WeaponAttachmentModifier` | `config\config.jsonc` | 서버 시작할 때 DB에 굽힘 |
| **클라 플러그인** | `BepInEx\plugins\WeaponAttachmentModifier` | **F12** | 실시간, 재시작 없음 |

둘 다 **기본값이 완전 중립**입니다 — 배수는 전부 `1.0`, 오버라이드는 전부 꺼짐. 그냥 깔면
아무것도 안 바뀌고, 둘 중 하나를 건드려야 효과가 납니다.

**둘은 곱해집니다.** 플러그인은 클라가 서버에서 받은 값을 기준선으로 잡고 그 위에 자기
숫자를 겁니다. 그러니 하나만 쓰세요 — `config.jsonc`는 그대로 두고 **F12로만 조절**(편한 쪽),
아니면 F12는 그대로 두고 파일로 조절. 양쪽에 1.5를 넣으면 2.25가 됩니다.

서로 필수는 아닙니다. 서버 모드만 써도 되고, 플러그인만 써도 됩니다.

## F12 쓰는 법

게임 켜고 **F12** → *Weapon Attachment Modifier*. 섹션 4개:

- `1. Foregrip` — 인체공학, 반동
- `2. Stock` — 인체공학, 반동
- `3. Pistol Grip` — 인체공학
- `4. Muzzle Device` — 인체공학, 반동, 내구도 소모
  (소염기·컴펜세이터·소음기·나사 어댑터가 이 한 블록을 같이 씁니다)

스탯마다 **Multiplier 슬라이더**가 있고, **Advanced** 토글 안에 *Override Mode* /
*Override Value* 한 쌍이 더 있습니다 — 배수 말고 정확한 숫자를 박고 싶을 때 쓰세요.

슬라이더에서 손 떼는 순간 반영됩니다. `1.0`으로 되돌리거나 `0. General → Enabled`를 끄면
서버가 보낸 값으로 **정확히 복귀**합니다. 매번 "직전에 쓴 값"이 아니라 원본 기준선에서 다시
계산하기 때문에, 값이 굳거나 누적되는 일이 없습니다.

한 가지 제약: **이미 진행 중인 레이드에 들고 들어간 총**은 생성될 때 값을 그대로 씁니다.
은신처나 메인 메뉴에서 조절하면 다음 레이드부터 적용됩니다.

## 배수가 무슨 뜻이냐

### 인체공학 (Ergonomics)
여기만 **부호가 의미를 바꿉니다**. 부착물의 인체공학 값이 양수면 보너스, 음수면 페널티예요.
그래서 배수는 보너스를 **키우고** 페널티를 **줄입니다**:

- `+10` 에 `2.0` → `+20`
- `-10` 에 `2.0` → `-5`

즉 `1.0`보다 크면 항상 "총이 좋아짐", 작으면 항상 "나빠짐".

### 반동 (Recoil)
그냥 곱하기. 부착물 반동 값은 퍼센트고, 음수가 클수록 반동 감소가 큽니다:

- `-20%` 에 `1.5` → `-30%`
- `-20%` 에 `0.5` → `-10%`

### 내구도 소모 (Durability Burn)
그냥 곱하기, 총구 장치만. `1.0`보다 크면 총열이 빨리 닳습니다:

- `+50%` 에 `1.5` → `+75%`

## 오버라이드 모드

양쪽 다 같은 세 가지를 같은 순서로 봅니다 — 하드셋 먼저, 그다음 덧셈, 마지막이 배수:

| 모드 | 효과 |
|---|---|
| **HardSet** | 아이템 값 무시하고 내가 준 값으로 |
| **Additive** | 아이템 값 **+** 내가 준 값 |
| **Off** / 배수 | 위 규칙대로 배수 적용 |

계산식은 `Shared/AttachmentStats.cs` 한 파일에 있고 서버 모드와 플러그인 **양쪽에
컴파일해 넣습니다.** F12 슬라이더와 `config.jsonc`에 같은 숫자를 넣으면 반드시 같은 결과가
나옵니다.

## 개별 아이템 지정

`config.jsonc`의 `SpecificAttachmentOverrides`에 템플릿 ID로 개별 부착물의 값을 못박을 수
있습니다. 여기 적힌 아이템은 정확히 그 값이 되고, 카테고리 튜닝은 건너뜁니다.

**서버 전용**입니다 — F12에는 목록 편집기가 없어서요. 동봉된 config에 주석 처리된 예시가
있습니다. 적어넣은 항목만 바뀌고, 애초에 그 스탯이 없는 아이템이면 그 항목은 무시됩니다.

## 빌드

`Directory.Build.props`의 `SptRoot` 기본값은 `E:\SPT 4.1` 입니다. 다르면 `local.props`
(gitignore 됨)를 만들거나 커맨드라인으로:

```
dotnet build -c Release -p:SptRoot="D:\내SPT경로"
```

빌드 성공하면 알아서 들어갑니다:

- `$(SptRoot)\SPT_Runtime\user\mods\WeaponAttachmentModifier\` — 서버 모드,
  그리고 **최초 빌드에만** `config\config.jsonc` (이미 있는 config는 절대 안 덮어씀)
- `$(SptRoot)\BepInEx\plugins\WeaponAttachmentModifier\` — 클라 플러그인

서버나 게임을 켜놓고 빌드해서 DLL이 잠겨있으면 `-p:SkipDeploy=true`, 아예 배포를 끄려면
`SptRoot`를 `CHANGE_ME`로. 배포는 Windows에서만 돕니다(`SptRoot`가 윈도우 경로라서);
다른 데서 강제하려면 `-p:OS=Windows_NT`.

## 4.1 포팅에서 바뀐 것

- `AbstractModMetadata` → `IModMetadata` 인터페이스 (`IsBundleMod` 삭제, `HasPrepatcher` 추가),
  `IOnLoad.OnLoad()` → `OnLoadAsync(CancellationToken)`
- `DatabaseServer` / `DatabaseTables`가 없어져서 `TemplateTable`을 직접 주입받습니다
- `OnLoadOrder.PostDBModLoader`가 없어졌습니다. 4.1의 마지막 슬롯인 `PostLoad`로 옮겨서,
  다른 모드가 추가한 부착물까지 같이 튜닝됩니다
- **인체공학과 반동을 더 이상 정수로 반올림하지 않습니다.** 원작은 리플렉션으로 이 값들에
  접근하면서 들어갈 때 반올림을 했는데, 4.1에서는 그냥 `double?`이고 실제 템플릿에는
  `-21.25` 반동, `-0.5` 인체공학 같은 값이 널려 있습니다. `-0.5` 인체공학에 `1.2`배를 걸면
  `-1`로 반올림돼서 **67% 오차**가 났습니다. 이제 아무것도 반올림하지 않습니다
- `config.jsonc`가 없거나 깨졌을 때 서버를 죽이는 대신 에러 한 줄 찍고 아무것도 안 바꿉니다.
  이게 중요한 게, **원본 저장소에는 이 모드가 읽는 config 파일이 아예 없었습니다**

## 클라 플러그인은 어떻게 실시간이 되나

튜닝한 값을 클라에 로드된 아이템 템플릿에 직접 씁니다. 이게 먹히는 이유는 아래쪽 어디에도
캐싱이 없기 때문입니다:

```csharp
// EFT.InventoryLogic.Mod
public float Ergonomics => GetTemplate<ModTemplate>().Ergonomics;

// EFT.InventoryLogic.Weapon
public float ErgonomicsDelta => Mods.Sum(mod => mod.Template.Ergonomics) / Mathf.Max(1f, Template.Ergonomics);
public float RecoilDelta    => Mods.Sum(mod => mod.Template.Recoil) / 100f;
```

읽을 때마다 템플릿에서 다시 합산합니다. 그래서 슬라이더 하나가 검사창·인벤토리 스탯바·실제
총 반동을 동시에, 재시작 없이 움직입니다.

**Harmony 패치는 하나도 없습니다.** `ItemFactory`, `ItemTemplates`, `ModTemplate` 계열이
4.1 역난독화 어셈블리에서 전부 public이라 publicized 빌드도 `spt-reflection`도 필요 없고,
게임 업데이트 때 다시 바인딩할 대상도 없습니다.

## 검증

- 클라 플러그인: **실제 4.1.5 `Assembly-CSharp.dll`** 상대로 컴파일 (경고 0). 리플렉션이
  없으니 컴파일 자체가 바인딩 검증입니다
- 서버: 42개 체크 하네스 통과 — 우선순위(HardSet > Additive > 배수), 음수 인체공학 나눗셈과
  0 근처 가드, 소수점 정밀도, 개별 오버라이드가 카테고리 튜닝을 대체하는지, 대상 아닌
  아이템이 안 건드려지는지, 동봉 config가 SPT 자체 `JsonUtil`로 대소문자 구분해 바인딩되는지,
  config 없을 때 안 죽는지
- 배포: `SPT_Runtime` + 루트 `BepInEx` 구조 복제본에서 두 DLL과 config가 제자리에 떨어지고,
  이미 수정한 config는 덮어쓰지 않는 것 확인

## 라이선스

원작 **McOnie**, MIT. `LICENSE` 참고.

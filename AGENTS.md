# Negative Carver — 에이전트 계약

파일럿 요건은 [`docs/prd_pilot.md`](docs/prd_pilot.md), 진척과 수용 기준은 [`to-do-list.md`](to-do-list.md)에 있다. 이 파일은 명령, 지도, 경계만 담는다.

## 명령

로컬 검증 (Windows):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1
```

이 명령이 하는 일: `dotnet test NegativeCarver.sln --nologo --verbosity minimal` 후, 통과하면 `.harness/last-verify`를 갱신한다.

CI는 같은 테스트를 직접 실행한다.

```bash
dotnet test NegativeCarver.sln --nologo --verbosity minimal
```

.NET SDK가 없으면 검증은 실패한다. SDK 8 이상을 설치한다. `global.json`은 8.0.100에서 같은 메이저 이후 버전까지 허용한다.

Unity 에디터 재생, 씬 확인, 패키지 추가는 에이전트가 대신 실행하지 않는다. 절차를 사용자에게 적는다.

패키지 명령은 pnpm만 쓴다. npm은 훅이 거절한다.

## 지도

| 경로 | 역할 |
| --- | --- |
| `src/NegativeCarver.Core` | 규칙. UnityEngine 참조 금지. `netstandard2.1` |
| `Assets/Plugins/NegativeCarver.Core.dll` | 위 프로젝트의 빌드 결과. 검증 명령이 갱신한다 |
| `tests/NegativeCarver.Core.Tests` | 규칙이 깨지면 실패하는 시험 |
| `Assets` | Unity 6.6 URP 프로젝트. 입력·프리팹·연출만 담당 |
| `ProjectSettings` | PC 1920×1080, 모바일 창 1080×1920 |
| `.cursor/agents` | `slice-planner`, `unity-implementer`, `acceptance-reviewer` |
| `.cursor/skills/negative-carver-harness` | 한 칸씩 구현하는 절차 |
| `.cursor/rules` | 항상 적용되는 경계와 C# 규칙 |
| `.cursor/hooks.json` | 세션 맥락, 셸 게이트, 검증 누락 시 한 번 재촉 |

한 번의 작업은 `to-do-list.md`의 체크박스 하나다. 기획 에이전트가 칸을 고르고, 구현 에이전트가 코드만 고치고, 검토 에이전트가 수용 기준을 대조한다. 검토가 통과하고 검증 명령이 0으로 끝나야 그 칸을 `[x]`로 바꾼다.

## 경계

항상 한다

- 그리드 변경은 Core의 공개 API로만 한다.
- 파일럿 수치는 `PilotRules` 상수만 바꾼다. 판정 로직에 숫자를 다시 적지 않는다.
- 블록 제거는 그리드 값과 표현 객체 제거를 한 호출에서 맞춘다.
- 연출 실패가 승패 판정을 막지 않게, 파티클과 오디오는 규칙 처리 뒤에 호출한다.

먼저 묻는다

- `docs/prd_pilot.md`의 규칙 변경
- `to-do-list.md`의 "파일럿에서 고정하는 규칙" 변경
- 커밋, 푸시, Unity 에디터 실행
- 새 패키지, URP 에셋, 씬 파일 생성

건드리지 않는다

- `git reset --hard`, `git clean`, `git push --force`
- npm
- 사용자가 이번 요청에 적지 않은 Step의 기능
- Core 프로젝트의 UnityEngine 참조

## 끝난 상태

- 이번 칸의 수용 기준을 시험 또는 코드 경로로 증명했다.
- `scripts/verify.ps1`이 실패하지 않았다.
- `to-do-list.md`의 해당 칸, 상단 완료 수, 진척 일지를 같이 고쳤다.

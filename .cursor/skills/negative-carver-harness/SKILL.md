---
name: negative-carver-harness
description: >-
  Runs one Negative Carver pilot slice through plan, implement, verify, and
  review. Use when implementing the game, advancing to-do-list.md, carving,
  GridManager, CutterController, gravity collapse, CoreGem, or when the user
  mentions 하네스, 네거티브 카버, or the next pilot task.
---

# Negative Carver 하네스

한 턴에는 `to-do-list.md`의 체크박스 하나만 끝낸다. 절차는 아래 순서를 지킨다.

## 루프

1. `AGENTS.md`, `docs/prd_pilot.md`, `to-do-list.md`를 읽는다.
2. 사용자가 칸을 지정하지 않았으면 `slice-planner`에게 다음 칸 하나와 허용 파일을 고르게 한다.
3. `unity-implementer`에게 그 칸만 구현하게 한다. 구현자는 할 일 문서를 완료로 바꾸지 않는다.
4. `scripts/verify.ps1`을 실행한다. 실패하면 같은 칸 안에서만 고치고 다시 실행한다.
5. `acceptance-reviewer`에게 차이, 수용 기준, 검증 결과를 검토하게 한다. 검토자는 파일을 고치지 않는다.
6. 검토가 통과이고 검증 종료 코드가 0일 때만 그 칸을 `[x]`로 바꾸고, 상단 완료 수와 진척 일지를 갱신한다.
7. 사용자가 계속하라고 하지 않았으면 다음 칸으로 넘어가지 않는다.

## 슬라이스 카드

기획 결과는 이 형식으로 고정한다.

```markdown
## Slice
- id:
- done when:
- files allowed:
- out of scope:
- verify: powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 증명

- 규칙과 수치는 `src/NegativeCarver.Core`와 `tests/NegativeCarver.Core.Tests`에서 증명한다.
- 씬, 파티클, 카메라처럼 에디터 재생이 필요한 부분은 사용자에게 재생 절차를 적고, 그 칸은 재생 확인 전까지 `[~]`로 둔다.
- Unity 에디터와 개발 서버는 에이전트가 켜지 않는다.

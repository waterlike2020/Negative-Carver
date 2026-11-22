# 📄 [PRD] 네거티브 카버 (Negative Carver) Pilot 개발 요건서

## 1. 프로젝트 개요 (Overview)
* **프로젝트명:** Negative Carver (Pilot Prototype)
* **장르:** 2.5D/3D 물리 기반 역발상 카빙 퍼즐 (Physical Carving Puzzle)
* **핵심 컨셉:** 텅 빈 보드에 블록을 쌓는 기존 테트리스와 달리, **채워져 있는 입체 암석 덩어리를 테트로미노 형태의 음각 커터(Carver)로 깎아내어** 가로 줄을 관통시키고, 상층부 물리 붕괴 연쇄 콤보를 유발해 코어 유물을 구출하는 퍼즐.
* **목표 엔진/환경:** Unity 2022.3 LTS 이상 (C#, URP), 모바일/PC 겸용

---

## 2. 게임 코어 규칙 & 상태 구조 (Core Rules & Data State)

```text
 [ 음각 커터 (Tetromino Cutter) ]  ---> 회전 및 좌우 이동 후 하강 투하
        ↓ (투하)
 ┌─────────────────────────────┐
 │ █ █ █ █ █ █ █ █ █ █ █ █ █ █ │  <- 빽빽한 암석 격자 (Solid Grid)
 │ █ █ [커터 닿은 면적 바스러짐] █ │
 │ █ █ █ █ █ █ █ █ █ █ █ █ █ █ │
 │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░ │  <- 1줄 완전히 깎임 (Line Penetrated)
 │ █ █ █ █ [ 코어 유물 ] █ █ █ │  <- 상층부 암석 묵직하게 낙하 (Physic Collapse)
 └─────────────────────────────┘
```

1. **그리드 규격 (Board Grid):**
   * 가로 10칸 × 세로 18칸 (기본 2D 그리드 좌표계 기반, 각 칸은 $1 \times 1$ Unity World Unit).
2. **초기 셋업 (Initial State):**
   * 보드 하단 1~12행은 `BlockState.Solid` 암석 블록으로 가득 채워짐.
   * 암석 블록 내부 특정 좌표(예: X: 4~5, Y: 2~3)에 `CoreGem` (목표 오브젝트) 배치.
3. **음각 커터 투하 (Cutter Drop):**
   * 상단(Y: 16)에서 7종 테트로미노 형태의 `Cutter` 생성 (I, J, L, O, S, T, Z).
   * 커터 조작: 좌/우 이동, 90도 회전, 투하. 투하는 한꺼번에 떨어지지 않고 한 칸씩 내려가며, 그 동안 좌우로 방향을 바꿀 수 있다. 칸 간격은 파일럿 상수로 두고 나중에 빨라진다.
4. **절삭 판정 (Carving Collision):**
   * 커터가 암석의 윗면이나 옆면에 먼저 닿으면, 그 맞닿은 `SolidRock`만 `Empty`로 전환 및 Destroy. `CoreGem`은 깎지 않는다.
   * 파쇄 파티클(Shatter Effect) 및 재질별 ASMR 사운드 재생.
5. **관통 & 붕괴 판정 (Penetration & Gravity Collapse):**
   * 가로 한 줄(Row)의 모든 암석 칸(10칸)이 `Empty`가 되면 해당 Row는 '관통'으로 처리.
   * **물리 붕괴:** 관통된 Row 위의 모든 암석 층을 1칸 아래로 물리 낙하(Y - 1)시킴.
   * 이 낙하로 인해 다시 새로운 관통 줄이 생기면 **연쇄 붕괴 콤보(Chain Combo)** 실행.
6. **승리/패배 조건:**
   * **승리:** `CoreGem`을 둘러싼 주변 암석이 모두 제거되어 유물이 외부로 완전 해방될 때.
   * **패배:** 암석 층이 상단 한계선(Y: 15) 위로 솟구치거나 지정된 커터 투하 횟수 초과 시.

---

## 3. 클래스 구조 설계 (Architecture & Class Diagram)

```csharp
// 1. 블록 데이터 상태 Enum
public enum BlockType { Empty, SolidRock, CoreGem, CutterPreview }

// 2. 그리드 관리자 (GridManager.cs)
// - 10x18 2D 배열로 보드 상태 관리
// - 절삭(Carve), 줄 관통 체크(CheckPenetration), 중력 낙하(ApplyGravity) 담당
public class GridManager : MonoBehaviour {
    public int width = 10;
    public int height = 18;
    public BlockType[,] grid;
    
    public void CarveArea(List<Vector2Int> cutterPattern, Vector2Int origin);
    public void CheckAndCollapseLines();
    public bool CheckCoreGemFreed();
}

// 3. 음각 커터 조종기 (CutterController.cs)
// - 커터 회전, 이동, 하드드롭 구현
// - 충돌 시 GridManager.CarveArea() 호출
public class CutterController : MonoBehaviour {
    public Vector2Int position;
    public int[,] shapeMatrix;
    
    public void MoveLeft();
    public void MoveRight();
    public void Rotate();
    public void HardDrop(); // 한 칸씩 느리게 내려가며, 윗면이나 옆면이 먼저 닿으면 Carve
}

// 4. 파괴/물리 이펙터 (CarveFxManager.cs)
// - 파쇄 파티클 생성 (Instantiate ParticleSystem)
// - 화면 흔들림 (Camera Shake) 및 절삭 ASMR 음향 재생
public class CarveFxManager : MonoBehaviour {
    public void PlayCarveEffect(Vector3 worldPos);
    public void PlayCollapseEffect(int lineY);
}
```

---

## 4. 파이럿(Pilot) 단계 기능 구현 우선순위 (Sprint Plan)

* **Step 1 [최우선 코어]: 2D Grid + Cutter 파괴 로직**
  * 10×18 배열 생성 후 하단 10줄을 기본 큐브 Prefab으로 채우기.
  * 테트로미노 커터를 위에서 떨어뜨려, 부딪힌 위치의 큐브 Prefab이 `Destroy`되고 그리드 배열 값이 `Empty`로 바뀌는 기본 로직 구현.
* **Step 2 [핵심 재미]: 줄 관통 및 중력 붕괴**
  * 가로 10칸이 비워졌는지 검사하는 `CheckPenetration()` 작성.
  * 관통 시 그 위의 큐브들이 아래로 1칸 이동하는 `ApplyGravity()` 작성 및 연쇄 콤보 판정.
* **Step 3 [게임 완성도]: 코어 유물 구출 & UI**
  * 중앙에 색상이 다른 `CoreGem` 큐브 배치.
  * CoreGem 사방의 큐브가 없어지면 "Stage Clear" UI 팝업.
* **Step 4 [시각/청각 피드백]: 연출 적용**
  * 큐브 파괴 시 파티클 분사, 카메라 Shake, 절삭음(ASMR) 추가.

---

## 5. AI 개발 엔진 전달용 프롬프트 (Prompt for AI Code Generator)

```text
[Role]
You are an expert Unity C# Game Developer. Help me build a Pilot prototype for a novel 2.5D carving puzzle game named "Negative Carver".

[Game Concept]
Unlike traditional Tetris (which places blocks into empty spaces), this game starts with a board FULL of solid rock blocks. The player drops Tetromino-shaped "Cutter" pieces to CARVE AWAY and destroy the rock blocks it hits. 

[Key Features & Requirements]
1. Board System:
   - 10x18 grid array (`BlockType[,] grid`).
   - Bottom 1~10 rows are initially filled with Solid Rock blocks.
   - Place a `CoreGem` (Target item) inside the rock grid at (4,2).

2. Cutter Mechanics (Tetromino):
   - Player controls a falling Tetromino Cutter (I, J, L, O, S, T, Z shapes).
   - Controls: Arrow keys / WASD (Move Left/Right, Rotate 90 deg, Hard Drop Down).
   - On Hard Drop or Collision: The Cutter carves out all solid blocks matching its shape. Destroy the Carved block GameObjects and set grid state to `BlockType.Empty`.

3. Line Penetration & Physics Collapse:
   - When an entire horizontal row (10 blocks) becomes Completely EMPTY (`BlockType.Empty`), that row is marked as "Penetrated".
   - All rock block rows ABOVE the penetrated row must drop down by 1 unit (simulating gravity collapse).
   - Check recursively for chain combos.

4. Win Condition:
   - If all 4 adjacent blocks (Up, Down, Left, Right) of the `CoreGem` are cleared to `Empty`, trigger Stage Clear.

Please write a clean, well-commented Unity C# script set including:
- `GridManager.cs` (Handles grid array, block spawning, carving, line checking, and gravity collapse)
- `CutterController.cs` (Handles Tetromino movement, rotation, and input)
- `BlockTile.cs` (Attached to individual block prefabs)

Start by providing `GridManager.cs` first.
```
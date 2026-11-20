# Cat_Tang — 오브젝트 풀링 최적화 명세 (Codex 작업)

Unity 6.3 프로젝트 `C:\Project\Cat_Tang\Game` (스크립트 `Game/Assets/Scripts`, 네임스페이스 `CatTang`, 주석은 한국어 평서체, 기존 코드 스타일 그대로).
너는 Unity 에디터를 실행할 수 없다. 코드만 고치고, 컴파일 확인 · 플레이 확인은 Claude가 한다. 그래서 **C# 문법 · Unity API를 아주 조심해서** 쓰고, 확실하지 않은 API는 쓰지 마라.

## 왜
지금 모든 이펙트 · 탄 · 피해 숫자 · 예고가 `new GameObject` / `Instantiate` → 수명 끝에 `Destroy`다. 측정(이펙트 300개 동시, 에디터):

| 방식 | 띄우기 | 메모리 증가 | 치우기 |
|---|---|---|---|
| 지금 (Instantiate → Destroy) | 7.55ms (개당 25µs) | 1.5MB (개당 ~5KB) | 1.68ms + Destroy 지연 처리 |
| 오브젝트 풀 (SetActive) | 1.96ms | 125KB (FxPart.Begin이 MaterialPropertyBlock을 매번 새로 만듦) | 0.50ms |
| 오브젝트 없는 배열 풀 + Graphics.DrawMesh (`DashFx.cs`) | 0.08ms | 0 | 0 |

살아 있는 동안의 프레임 비용은 셋이 비슷 → 이득은 **생성 · 파괴 순간과 쓰레기(GC) 끊김**이다. 전투 중 초당 수십 개가 생겼다 사라져 주기적 GC 끊김과 한꺼번에 사라질 때 Destroy 몰림이 생긴다.

## 방향 (섹션별로 나눠서 진행, 섹션마다 독립적으로 동작해야 함)

### 0. 공통 풀 (`Scripts/Game/Pool/` 새 폴더)
- `GameObjectPool` (static): 키(문자열 또는 프리팹)별 `Stack<GameObject>`. `Get(key, factory)`, `Release(go)`, `Prewarm(key, factory, count)`, 키별 상한(`maxKeep`, 넘치면 Destroy).
- 풀에 쉬는 오브젝트는 `SetActive(false)` + 하이어라키 루트 `"[Pools]"` 아래로 (씬에 하나, 씬이 바뀌면 같이 사라짐 → 정적 딕셔너리에서 파괴된 항목은 `if (!go)`로 걸러 버림. DontDestroyOnLoad 쓰지 마라: 판(Run) 씬을 다시 불러올 때 깨끗해야 함).
- `IPooled` 인터페이스: `void OnSpawned()` / `void OnDespawned()` — 상태 되돌리기 책임은 각 컴포넌트.
- `PoolStats`: 키별 생성 수 · 재사용 수 · 쉬는 수 (디버그용, `Debug.Log` 남발 금지).
- Release된 오브젝트에 남은 코루틴은 `StopAllCoroutines`, `Invoke`는 `CancelInvoke`.

### 1. 프리팹 이펙트 — `FxEffect.Spawn` / `FxPart` (가장 많이 씀: FX_Hit · FX_HitCrit · FX_Explosion · FX_SmokePuff · FX_FootDust · FX_DustClump · FX_Ch3_* · FX_Trait_* …)
- `FxEffect.Spawn(name, …)`: 이름별 풀에서 꺼냄 → 자리 · 회전 · 크기 · VividLayer → `Play(tint)`.
- 지금은 `Play`가 `Destroy(gameObject, end + 0.05f)`. 대신 끝나는 시각에 Release (`Update`에서 시간 확인, 또는 코루틴). `Follow` 대상 초기화.
- `FxPart.Begin`: `mpb`를 처음 한 번만 만들고 다시 쓸 때는 `mpb.Clear()` 후 다시 채움. `transform.localPosition/localRotation/localScale`의 **원래 값을 Awake에서 저장**해 두고 Begin 때 되돌림 (지금 코드가 Begin에서 localScale · localRotation을 읽어 baseScale · baseRot로 쓰고, Pose가 localScale · position을 덮어씀 → 재사용하면 줄어든 크기를 기준으로 삼는 버그가 생김. 반드시 원래 값 저장 · 복원).
- 플립북 조각(`flipSet`)이 `mf.sharedMesh` · `mr.sharedMaterial`을 바꾸는 건 그대로 둬도 됨 (같은 프리팹이면 같은 값).
- `FxEffect.Has` · 캐시는 그대로.

### 2. 런타임 조립 이펙트
- `FxBurst.Spawn` (호출 105곳): `GameObject.CreatePrimitive`류로 만들고 Collider를 지우는 구조 → 종류별 풀. 재질 · MaterialPropertyBlock 재사용.
- `FxMesh.Spawn` (GunkataFx.cs, 44곳), `FxFlipbook.Spawn` (GunkataFx.cs, 20곳): 풀.
- `Tracer.Spawn` / `SpawnPath`: LineRenderer 오브젝트 풀 (positionCount · 색 · 폭 되돌림).
- `StylizedFx.Spawn` (47곳): 풀. `FlashRibbon`은 메시를 매번 새로 만드는데, 풀에서 꺼낼 때 메시는 재사용 (`mesh.Clear()`), 리스트도 Clear.
- `Afterimage`는 구운 메시를 들고 있어 복잡함 → **이번엔 건드리지 마라**.

### 3. 게임플레이 오브젝트
- `EnemyProjectile.Spawn`: 탄 풀 (스타일별 키). 자식(꼬리 · 숫자 · 장식)을 Destroy하던 코드(`foreach (Transform ch in transform) Destroy(ch.gameObject)`)를 풀 방식에 맞게 — 스타일별 완성품을 통째로 재사용하는 게 가장 단순. TrailRenderer는 꺼낼 때 `Clear()`, 넣을 때 `emitting=false`. 맞음 판정 · 거리 · 수명 · 튕김 횟수 등 모든 필드 되돌리기.
- `DamageNumber.Spawn` (숫자 글자 자식 생성/Destroy) → 풀. TMP/글자 오브젝트 재사용.
- `Telegraph` (예고: 선 · 부채꼴 · 원): 종류별 풀. `Kill()`이 Release로.

### 4. 미리 만들어 두기
- `RunManager`가 방(스테이지)을 지을 때(`BuildStage` 끝) 그 장에서 자주 쓰는 것만 몇 개씩 `Prewarm` (FX_Hit 12, FX_HitCrit 6, 적 탄 스타일별 16, DamageNumber 12, FxBurst Spark/Hit 12 정도). 한 프레임에 몰리지 않게 코루틴으로 프레임당 몇 개씩.

### 5. 손대지 않는 것 (드물고 큰 연출 — 풀링 이득보다 복잡도가 큼)
보스 패턴 고유 오브젝트, `DeathMist`, `GlassShatter`, `ExecutionFx`, `OblivionFx`, 방 · 소품, `Afterimage`.

### 6. 측정 갱신
`Scripts/Dev/FxPoolBench.cs`에 D) "실제 풀(FxEffect.Spawn이 풀을 탈 때)" 항목 추가: 300개 Spawn → 끝까지 → 다시 300개 Spawn의 시간 · 메모리. 기존 A · B · C는 그대로 둠.

## 절대 건드리지 말 파일 (Claude가 지금 고치는 중 — 겹치면 덮어씀)
`DashTrail.cs`, `DashFx.cs`, `HeavyStep.cs`, `GlassShatter.cs`, `GroundFeet.cs`, `Ch4Bosses.cs`, `Ch4Twins.cs`, `EnemyCh4.cs`, `BladeStreak.cs`, `LaserFx.cs`, `Ch5MidBoss.cs`, `MirrorCatSkin.cs`, `Enemy.cs`, `EnemyFactory.cs`, `Scripts/Dev/WalkDemo.cs`, `Assets/Editor/` 아래 전부, 모든 프리팹 · 씬 · 에셋 파일(.prefab .unity .asset .mat .meta).
위 파일 안의 호출부를 바꿔야만 풀이 동작하는 경우엔 **바꾸지 말고**, 호출하는 쪽은 그대로 두고 받는 쪽(Spawn 함수 안)에서 풀을 타게 만들어라. 그래도 꼭 필요하면 작업 끝 보고에 "Claude가 고칠 것" 목록으로 남겨라.

## 지켜야 할 것
- 겉모습 · 타이밍 · 판정이 지금과 **똑같아야** 한다 (풀은 내부 구현만 바꿈). 공개 API(함수 이름 · 인자) 유지.
- 꺼낸 오브젝트의 상태가 이전 사용 흔적 없이 처음 만든 것과 같아야 한다 (크기 · 회전 · 색 · MaterialPropertyBlock · 부모 · 레이어 · 활성 자식 · 컴포넌트 enabled · 코루틴).
- 실행 중 할당 줄이기: 풀 경로에서 LINQ · 람다 캡처 · `GetComponentsInChildren` 매번 호출 금지(처음에 캐시).
- 섹션마다 `// 풀링 (2026-11-20)` 주석으로 무엇을 왜 바꿨는지 짧게.
- git 커밋 · 브랜치 만들지 마라.

## 끝나면 출력
1. 섹션별로 바꾼 파일과 요약
2. 되돌림(reset) 처리한 상태 목록 (컴포넌트별)
3. "Claude가 고칠 것" (금지 파일 안에서 바꿔야 하는 호출부가 있으면)
4. 위험해 보이는 곳 (확인이 필요한 부분)

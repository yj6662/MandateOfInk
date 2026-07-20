# PROGRESS.md — 오행부(五行符) 진행 관리

이 파일은 프로젝트의 **현재 상태 단일 진실(single source of truth)** 이다.
- Claude Code는 매 세션 시작 시 CLAUDE.md 다음으로 이 파일을 읽는다.
- 작업 단위가 끝날 때마다(하루치 체크리스트 항목 완료, 게이트 통과, 결정 확정 등) 이 파일을 갱신한다.
- 체크는 `[ ]` → `[x]`, 부분 완료는 `[~]`(비고에 남은 일 기재).
- 일정 기준: 개발일정 일단위체크리스트 v1.1 (주 5일, D1=착수일). 여기서는 M0~M1만 하루 단위로 펼치고, M2 이후는 게이트 통과 시 v1.1에서 펼쳐 온다.

---

## 현재 상태

| 항목 | 값 |
|---|---|
| 현재 마일스톤 | **M0 완료** → M1 진행 중 |
| 현재 일차 | 게이트 M0 통과. M1은 D9~D11·D14 선행됨 |
| 마지막 갱신 | 2026-07-19 |
| 다음 할 일 | M1 — W3 교전 동사·먹 경제(D18~D19)부터 권장 (W1·W2 상당 부분 선행됨) |
| 블로커 | 없음 |

---

## S — 선행 스파이크 (권장 · M0 이전 또는 병행)

자모 획 인식은 프로젝트 최고 위험 가정. 본 프로젝트에 붙이기 전 별도 미니 프로젝트로 검증한다.

- [x] S1. 빈 Unity 프로젝트(또는 콘솔)에서 $1 Unistroke Recognizer C# 포트 구동 — 콘솔로 검증(리포 루트 `Spike/S1_PDollarConsole`, PDollar 원본 소스 링크 + Mathf 셔밍). 동봉 16종 템플릿: 자기 분류 16/16, 교란 변형(지터 2%·크기 0.7~1.3배·회전 ±10도·점 15% 솎기) 1,580/1,600 = **98.8%**, 분류 지연 평균 **0.59ms**/p95 0.75ms. 유일한 약세는 asterisk 82/100(육각별과 혼동) — 형태가 비슷한 클래스 간 혼동 가능성 확인
- [x] S2. 초성 5자(ㄱㄴㅁㅅㅇ) 템플릿 등록 → 마우스 입력 인식률 측정 — `Assets/Spike/S2_JamoTest.unity` 플레이모드에서 실측. 템플릿 23개 등록(ㄱ5·ㄴ5·ㅁ5·ㅅ5·ㅇ3), 사용자 실기 테스트에서 정확 인식 확인(정량 수치는 별도 기록 안 함)
- [x] S3. 다획 자모(ㅁ 등) 처리 전략 검증 — 획 분할 vs $N/$P 계열 필요 판단 → **이미 $P(PDollar) 사용 중이라 다획 네이티브**. 실증: ㅁ 3획/1획(한붓쓰기), ㅅ 1획/2획 혼재 템플릿이 전부 정상 인식. 획 분할 로직 불필요
- [x] S4. 판정: 인식률·지연이 실사용 가능 수준인가 → **통과, 결정 로그 기록**. S1 합성 교란 98.8%·평균 0.59ms + S2 실기 확인. M1 게이트(외부 5인)에서 재판정 예정

## M0 — 셋업 · 프리프로덕션 (D1–D8)

- [x] D1. Unity 6·URP 프로젝트 생성, Git+LFS·.gitignore — Unity 6000.3.9f1 · URP 17.3.0. .gitignore는 Unity 프로젝트 폴더에 배치, .gitattributes(LFS)는 리포 루트에 작성
- [x] D1. 초기 세팅 — Force Text 직렬화 · Visible Meta Files · Linear 색 공간 · URP 프리셋 (전 항목 확인 완료)
- [x] D1. 폴더 구조 + asmdef 모듈 경계 확정(Core / Spellcraft / Combat / Data / Presentation / EditorTools — 런타임 코어 Odin 참조 금지) → 「프로젝트 메모」에 기록 완료
- [x] D2. 코어 패키지 도입 — VContainer **1.19.0** · UniTask **2.5.11** 설치(OpenUPM), Input System 1.18.0 기설치, Odin Inspector 임포트 완료(`Assets/Plugins/Sirenix`, 에디터/데이터 계층 한정 · 런타임 코어 참조 금지)
- [x] D2. 부트 씬 + 루트 LifetimeScope, 앱 시작 흐름(부트 → 인게임 스텁) 골격 — `Core/App/RootLifetimeScope.cs`(+DontDestroyOnLoad)·`AppEntryPoint.cs`(IAsyncStartable, UniTask 씬 전환), Boot·InGame 씬, 빌드 세팅 등록(Boot=0). 플레이모드에서 부트→인게임 전환 로그 확인 완료(2026-07-19)
- [x] D3. ScriptableObject 이벤트 채널 코어(제네릭 채널·리스너 + 에디터 디버그 표시) — `Core/Events/`: EventChannelSO<T>·VoidEventChannelSO(+ContextMenu 디버그 발신·발신 카운터)·리스너 2종(UnityEvent 반응)
- [x] D3. 핵심 서비스 인터페이스 정의·DI 등록 — `Core/Services/`: ISceneLoadService(실구현)·ISaveService·ISoundService·IPoolService(스텁), RootLifetimeScope에 싱글턴 등록
- [x] D3. 부트 흐름 검증(싱글턴 없이 DI·이벤트 채널로만 연결) — AppEntryPoint가 ISceneLoadService를 생성자 주입으로 받아 씬 전환. 플레이모드 [SceneLoad] 4단 로그 확인 완료(2026-07-19)
- [x] D4. 1인칭 컨트롤러(WASD·마우스 시점) + 기본 카메라 — **StarterAssets FirstPerson 활용**(사용자 제안·합의). InGame 씬에 NestedParent_Unpack 배치(언팩 완료)·프로토 바닥(Plane 50m)·기본 카메라 제거·모바일 조이스틱 UI 삭제. 플레이모드 WASD·마우스룩 검증 완료(2026-07-19)
- [x] D5. 작도 키 홀드 상태머신(교전↔작도) + 뷰모델 별도 카메라·레이어 — 상태머신·시간 감속 구현: `Spellcraft/SpellcraftModeController`(Combat↔Drawing, timeScale+fixedDeltaTime 감속·복원), `Data/SpellcraftModeConfigSO`([가정] Q 토글·0.2배), `EC_DrawingModeChanged`(bool 채널) → ProtoGlue가 StarterAssets 입력·커서 잠금/해제 중계. InGame 배선 완료. 모드 전환·감속·커서 플레이 검증 완료. 뷰모델: ViewModel 레이어(6) 신설, MainCamera 컬링 제외, ViewModelCamera(URP Overlay, FOV 50 [가정], near 0.01) 스택 등록, 대필 자리표시자 큐브([가정] W2에서 교체). 자리표시자 표시·추적 플레이 확인 완료(2026-07-19)
- [x] D6. 데이터 스키마 — 도면 DTO·상극 5×5(SerializedScriptableObject + Dictionary)·적 DTO·강화 DTO — `Data/`: Element·Polarity·FinalModifier enum, EffectParam 추상 다형성(Damage/Shield/Dot), SpellDiagramSO(글자·자모·파생속성·먹비용·효과), ElementRelationTableSO(중첩 Dictionary + GetMultiplier), EnemyDefinitionSO, UpgradeDefinitionSO(마석 비용 — 화폐 아님 명시). `ElementRelationTable.asset` 생성·5×5 기본값([가정] 우세 1.5/열세 0.5/중립 1.0) 채움·조회 검증(수→화 1.5, 금→화 0.5, 목→목 1.0). Odin 직렬화 파일 저장 확인
- [x] D7. 글자 일람 v1.2 → 도면 DTO 임포트(40장), 로드·조회 검증 — **120장 전체 임포트**(요구 40장의 상위집합). PDF→CSV 추출(`Assets/Docs/오행부_작도글자일람_v1_2.csv`, 120행 원문 대조 100% 일치), EditorTools `SpellDiagramCsvImporter`(메뉴·멱등, Effects 수동 튜닝 보존) → `Data/Diagrams/SD_*.asset` 120개 생성, 조회 검증(나=화·양·단일·공격). 주의: 일람 v1.2 축 기준으로 스키마 수정 — 광역은 중성(천지인 Scope), 종성=속박/지속/격발/관통/연쇄
- [x] D8. 플레이스홀더 — 졸개 적(캡슐+텔레)·대필 막대·회색 아레나 — 더미 적(EN_TestDummy 금속성) + EnemyAI 명시적 상태머신(대기→추적→텔레그래프[몸 붉어짐, 시간 감속 영향]→공격→회복, 수치는 SO), PlayerHealth+PlayerConfigSO(HP HUD — 허용 항목), 회색 아레나 벽 4면(M_Proto_Wall), 대필 막대는 D5 자리표시자. GAP VFX·시전→격파 파이프라인 포함

**게이트 M0**: [x] **통과(2026-07-19)** — 아레나에서 1인칭 이동 + 원거리 텔레그래프 적 교전 + 작도(이어 그리기 인식)/교전 모드 전환 전부 작동. 싱글턴 없음 — VContainer DI + SO 이벤트 채널로만 연결. 사용자 플레이 확인.

## M1 — 프로토타입: 작도 손맛 + 룩 (D9–D28, 최우선)

### W1 — 작도 인식 코어
- [~] D9. 작도 평면 + 마우스 투영, 날것 점 궤적 수집·시각화 — M0에서 선행: DrawingInputController가 화면 좌표 궤적 수집 + ViewModel 레이어 먹선 표시(카메라 추종). 작도 평면·투영 정식화는 M1에서
- [~] D10. $1류 제스처 인식기 통합(스파이크 결과 반영) + 초성 5 단독 인식 — M0에서 선행: PDollar에 asmdef 부여(Spellcraft 참조), 템플릿 23개를 _Project/Data/JamoTemplates로 이관·런타임 로드, 초성 인식→ㅏ 기본형 도면 매핑([가정] 임시 규칙)→EC_DiagramDrawn 채널→시전→모드 자동 해제. 인식 품질·정식 통합은 M1에서 재검
- [~] D11. 중성 4(ㅏㅓㅗㅜ) 인식 + 초·중 결합 20장 — M0에서 선행: 획 그룹 분할 인식(초성 우선 쓰기 규칙 이용, 부분별 독립 정규화 — 위치·침범 무관) + F2 중성 실필기 등록. 사용자 체감 「굉장히 좋아짐」(2026-07-19). 정량 측정·정식화는 M1에서
- [ ] D12. 받침 ㅁ 인식(40장) + 도면 DTO 조회 → 진 식별 연결
- [ ] D13. 획 품질 3축 스코어 + 폴백(약발동) + 먹선/날것 점 분리 확인

### W2 — 작도 뷰모델(대필·석경)
- [~] D14. 대필 자루끝 축 겨누기 회전 + 정적 손 — M0에서 선행: BrushViewModel(교전 대기 자세 <-> 작도 시 붓끝 커서 추종, Lerp 무게감) + Meshy로 대필 모델 생성 파이프라인 첫 가동. 겨누기 회전·손 모델은 M1에서
- [ ] D15. 스프링/스무딩 무게감 + 붓끝 먹선 TrailRenderer
- [ ] D16. 모드 전환 자세 디제틱(붓끝 작도 ↔ 자루끝 평타)
- [ ] D17. 근평면·벽 클리핑 + 깊이·크기감 1차 튜닝

### W3 — 교전 동사 + 먹 경제
- [ ] D18. 회피(무적 프레임) + 클릭 평타
- [ ] D19. 단일 먹 풀 — 트리클 + 교전 충전 + 비상 수동 갈기
- [ ] D20. 디제틱 잔량 + 부적(즉발·제한 수량)
- [ ] D21. 졸개 텔레그래프(느린/빠른/페인트, 0.8~1.2초)
- [ ] D22. 적 피해·체력·기본 AI 상태머신 + 한 합 검증

### W4 — 받아치기 · 그로기 · 상합 + 룩
- [ ] D23. 받아치기(선제 상극) + 속성 3단계 판정
- [ ] D24. 막 받아치기(~0.5초 창) 보너스 + 먹 환급
- [ ] D25. 그로기 누적·감소 + 급소 창 폭딜
- [ ] D26. 그로기 표시 — 락온 레티클 + 본체 디제틱 텔
- [ ] D27. 상합(격발) — 음+ㅁ 설치 → 양 격발
- [ ] D28. 수묵 룩 프로토타입 + 먹 VFX 기초 + 게이트용 빌드 + 외부 5인 테스트 준비

**게이트 M1**: [ ] 외부 5인 손맛·룩 긍정 / 자모 인식률·좌절 임계 통과 / 0.8~1.2초 내 2획 상극을 0.5초 창에 / 먹 자기조절·모드 가독성 체감.

## M2 이후 (요약 — 게이트 통과 시 v1.1에서 하루 단위로 펼쳐 옴)

- [ ] M2. 수직 슬라이스 — 황경 처음부터 끝까지 (D29–D53)
- [ ] M3–M4. 압축 프로덕션 — 2강토·종성 전체·상극 5×5·메카천수관음 (D54–D93)
- [ ] M5. 알파·데모 — 스팀 페이지·넥스트 페스트·위시리스트 2만 목표 (D94–D113)
- [ ] M6. 베타·폴리시·출시 (D114–D133)

---

## 결정 로그

확정된 결정만 기록한다. [제안] 단계는 「미결 사항」에 둔다.

| 날짜 | 결정 | 근거/비고 |
|---|---|---|
| 2026-07-19 | 약발동(A안) 채택 — 임계 미달 시 최근접 글자를 위력 50%로 발동, 완전 불발 금지 유지. 정발동=먹선 글로우 소멸/약발동=무효과 서서히 소멸. 작도 입력 = Q 토글 -> **홀드**(누른 채 그리고 떼면 판정) | 사용자 결정. 임계·배율은 SpellcraftModeConfigSO([가정] 거리 1.8·배율 0.5) |
| 2026-07 | 아키텍처: VContainer DI + SO 이벤트 채널 + 상태머신 + asmdef, "The Last One" 폐기 | 체크리스트 v1.1 반영 |
| 2026-07 | Odin: 에디터/데이터 계층 한정, 런타임 코어 독립. SerializedScriptableObject·Dictionary 직렬화·OdinMenuEditorWindow 패턴 | Personal 라이선스, Validator 스킵 |
| 2026-07 | 분기: 순수 플래그(필수/금지) + 카운터 2종. 원장 CSV = 단일 진실 | 분기 시스템 사양 v0.3 |
| 2026-07 | 문서 체계: 사람용 PDF 단독 / 개발용 MD·CSV | zip 방식 대체 |
| 2026-07-19 | 생성형 AI 스택 확정: 3D=**Meshy** / 2D·UI=**Recraft** / 사운드·음성=**ElevenLabs + SuperTone** (Hyper3D·Suno 미채택) | 사용자 결정. 키는 Tools/.env(gitignore), 4종 인증 검증 완료 — ElevenLabs는 TTS 전용 제한 키(관리용 읽기 권한 없음, 사용엔 지장 없음) |
| 2026-07-19 | 1인칭 컨트롤러 = **StarterAssets FirstPerson 채택(프로토타입 한정)** | 사용자 제안. asmdef 없이 Assembly-CSharp 소속 — 우리 모듈에서 직접 참조 금지, 플레이스홀더 취급. D5 작도↔교전 전환 통합 시 재평가 |
| 2026-07-19 | 작도 모드 = **키 진입 + 시간 감속, 작도 완료 시 해제·시간 복원** | 사용자 결정. D5 골격은 같은 키 토글로 해제, M1에서 자모 인식 성공 이벤트가 해제를 대신. 감속 배율 등 수치는 SpellcraftModeConfigSO([가정] 0.2배) |
| 2026-07-18 | 작도 인식기 = PDollar **$P Point-Cloud Recognizer** 채택. 다획 자모는 획 분할 없이 $P 네이티브 처리 | S 스파이크 통과 — S1 합성 교란 98.8%·0.59ms, S2 실기 마우스 정확 인식, S3 ㅁ(1·3획)/ㅅ(1·2획) 혼재 인식. M1 게이트에서 외부 테스트로 재검증 |

## 미결 사항 (임의 결정 금지)

- [ ] 산군 배치: 인왕산 1차 vs 청림 완전판
- [ ] 착호 병과 채택 여부
- [ ] 마물 이중 명칭(관보식/민간) 채택 여부
- [ ] 황경·조정 톤 방향(신(信) 변질, 인정·파루·순라 앵커) — 제안 단계
- [ ] 무학대사 정체
- [ ] 황경 보스 「개천 이무기」 채택 여부 — [제안]

## 문서 정비 잔무

- [x] CLAUDE.md 종성·중성 절 v2.0 개정(2026-07-19) — 광역=천지인, 종성=속박/지속/격발/관통/연쇄, 예시 교체
- [ ] 게임기획서 v1.4 §8 구 아키텍처 언어 정리(차기 개정 시)
- [ ] v1.0 개발 체크리스트 PDF를 프로젝트 지식베이스에서 제거

## 리스크 감시

| 리스크 | 상태 | 대응 |
|---|---|---|
| 자모 획 인식 실패(최고 위험) | S 스파이크 통과(2026-07-18) — 1인 검증 | M1 게이트에서 외부 5인·중성/받침 확장으로 재판정 |
| Suno 산출물 저작권 불확실성 | 상존 | 출시 전 사람 검토, 유료 상업 이용권 유지 |
| 셰이더 에셋 URP Render Graph 비호환 | 미확인 | 구매 전 에셋별 호환 확인 |
| ~~프로젝트 URP 파이프라인 에셋 부재~~ | 해결(2026-07-19) | `_Project/Settings/URP_PipelineAsset` 지정 완료 — 삭제 금지 |

## 프로젝트 메모 (작업하며 갱신)

- 리포 구조: Git 루트 = `C:\Users\yj666\MandateOfInk`, Unity 프로젝트 = 하위 `MandateOfInk/`. `.gitattributes`(LFS)는 루트, `.gitignore`는 Unity 프로젝트 폴더.
- 폴더 구조: 프로젝트 고유 자산은 전부 `Assets/_Project/` 아래(서드파티와 분리). 하위: `Art / Audio / Data(SO 에셋 인스턴스) / Prefabs / Scenes / Settings / UI / VFX / Scripts`. `Scripts` 하위 = asmdef 모듈 6종: `Core / Data / Spellcraft / Combat / Presentation / Editor(EditorTools)`. 서드파티(PDollar·GabrielAguiarProductions·Plugins 등)는 `Assets/` 루트 유지.
- asmdef 의존 방향(런타임): Core ← Data ← Spellcraft ← Combat ← Presentation (상위가 하위 참조, 역참조 금지). EditorTools는 Editor 전용 플랫폼으로 전 모듈 참조 가능 — Odin 참조는 EditorTools(및 추후 데이터 SO 계층)에만 허용, 런타임 코어 금지.
- 네이밍 규칙: 어셈블리·네임스페이스 `MandateOfInk.<모듈>`(예: MandateOfInk.Spellcraft). 클래스·메서드·프로퍼티 PascalCase, private 필드 `_camelCase`, 로컬·매개변수 camelCase. SO 에셋 파일명 PascalCase, 이벤트 채널 SO는 `EC_` 접두(예: EC_PlayerDamaged) — [가정] 필요 시 조정.
- 루트 `CLAUDE.md`(Unity 프로젝트 폴더)는 `Assets/Docs/CLAUDE.md`를 @임포트하는 연결 파일. 지침 원본은 Assets/Docs에 유지.
- 빌드·테스트 명령: (확정 후 CLAUDE.md에도 반영)

## 세션 로그

최신이 위. 형식: 날짜 / 한 일 / 다음 할 일 / 특이사항.

| 날짜 | 한 일 | 다음 할 일 | 특이사항 |
|---|---|---|---|
| 2026-07-19 | 설계 문서 6종 정독(서브에이전트) — 전투·먹 경제·상합 원 사양 확보, CLAUDE.md 종성/중성 절 v2.0 개정(사용자 승인). 진 효과 1차 실작동: 분류x범위 디스패치(ㅏ=투사체/ㅗ=팽창 파동/ㅓ=전방 막/ㅜ=광역 돔), 반투명 프리미티브+속성색+인식 글자 텍스트(획 원본 텍스처는 M1 폴리시), 방어막의 적 투사체 차단, 상합은 D12 후 스텁 | 플레이 검증 -> 커밋 | VFX 에셋 미사용 표현(사용자 지시). 격발용 5x5 매트릭스가 추후 별도 필요(문서) |
| 2026-07-19 | 인식·판정 개선 묶음 — JamoMatcher(원시 거리, 점수 포화 제거), 거울상 중성 기하 판별(ㅏㅓ/ㅗㅜ), 분할 구조 검증(직선 중성 배제 — 나→모·누·소 오인식 해소), 약발동(A안: 최악 자모 거리>1.8 [가정] 시 위력 50%·작은 투사체), Q 홀드 방식(떼면 판정), 소멸 연출(정발동 글로우/약발동 무효과 페이드) | 커밋 완료. M1 W3 착수 대기 | 「너」·「나」 오인식 사용자 검증 통과. ㅣㅡ 특수 중성 도입 시 직선 배제 검증 재설계 필요(주석 기록) |
| 2026-07-19 | **게이트 M0 통과·마일스톤 종료.** 번짐 +20%(1.62), 속성별 공격 색(ElementPaletteSO — [가정] 오방색 조정판, VFX 런타임 틴트). 전체 일괄 커밋 | M1 착수 — W3(회피·평타·먹 경제) 권장 | 먹선 룩 사용자 승인(「정말 괜찮아졌어」) |
| 2026-07-19 | 붓자국 텍스처를 Recraft로 생성 — 3차 시도 끝에 유효 4장 선별(조류·붓 오염물 제외), PCA 수평 보정·알파 변환·커버리지 정렬 아틀라스(`_Project/Art/Ink/T_InkStrokeAtlas.png` 4행). InkStroke를 스트레치 UV(획=텍스처 1장, 기필·수필 내장)·획당 행 선택(먹 잔량+무작위)으로 전환. 굵기 0.018·순흑 반영 | 룩 체감 확인(사용자) | 프롬프트 교훈: "flying white"는 새로, "brush"는 붓 사진으로 오해됨 — 원본 후보는 Tools/generated에 보존 |
| 2026-07-19 | 먹선 붓글씨 질감 — InkStroke 절차 메시(LineRenderer 대체): 점별 굵기(속도+노이즈)·정점 농도(속도+먹 소모)·비백 아틀라스(6단 마름, 코드 생성)·기필 눌림·수필 빼기·글자 단위 먹 소모(뒤 획 갈필)·번짐. Lerp 8로 무겁게. 파라미터 전부 인스펙터 [가정] | 룩 체감 확인(사용자) | 사용자 제공 서예 레퍼런스 기준. 셰이더 저작 없음(정점색+생성 텍스처) |
| 2026-07-19 | 허수아비 원거리형 전환(9m 정지·텔레그래프 후 무유도 투사체 — 회피 가능), Meshy 파이프라인 첫 가동: 대필 FBX+텍스처 생성·임포트·BrushRig 조립(크기 정규화 0.35m·ViewModel 레이어), BrushViewModel(교전 대기<->작도 커서 추종), 먹선 투영 카메라 VM 통일 | 플레이 확인(붓 방향 포함) → 게이트 M0 판정 | 붓끝이 반대로 보이면 SM_Brush의 Y회전 -90 -> 90 |
| 2026-07-19 | 인식 방식 개선: 합성 글자 템플릿 → **획 그룹 분할 인식**으로 교체(모든 분할점 시도, 부분 독립 정규화, 초성/중성 후보군 분리) — 침범·초성 오인식 해소, 사용자 체감 대폭 개선. API 키 4종(Meshy·Recraft·ElevenLabs·SuperTone) 인증 검증 | 일괄 커밋 → D8 잔여 | 자모 사이에는 펜을 떼야 함(이어 쓰면 분할점 없음) |
| 2026-07-19 | 이어 그리기 인식(합성 글자 템플릿, 배치 2종·변형 16/클래스), 중성 실필기 등록 모드(F2, 1~4 저장·즉시 재구성), 획 굵기 0.006, 좌클릭 임시 시전 제거, 투사체 접촉 즉시 소멸+VFX 3초 상한(최적화), 먹선 카메라 로컬 공간(화면 추종), API 키 스캐폴딩(`Tools/.env.example`+gitignore — Meshy·Hyper3D·Recraft·Suno·ElevenLabs) | 인식률 재체감 → 일괄 커밋 | 종성 확장은 종성 템플릿 등록 후 동일 조합 방식 |
| 2026-07-19 | 먹선 스타일(붓끝 Lerp·속도 굵기·번짐 애니·소프트 엣지 텍스처 코드 생성, 전 파라미터 [가정] 인스펙터 노출), 락온 떨림 수정(피치 로컬 회전 + 지수 감쇠 — 월드 회전 피드백 제거), 2단계 작도(초성→중성, 합성 글자 발동). 중성 템플릿 ㅏㅓㅗㅜ 합성 12개 생성(추후 실필기 교체 가능) | 통합 플레이 검증 → 일괄 커밋 | 종성 확장은 M1 D12 |
| 2026-07-19 | 렌더 파이프라인 복구 — GraphicsSettings에 URP 미지정 상태 발견, `_Project/Settings/URP_PipelineAsset`+`URP_Renderer` 생성·Graphics/품질 6레벨 지정. 갈색 바닥·붓대(먹빛)·허수아비(회색) URP 머티리얼. 작도 시전 파이프라인(초성→ㅏ기본형 매핑), 락온(Tab·범위 20m [가정]), 작도 중 이동 허용(시점만 잠금) | 통합 플레이 검증 → 커밋 | 프리미티브 생성 시 기본 머티리얼은 Built-in이라 URP에서 마젠타 — 프로토 머티리얼로 교체할 것 |
| 2026-07-19 | D3 플레이 검증 완료(4단 로그 확인)·커밋 00819b0. D4 — StarterAssets FPC 채택, InGame 씬 배선(플레이어 프리팹+바닥, 기본 카메라 제거), 카메라 스크린샷 확인 | D4 플레이 검증 → 커밋 → D5 | 에디트 모드 스크린샷의 카메라 위치는 Cinemachine 작동 전 상태 — 정상 |
| 2026-07-19 | D2 완료 확인(부트 플레이 검증·Odin 임포트, 커밋 9e24011) + D3 구현 — SO 이벤트 채널 코어(제네릭+Void, 리스너, 에디터 디버그), 서비스 4종 인터페이스·스텁 DI 등록, AppEntryPoint 생성자 주입 전환. 컴파일 에러 0 | 플레이 재검증 후 D3 커밋 → D4 | 리포는 private — Odin 커밋 가능 확인 |
| 2026-07-19 | M0 D2 구성 — VContainer 1.19.0·UniTask 2.5.11 설치, Core asmdef 참조 추가, RootLifetimeScope·AppEntryPoint 작성, Boot/InGame 씬 + 빌드 세팅 배선. 스파이크 커밋(89a4c3a) | 부트 흐름 플레이 검증·Odin 임포트(사용자) → D3 | MCP 도구가 도메인 리로드를 사이에 두면 응답이 끊기는 이슈 — 패키지 설치·스크립트 컴파일 후에는 에디터 재시작이 가장 확실. RootLifetimeScope 컴파일 누락도 재시작으로 해소 |
| 2026-07-18 | S2~S4 완료 — 실기 마우스 테스트 정확 인식(사용자 확인), 템플릿 23개(`Assets/Spike/Templates/`), 다획 네이티브 실증. $P 채택을 결정 로그에 기록 | M0 D2 — VContainer·UniTask·Input System 순 도입 후 Odin | 스파이크 폴더(Assets/Spike, Spike/)는 M1 W1에서 본 시스템 이식 후 정리 예정. 템플릿 XML은 M1 D10 재사용 가치 있음 — 보존 |
| 2026-07-18 | Unity MCP 로컬 모드 연결 확립(클라우드 OAuth 불가 → bootstrap-local). S2 준비 완료 — `Assets/Spike/S2_JamoRecognitionTest.cs` 작성, `Assets/Spike/S2_JamoTest.unity` 씬 구성(카메라·라이트·컴포넌트 부착), 컴파일 에러 0 | 사용자가 플레이모드에서 ㄱㄴㅁㅅㅇ 템플릿 등록(클래스당 3~5개) 후 테스트 모드로 인식률 측정 | 플러그인 설정상 playmode 제어 도구 비활성 — 플레이 진입은 사용자가 직접 |
| 2026-07-18 | S1 완료 — PDollar($P) 콘솔 구동 검증(Spike/S1_PDollarConsole). 16종 템플릿 자기 분류 16/16, 교란 인식률 98.8%, 지연 평균 0.59ms | S2 — 초성 5자 템플릿 + 마우스 입력 인식률 | Unity MCP(ai-game-developer) 미인증 상태 — `npx unity-mcp-cli login` 필요. $P는 회전 비불변이므로 자모처럼 방향이 의미 있는 글자에 유리 |
| 2026-07-18 | M0 D1 완료 — 초기 세팅 확인(Force Text·Visible Meta·Linear·URP), .gitattributes(LFS) 작성, Assets/_Project 폴더 구조 + asmdef 6종 생성, 루트 CLAUDE.md 연결, 초기 커밋 | S1 스파이크 검증 또는 D2(VContainer·UniTask·Input System → Odin) | PDollar($P 인식기)가 이미 Assets에 임포트되어 있음 — S 스파이크를 본 프로젝트 내에서 바로 진행 가능. Unity 에디터에서 다음 열기 시 asmdef 컴파일 확인 필요(빈 어셈블리 경고는 무해) |
| 2026-07-18 | CLAUDE.md 갱신, PROGRESS.md 신설 | S1 착수 여부 결정 → M0 D1 | — |

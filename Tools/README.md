# Tools — 생성형 AI 파이프라인 작업 폴더

에셋 생성(3D·2D·음악) 자동화 스크립트와 API 키 설정을 두는 곳이다. Unity 프로젝트 밖이라 에디터가 임포트하지 않는다.

## API 키 설정

1. `.env.example` 을 복사해 같은 폴더에 `.env` 를 만든다.
2. 각 서비스에서 발급받은 키를 채운다 (유료 플랜 상업 이용권 확인 후).
3. `.env` 는 gitignore 되어 있다 — **키를 커밋하지 않는다.**

| 키 | 서비스 | 용도 |
|---|---|---|
| MESHY_API_KEY | Meshy | 3D 모델 생성 (주력) |
| HYPER3D_API_KEY | Hyper3D | 3D 모델 생성 (후보) |
| RECRAFT_API_KEY | Recraft | 2D·UI 이미지 |
| SUNO_API_KEY | Suno | 음악 (출시 전 사람 검토 필수) |
| ELEVENLABS_API_KEY | ElevenLabs | 효과음 — [제안] 단계 |

비용 가드: 각 서비스에서 요율 제한을 걸고 자동 충전은 꺼 둔다 (CLAUDE.md 규칙).

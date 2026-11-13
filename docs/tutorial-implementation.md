# 튜토리얼 구현 명세

## 진입 및 기존 코드와의 관계

`AISceneTrigger.Start()`는 `useTutorial`이 켜져 있으면 `TutorialController`를 추가하고 기존 AI/1막 흐름을 시작하지 않는다. `SampleScene`은 이 값을 명시적으로 활성화한다. `useTutorial=false`일 때 기존 `useOfflineActOne` 분기가 동작한다. 이전 작업의 미커밋 변경을 유지했다.

튜토리얼은 `TutorialView`가 만든 별도 런타임 Canvas(sort order 100)를 사용한다. 기존 장면 오브젝트·모듈·SO 이벤트 연결을 제거하거나 바꾸지 않는다. 기존 `SceneDirective` 기반 모듈은 이전 모드에서 유지된다. 새 화면에는 별도 그림 에셋이 필요하지 않다.

후속 UI 개편에서 대화 화면, 종이색 모달 기록장, 설정 메뉴를 분리했다. 수집 알림과 입력창 위 근거 첨부, 분류 필터, 모달 포커스 차단, 새 대화에만 스크롤 갱신을 적용했다. 자세한 화면 구성과 편집기 이미지 출력 도구는 [UI 문서](tutorial-ui.md)에 기록한다.

가독성 보정: 버튼·입력창·토글의 기본/호버/선택/누름/비활성 색상을 모두 어두운 색으로 명시한다. 밝은 글자 위로 기본 흰 상태색이 적용되지 않도록 `SetDarkControlColors`에서 상태색을 통일한다. 입력 선택 영역은 반투명 청색이며 캐럿과 안내 문구의 대비를 높였다. 이는 상태색 대비 보정이며 폰트 아틀라스/머티리얼 문제의 시각적 진단을 대신하지 않는다.

폰트 보정: 기존 `NanumGothic SDF`는 원본 참조가 비어 있고 padding 0이다. 이전 코드는 이 경우 동적 폰트 생성을 건너뛰었다. `AISceneTrigger.tutorialSourceFont`에 `NanumGothic.ttf`를 직접 직렬화하고 Controller→View로 전달한다. View는 SDFAA, 64pt, padding 9, 1024² 동적 멀티 아틀라스 폰트를 생성하고 각 TextMeshPro에 그 폰트의 전용 머티리얼을 지정한다. 공유 원본 SDF 에셋은 변경하지 않는다. 에디터 Console의 `[Tutorial] 폰트:` 로그로 새 경로가 적용됐는지 확인한다. 실제 흰 사각형 해소 여부는 Game 화면에서 확인해야 한다.

텍스트 뷰어 후속 변경: 중앙은 최신 장면 하나를 타이핑하고 긴 본문은 페이지로 나눈다. 표시 종료 후 선택지와 직접 입력 버튼을 노출한다. 하단 기록장/소지품/로그/설정, 속도/즉시 표시, 모달 일시정지와 같은 프레임 넘김 방지를 추가했다. TextReveal.cs는 시간 누적을 관리하는 순수 C# 모델이다. 자세한 현재 동작은 UI 문서를 우선한다.

## 파일별 책임

| 파일 | 책임 |
|---|---|
| TutorialCore/TutorialSession.cs | 장소/행동, 기록 정의, 연결 조합, 제시 결과, 진행 조건, 대화 기록 |
| TutorialCore/TutorialSaveStore.cs | 튜토리얼 전용 버전1 JSON 저장, 임시 파일 교체, 백업 복구 |
| TutorialCore/TutorialAIReply.cs | API 외피/내부 JSON 파싱, 잘린 응답·알 수 없는 행동·빈/과대 대사 거부 |
| TutorialCore/Game.Tutorial.asmdef | UnityEngine 비의존 코어 어셈블리, 기본 어셈블리에서 참조 가능 |
| TutorialController.cs | UI 명령, AI 요청 구성과 검증, 상태 적용, 저장, 로그 |
| TutorialView.cs | 1440×900 기준 대화 화면, 모달 기록장/메뉴, 수집 알림, 수단 첨부/제시, 입력 및 잠금 |
| TutorialCore/GamebookContent.cs | 원문을 유지하며 짧은 상황 설명과 대사 본문을 분리, 긴 도입부는 페이지 본문 유지 |
| TutorialPortraitCatalog.cs | 인물 이름과 교체 가능한 Sprite를 연결하는 ScriptableObject |
| Assets/Resources/UI/TutorialPortraits.asset | 기존 고양이 이미지로 구성한 기본 인물별 초상화 매핑 |
| SoftPortrait.cs | UI 메시로 초상화 비율 유지 및 가장자리 알파 페이드, 원본 텍스처 수정 없음 |
| Assets/Editor/TutorialUIPreview.cs | 실제 저장/API 없이 별도 Preview Scene에서 대화·기록장 이미지 출력 |
| AIRequestService.cs | 기존 요청 호환 래퍼, 커스텀 구조화 응답 스키마, 45초 타임아웃, 오류 콜백, 요청 Dispose |
| Assets/Tests/Editor/TutorialTests.cs | NUnit EditMode용 코어/저장/AI 파싱 테스트 |
| Tools/Test-Tutorial.ps1 | 같은 NUnit 테스트를 Unity 내장 Mono로 독립 컴파일·실행 |
| Tools/Tests/TutorialTestRunner.cs | 독립 실행용 Test 메서드 실행기. Unity 에디터/PlayMode 테스트 결과와는 별개 |

모든 새 Assets 파일·폴더에는 `.meta`를 제공한다. 에디터 작업 파일, 실행 결과, 테스트 저장은 `Logs/`에 남으며 Git에서 제외한다.

## 데이터

`TutorialProgress`는 `version`, `location`, `records`, `tools`, `flags`, `transcript`, `lastText`, `notice`를 저장한다. 세 집합은 ID 중복을 방지한다. `TutorialRecord`는 종류, 제목, 출처, 내용이 있는 작성된 정의다. 정의는 코드에 있고 획득 여부만 저장한다.

진행 플래그:

| 플래그 | 설정 조건 |
|---|---|
| ate | 아침 식사 |
| accepted | 길드 의뢰 수락 |
| door_identified | 배달원에게 오배송 가능성 제시 |
| recovered | 가게에서 주문 일치 근거 제시 |
| reported | 여관에서 결과 전달 |
| completed | 보고 후 취침 |

`Perform`은 현재 `Actions()`에 있는 ID만 적용한다. `Connect`는 실제 획득한 서로 다른 기록 두 개만 받는다. `Present`는 등록된 수단과 현재 장소를 확인한다. 반복 회수·중복 보고·조기 취침 등은 허용된 행동 목록에 나오지 않거나 제시 단계에서 거부된다.

## 자유 입력과 사실 분리

1. 입력 길이/전송 가능 여부 확인. AI는 기본 OFF이고 키를 저장하지 않는다.
2. 현재 인물, 장소, 획득 기록, 등록 수단, 가능한 행동을 프롬프트에 넣는다. 미래 반전 설명은 전달하지 않는다.
3. 같은 장소의 최근 8개 대화만 전달한다. 입력은 발언, 첨부 기록 ID, 첨부 수단 ID로 직렬화한다.
4. AI는 `reply`, `actionId`만 반환한다. 행동은 현재 가능한 ID 및 등록 수단 `present:<id>`, 또는 빈 문자열로 제한한다.
5. 응답을 검증한 뒤 적용한다. 허용된 행동이면 작성된 결과를 표시한다. 빈 행동이면 대사만 기록하며 증거/플래그를 바꾸지 않는다.
6. 저장 후 UI 갱신. 오류 시 입력을 유지하고 잠금을 해제한다.

원시 사용자 문장으로 플래그를 만들거나 증언을 사실로 확정하지 않는다. AI가 새로운 증거, 수치, 세계 규칙을 발급하지 못한다. 출력 대사 자체의 사실 일치/자연스러움은 모델 의존이므로 실제 대화 QA가 필요하다. 엄격한 사실 검증은 대사 문자열 전체에 적용되는 것이 아니라 코드 상태 변경 경계에 적용된다.

기존 API 서비스의 수동 JSON 문자열 조립을 Newtonsoft 직렬화로 교체해 따옴표·탭·줄바꿈을 처리한다. 기존 `SendConversation` 호출도 유지한다. 오류 시 응답 본문이나 키를 로그에 출력하지 않는다. 에디터 외 직접 호출은 실패 콜백으로 종료하며 배포용 프록시는 아직 없다.

## 실패와 저장

튜토리얼 저장은 기존 `SaveData` v2 및 `GameState`를 바꾸지 않는 별도 슬롯이다. 임시 JSON 작성 후 `File.Replace`로 교체하며 첫 저장은 이동한다. 읽기 실패 시 `.bak`을 시도한다. 미래 버전/잘못된 장소/빈 JSON은 거부한다. 두 파일 모두 실패하면 UI를 잠그고 명시적 새 시작을 기다린다. 저장 쓰기 실패는 화면/Console에 알리고 현재 메모리 상태를 유지한다.

동시 입력은 busy 잠금으로 차단한다. 요청 시작 시 revision을 캡처하고 오래된 응답은 적용하지 않는다. 네트워크 실패, 45초 타임아웃, 잘린 응답, 허용되지 않은 행동, 빈 대사/1200자 초과 응답을 거부한다. 재전송 전까지 실패 입력은 남는다. 수동 취소, 백그라운드 재시도, 요금 추적은 미구현이다.

## 확장 지점과 제한

현재 사건과 연결은 코드로 작성되어 있다. 후속으로 ScriptableObject 카탈로그, 단서 그래프, 더 많은 유효 연결, NPC별 신뢰/주장 기억을 추가할 수 있다. 지금은 허기/전투/골드 수치, AI 생성 추천 선택지, 월드 삭제, 첫 적대 조우, 회귀를 새 튜토리얼에 연결하지 않았다. 기존 1막 초안의 상대 회귀 기억 설정은 이 튜토리얼로 가져오지 않는다.

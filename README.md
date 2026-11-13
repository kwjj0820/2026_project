# 2026 Project — 대화형 텍스트 RPG

Unity **6000.3.6f1** 기반 개인 프로젝트입니다. 자유로운 말·행동 입력과 증언·증거의 수집, 연결, 활용을 중심으로 개발합니다.

## 현재 플레이 범위

`Assets/Scenes/SampleScene.unity`를 열고 Play를 누르면 새 튜토리얼 화면이 생성됩니다.

**여관의 아침 → 길드 의뢰 → 배송 조사 → 기록 연결 → 근거 제시 → 물건 회수 → 여관 복귀 → 취침**까지 진행할 수 있습니다. 검은 처음부터 소지한 평범한 장비입니다. 삭제 UI와 본편 첫 적대 조우는 아직 구현하지 않았습니다.

중앙 본문이 타이핑된 뒤 선택지와 ‘직접 입력하기’가 나타납니다. 클릭/Space로 즉시 완성하거나 다음 페이지로 이동합니다. 하단에는 기록장·소지품·대화 로그·설정이 있습니다. API 없이 행동 버튼과 기록장으로 완료할 수 있습니다. 하단 **기록장**에서 정보를 연결하고 근거를 선택해 대화에 첨부합니다. 자유 채팅은 **설정 → AI 자유 대화**를 직접 켜야 합니다. 에디터에서만 지원하며 실제 전송에는 Anthropic API 비용이 발생합니다. 키는 사용자 환경 변수 `ANTHROPIC_API_KEY`로 설정하고 Unity Hub와 Editor를 다시 시작하세요. 키를 파일이나 채팅에 붙여넣지 마세요.

## 문서

현재 화면은 NPC 초상화가 있는 게임북형 UI입니다. 상황 설명과 대사를 순서대로 타이핑하고 회색 선택지를 세로로 표시합니다. 초상화는 기존 밈 고양이를 임시 사용하며 `Assets/Resources/UI/TutorialPortraits.asset`에서 인물별로 교체할 수 있습니다.

- [튜토리얼 실행·테스트 안내](docs/tutorial-playtest.md)
- [설정·사건·정보 설계](docs/tutorial-design.md)
- [구조·데이터·AI·저장 명세](docs/tutorial-implementation.md)
- [개편 UI와 기록장 사용법](docs/tutorial-ui.md)
- [게임북 UI 구현·검증 기록](devlog/2026-10-16-gamebook.md)
- [개발 변경 및 검증 기록](devlog/2026-10-16-tutorial.md)
- [기여 가이드](AGENTS.md)

## 검증

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Test-Tutorial.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Test-ActOne.ps1
```

첫 명령은 실제 NUnit 테스트를 Unity 내장 Mono로 독립 실행합니다. Unity에서도 **Window → General → Test Runner → EditMode → TutorialTests**로 실행할 수 있도록 테스트 어셈블리를 제공합니다. Unity 실행 결과와 독립 실행 결과는 개발 기록에서 구분합니다.

## 저장 및 개발 모드

튜토리얼은 `Application.persistentDataPath/tutorial_save.json`을 사용하며 기존 `save.json`, `act_one_save.json`은 변경하지 않습니다. `AiRequest`의 `AISceneTrigger`에서 `Use Tutorial`을 끄면 기존 모드로 돌아갑니다. 이전 1막 초안에는 변경 전 설정이 남아 있으므로 최종 시나리오로 사용하지 않습니다.

# CI 도입 계획안 (문서만 — 코드·워크플로 파일 생성 없음)

작성: 빌드 봇, 2026-10-03 KST. 요청: 총괄 봇. 구현은 총괄 LOCK 후에만 진행.
근거: 저장소 `scarlel96-design/desktop-media-player` (public, default branch `main`, Actions 활성, allowed_actions=all), 작업 브랜치 `integrate/s19-ki022-ki024`, 현재 `.github/` 없음.

## 1. 확인된 사실 (직접 확인한 것만)
- `Directory.Build.props`: `net10.0-windows`, x64, `EnableWindowsTargeting=true`. .NET SDK 10.0.400로 로컬(Linux 박스)에서 restore/publish/test가 0 error 0 warn, 18/18로 돌았다(S71 로그).
- 테스트 프로젝트 `tests/DesktopMediaPlayer.Playback.Tests`는 Contracts와 Playback만 참조한다. libmpv와 WPF를 로드하지 않아 네이티브 DLL 없이 돈다. 테스트 시간은 약 218 ms(S71 로그).
- Shell publish는 WPF 때문에 `-p:EnableWindowsTargeting=true`로 Linux에서도 되지만, CI에서 실제 Windows 산출물 검증 의미를 주려면 Windows runner가 더 정확하다.
- `native/win-x64/`에는 README만 있고 바이너리는 커밋되지 않는다. Shell csproj는 `libmpv-2.dll`이 있을 때만 복사한다(없어도 빌드는 성공).
- native README 규칙 1: "CI나 agent에서 인터넷으로 DLL을 내려받지 않는다." 이 규칙을 CI에도 그대로 적용한다.
- `gh` 토큰 scope에 `workflow`가 있어 워크플로 파일 push가 가능하다(총괄 LOCK 후에).

## 2. 최소 워크플로 1개 (제안)
파일: `.github/workflows/ci.yml` (단일 job `build-test`)
- 트리거: `push`(브랜치 `main`, `integrate/**`)와 `pull_request`(대상 `main`). `workflow_dispatch` 추가(수동 재실행용).
- Runner: `windows-latest` 권장(Windows x64, WPF 타깃과 일치). 대안: `ubuntu-latest` + EnableWindowsTargeting(현재 박스와 동일, 더 빠르고 저렴하지만 Windows 호환 증거로는 약함).
- .NET: `actions/setup-dotnet`로 `dotnet-version: 10.0.x`(SDK 10.0.400 이상 확인된 박스 환경과 맞춤). `global.json`이 없으므로 SDK 고정이 필요하면 별도 파일 추가를 따로 LOCK 받는다.
- 단계:
  1. `actions/checkout`
  2. `actions/setup-dotnet`
  3. `dotnet restore DesktopMediaPlayer.sln` (또는 개별 csproj)
  4. `dotnet publish src/DesktopMediaPlayer.Shell/DesktopMediaPlayer.Shell.csproj -c Release -r win-x64 --self-contained false -o publish`
  5. `dotnet test tests/DesktopMediaPlayer.Playback.Tests/DesktopMediaPlayer.Playback.Tests.csproj -c Release` — 기대값 Passed 18 / Failed 0
  6. (선택) Shell.exe SHA-256을 job summary에 출력. 아티팩트 업로드는 기본 제외(릴리스·Portable 금지 원칙 유지).
- 권한: `permissions: contents: read`. 시크릿 사용 0. 외부 다운로드는 setup-dotnet과 NuGet 복원만.

## 3. libmpv 네이티브 처리
- CI에서는 libmpv를 받지 않는다. 이유: 규칙 1(CI에서 DLL 다운로드 금지), 라이선스 단계 LEVEL 1(번들·LGPL 질문 보류), 그리고 위 18개 테스트와 publish는 DLL 없이 성공하기 때문이다.
- 결과적으로 CI가 증명하는 범위는 컴파일, publish, 단위 테스트 18/18까지다. 실제 재생, seek, 렌더링은 CI가 검증하지 못한다. 이 부분은 계속 사용자 Windows 측정이 담당하며 Soft≠Windows PASS, KI-022/024 WINDOWS PENDING 원칙은 CI GREEN이 되어도 바뀌지 않는다.
- 이후 단계(별도 LOCK): 승인된 내부 캐시에서 NATIVE-002 세트를 가져오는 방식은 사용자·총괄 결정 사항이므로 이번 계획에 포함하지 않는다.

## 4. 예상 소요
- 구현: 워크플로 1개 작성, 박스에서 `act` 없이 문법 점검, push, 첫 실행 확인까지 약 30~45분(첫 실행 실패 시 수정 1~2회 포함).
- CI 1회 실행 시간 추정(측정값 아님): windows-latest에서 setup 및 restore 포함 약 3~6분. ubuntu 대안은 약 2~4분. 첫 실행은 NuGet 캐시가 없어 더 길 수 있다.
- 비용: public 저장소라 GitHub-hosted runner 무료(조건은 GitHub 정책 기준이며 실제 청구 여부는 첫 실행 후 확인).

## 5. 위험
1. runner의 .NET 10 SDK 가용성: `10.0.x` 해석이 실패하면 preview/quality 옵션이 필요할 수 있다. 첫 실행에서 확인.
2. `windows-latest` 이미지 변경으로 인한 간헐 실패(이미지 업데이트).
3. WPF publish가 Windows runner에서만 나타나는 경고·오류를 낼 가능성(현재 Linux 결과 0/0과 달라질 수 있음). 나오면 그대로 보고하고 기능 삭제로 맞추지 않는다.
4. "CI GREEN"이 Windows 재생 PASS로 오해될 위험: 이름·요약에 "build+unit test only"를 명시하고 상태 문서에서 CI GREEN과 Windows PASS를 분리 유지.
5. 워크플로 파일 push는 `integrate/**` 브랜치에 커밋을 하나 더 만든다. 그 커밋은 Canonical tip과 해시가 달라지므로 비서 기록, Soft tip 정의와 충돌하지 않게 "코드 변경 0, CI 파일만"으로 별도 S 단계 또는 별도 브랜치로 처리해야 한다(총괄 판단 필요).
6. 기존 PR 흐름 원칙(큰 변경은 PR)과의 관계: 이 변경은 신규 파일 1개라 작지만, 대상 브랜치를 `main`으로 할지 `integrate/*`로 할지 LOCK에서 정해야 한다.

## 6. 필요한 파일 목록
- 필수(신규): `.github/workflows/ci.yml`
- 선택(별도 LOCK): `global.json`(SDK 고정), README의 CI 배지·설명 한 줄, `docs/` 내 CI 범위 설명(Windows PASS 아님 명시)
- 변경 없음: src, tests, csproj, Directory.Build.props, native/

## 7. LOCK 때 총괄이 정해 줄 것
1. runner: windows-latest(권장) 또는 ubuntu-latest
2. 대상 브랜치와 반영 방식(직접 push vs PR) 및 Canonical tip과의 관계
3. 아티팩트 업로드 여부(기본: 없음)
4. 실행 중 사용량·비용 확인 방식

상태: 계획 문서만. 워크플로 파일, 코드, push 없음. CI GREEN 미주장, Windows NOT TESTED.

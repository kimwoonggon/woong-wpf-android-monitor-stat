# PRD: Windows + Android 전자기기 사용 측정/통계 프로젝트

문서 버전: v0.1
작성일: 2026-04-28
대상 실행 환경: Windows 10 개발 환경
주요 구현 방식: TDD-first, 테스트 통과 후 빌드/실행
대상 소비자: Codex 서브 에이전트 및 구현 담당 에이전트

---

## 1. 프로젝트 개요

이 프로젝트는 사용자가 하루 동안 Windows PC와 Android 모바일에서 어떤 앱/창/사이트를 얼마나 사용했는지 기록하고, 로컬 실시간 대시보드 및 통합 일간 리포트를 제공하는 사용 시간 측정 시스템이다.

목표는 단순한 타이머 앱이 아니라, 다음을 모두 지원하는 것이다.

1. Windows에서 실시간 앱/창 사용 세션 수집
2. Windows WPF 대시보드에서 기간별 실시간 통계 확인
3. Android에서 앱별 사용 시간 수집
4. Android XML 기반 UI에서 모바일 사용 통계 확인
5. Windows + Android 데이터를 서버에서 통합
6. 매일 아침 전날 사용 요약 통계 제공
7. 모든 핵심 로직은 TDD 기반으로 구현

---

## 2. 핵심 제품 방향

### 2.1 제품 핵심 가치

사용자가 하루 동안 PC와 모바일을 어떻게 사용했는지 다음 관점으로 확인할 수 있어야 한다.

* 시간대별 활동량
* 앱별 사용 시간
* 웹사이트/도메인별 사용 시간
* Windows와 Android의 통합 사용 시간
* idle 시간을 제외한 실제 활동 시간
* 매일 아침 전날 요약

### 2.2 가장 중요한 설계 원칙

이 프로젝트는 반드시 **기기별 로컬 DB + 서버 통합 DB** 구조로 설계한다.

```text
Windows WPF App
  -> Windows Local SQLite
  -> Sync API

Android Kotlin App
  -> Android Room(SQLite)
  -> Sync API

ASP.NET Core Server
  -> PostgreSQL Integrated DB
  -> Daily Summary Job
```

로컬 DB끼리는 서로 직접 알지 않는다.
Windows 앱은 Android DB를 알 필요가 없고, Android 앱도 Windows DB를 알 필요가 없다.

두 클라이언트는 공통 API 계약을 통해 서버와만 동기화한다.

---

## 3. 범위

## 3.1 MVP 포함 범위

### Windows

* C# WPF 앱
* MVVM 구조 기본 적용
* Windows foreground window/app 추적
* 프로세스명, PID, HWND, 창 제목 기록
* focus session 생성
* idle 감지
* SQLite 로컬 저장
* WPF 실시간 대시보드
* 기간 필터: 오늘, 최근 1시간, 최근 6시간, 최근 24시간, 사용자 지정
* 차트 3종

  * 시간대별 활동량
  * 앱별 사용 시간
  * 도메인/카테고리 비율
* outbox 기반 서버 동기화 준비
* 테스트 코드 기본 작성

### Android

* Kotlin 기반 Android 앱
* 기존 `WoongAndroidBasicProject`의 Gradle/버전 관리 스타일 유지
* XML/View 기반 UI
* AppCompatActivity 또는 Fragment 기반 구성
* ConstraintLayout, RecyclerView, ViewBinding 사용
* UsageStatsManager 기반 앱 사용 시간 수집
* Room 로컬 저장
* WorkManager 기반 주기 수집/동기화
* Android 대시보드 화면
* 아침 요약 알림 또는 요약 화면
* 테스트 코드 기본 작성

### Server

* ASP.NET Core Web API
* PostgreSQL 통합 DB
* device 등록 API
* session upload API
* raw event/session 중복 방지
* daily summary 생성
* Windows + Android 통합 조회 API
* integration test 기본 작성

---

## 3.2 MVP 제외 범위

다음 기능은 MVP에서 제외한다.

* 전역 키 입력 내용 기록
* 비밀번호, 채팅 내용, 폼 입력 내용 수집
* Android에서 타 앱의 터치 좌표 전역 수집
* 몰래 실행되는 감시형 기능
* Android Chrome URL 단위 추적
* 모든 차트의 고급 애니메이션/줌/팬
* 클라우드 멀티유저 SaaS 운영 기능
* 결제/구독

---

## 4. 개인정보 및 안전 정책

이 프로젝트는 생산성 통계 도구이며 감시 도구가 아니다.

반드시 지켜야 할 원칙:

1. 전역 키 입력 내용을 수집하지 않는다.
2. 비밀번호, 메시지, 입력 폼 내용을 수집하지 않는다.
3. Android에서 타 앱의 터치 좌표를 수집하지 않는다.
4. 사용자가 명시적으로 허용한 권한만 사용한다.
5. 수집 중임을 UI에서 명확히 표시한다.
6. 동기화는 opt-in으로 설계한다.
7. raw_event는 디버깅 목적이며 장기 보관 정책을 별도로 둔다.
8. 모든 시간은 UTC로 저장하고, 화면 표시 시 timezone 기준으로 변환한다.

---

## 5. 기술 스택

## 5.1 Windows App

* Language: C#
* UI: WPF
* Pattern: MVVM
* MVVM Library: CommunityToolkit.Mvvm 권장
* Local DB: SQLite
* DB Access: EF Core SQLite 또는 Dapper 중 하나 선택
* Chart: LiveCharts2 우선 검토
* Windows API: user32.dll P/Invoke
* Test: xUnit
* WPF Thread Test: Xunit.StaFact 또는 동등한 STA 테스트 방식
* UI Test: FlaUI 검토

## 5.2 Android App

기존 `WoongAndroidBasicProject`의 Gradle 스타일을 따른다.

기본 방향:

* Language: Kotlin
* UI: XML/View 기반
* Layout: ConstraintLayout
* List: RecyclerView
* Binding: ViewBinding
* Local DB: Room
* Background Work: WorkManager
* Usage Collection: UsageStatsManager
* Chart: MPAndroidChart 우선 검토
* Network: Retrofit + Moshi 또는 OkHttp
* Test:

  * JUnit
  * Robolectric
  * Room Testing
  * WorkManager Testing
  * Espresso
  * UI Automator

개발 환경:

* Windows 10
* Gradle Wrapper 사용
* 빌드는 `gradlew` 기반
* 프로젝트에 이미 준비된 `test androidapp` 도구가 있다면 해당 도구 우선 사용

## 5.3 Server

* Language: C#
* Framework: ASP.NET Core Web API
* DB: PostgreSQL
* ORM: EF Core PostgreSQL
* Test: xUnit
* Integration Test: WebApplicationFactory
* Background Job: 초기에는 ASP.NET Core BackgroundService, 이후 필요 시 Hangfire 검토

---

## 6. 아키텍처

## 6.1 전체 구조

```text
[Windows WPF Tracker]
  - Collector
  - Sessionizer
  - Local SQLite
  - WPF Dashboard
  - Sync Worker

[Android Kotlin Tracker]
  - UsageStats Collector
  - Sessionizer
  - Room DB
  - XML Dashboard
  - WorkManager Sync

[API Server]
  - Device Registration
  - Session Upload
  - Summary API
  - PostgreSQL
  - Daily Aggregation Job
```

## 6.2 로컬 DB와 통합 DB 관계

로컬 DB는 해당 기기 데이터만 저장한다.

* Windows SQLite는 Windows 데이터만 저장
* Android Room은 Android 데이터만 저장
* PostgreSQL은 모든 기기 데이터를 사용자 기준으로 통합

로컬 DB와 서버 DB는 SQL 레벨로 직접 연결되지 않는다.
대신 API DTO를 통해 동기화한다.

---

## 7. 공통 도메인 모델

## 7.1 핵심 엔티티

### Device

기기를 나타낸다.

필드 예시:

* id
* userId
* platform: windows | android
* deviceKey
* deviceName
* timezoneId
* createdAtUtc
* lastSeenAtUtc

### AppFamily

크로스플랫폼 앱 묶음이다.

예:

* Chrome
* YouTube
* VS Code
* Slack

Windows의 `chrome.exe`와 Android의 `com.android.chrome`는 둘 다 `Chrome` AppFamily에 매핑될 수 있다.

### PlatformApp

플랫폼별 실제 앱 식별자다.

예:

* Windows: chrome.exe
* Android: com.android.chrome

### FocusSession

가장 중요한 공통 fact 테이블이다.

의미:

> 사용자가 특정 기기에서 특정 앱에 머문 시간 구간

필드 예시:

* clientSessionId
* deviceId
* platformAppKey
* startedAtUtc
* endedAtUtc
* durationMs
* localDate
* timezoneId
* isIdle
* source

### WebSession

브라우저 상세 사용 세션이다.

초기에는 주로 Windows에서 채워진다.

필드 예시:

* focusSessionId
* browserFamily
* url
* domain
* pageTitle
* startedAtUtc
* endedAtUtc
* durationMs

### DeviceStateSession

기기 상태 세션이다.

예:

* active
* idle
* screen_on
* screen_off
* locked
* unlocked

### DailySummary

일간 집계 결과다.

예:

* summaryDate
* totalActiveMs
* totalIdleMs
* totalWebMs
* topApps
* topDomains

---

## 8. 데이터 저장 전략

## 8.1 Raw + Derived 구조

반드시 원본 이벤트와 파생 세션을 나눈다.

```text
raw_event
  -> focus_session
  -> web_session
  -> daily_summary
```

이유:

* 세션 계산 로직을 나중에 수정 가능
* 재집계 가능
* 디버깅 가능
* 동기화 오류 분석 가능

## 8.2 Outbox Sync 패턴

클라이언트는 서버에 바로 쓰려고 하지 않는다.

수집 흐름:

```text
수집
  -> 로컬 DB 저장
  -> sync_outbox 등록
  -> 서버 업로드
  -> 성공 시 synced 처리
```

장점:

* 오프라인 동작 가능
* 재시도 가능
* 유실 방지
* 서버 장애에 강함

---

## 9. Windows 기능 요구사항

## 9.1 Foreground Window Collector

Windows collector는 다음 정보를 수집한다.

* 현재 foreground HWND
* PID
* process name
* executable path
* window title
* timestamp

필수 동작:

1. 앱/창 전환 시 기존 focus session 종료
2. 새 focus session 시작
3. 동일 창이 계속 활성 상태면 세션 유지
4. idle 상태 진입 시 idle flag 반영
5. 자정 경계에서 localDate 계산이 정확해야 함

## 9.2 Chrome/Web 추적

MVP에서는 두 단계로 나눈다.

### Phase 1

* Windows 창 제목 기반으로 브라우저 사용 시간 추정
* Chrome process와 window title 기록

### Phase 2

* Chrome extension + native messaging 도입
* active tab URL/title/domain 기록
* web_session 생성

Chrome 내부 `windowId/tabId`와 Windows `HWND/PID`는 같은 ID가 아니다.
따라서 직접 같은 키로 보지 않고 timestamp, title, foreground state 등을 통해 매핑한다.

## 9.3 WPF Dashboard

필수 화면:

* Dashboard
* Live Event Log
* App Sessions
* Web Sessions
* Settings

필수 차트:

1. 시간대별 활동 라인/영역 차트
2. 앱별 사용 시간 막대 차트
3. 도메인/카테고리 도넛 차트

필수 필터:

* 오늘
* 최근 1시간
* 최근 6시간
* 최근 24시간
* 사용자 지정

---

## 10. Android 기능 요구사항

## 10.1 기본 방향

Android는 XML/View 기반으로 구현한다.

Compose는 MVP에서 사용하지 않는다.
나중에 필요하면 `ComposeView`를 통해 일부 화면만 점진적으로 도입할 수 있다.

## 10.2 UsageStats Collector

Android app은 UsageStatsManager를 사용해 앱 사용 세션을 수집한다.

필수 동작:

1. Usage Access 권한 상태 확인
2. 권한이 없으면 Settings 이동 UI 제공
3. UsageEvents를 읽어 앱별 세션 생성
4. 같은 앱의 짧은 연속 이벤트 병합
5. Room에 focus_session 저장
6. WorkManager로 주기 수집 및 동기화

## 10.3 Android Dashboard

필수 화면:

* DashboardActivity 또는 DashboardFragment
* SessionsActivity 또는 SessionsFragment
* SettingsActivity 또는 SettingsFragment

Dashboard 구성:

* 기간 필터
* 총 활성 시간 카드
* top app 카드
* idle/비활동 시간 카드
* 시간대별 활동 차트
* 앱별 사용량 차트
* 최근 세션 RecyclerView

## 10.4 Android 알림/아침 요약

초기 목표:

* 앱 내부에서 전날 요약 확인

이후 목표:

* WorkManager 기반 아침 요약 알림
* 서버 daily summary 조회 후 표시

---

## 11. Server 기능 요구사항

## 11.1 API

필수 API:

1. Device 등록/갱신
2. FocusSession 업로드
3. WebSession 업로드
4. RawEvent 업로드
5. DailySummary 조회
6. Date range 기반 통계 조회

## 11.2 Idempotency

클라이언트는 `clientEventId`, `clientSessionId`를 생성한다.

서버는 다음 조건을 만족해야 한다.

* 같은 deviceId + clientSessionId가 다시 와도 중복 insert하지 않음
* 같은 raw event가 다시 와도 중복 insert하지 않음
* retry-safe API 제공

## 11.3 Summary Job

일간 요약은 서버에서 생성한다.

기준:

* user timezone 기준 localDate
* idle 제외 active time
* top app
* top domain
* Windows + Android 통합

---

## 12. 테스트 전략

이 프로젝트는 TDD-first로 진행한다.

원칙:

1. 기능 구현 전 실패하는 테스트를 먼저 작성한다.
2. 테스트를 통과시키기 위해 최소 구현을 한다.
3. 테스트가 틀렸다면 테스트를 고치되, 먼저 요구사항과 비교한다.
4. 테스트 실패가 실제 제품 코드의 버그라면 제품 코드를 수정한다.
5. 테스트를 억지로 느슨하게 만들어 통과시키지 않는다.
6. 테스트 통과 전 빌드/실행 확인으로 넘어가지 않는다.
7. 테스트 통과 + 빌드 성공이 각 작업의 완료 조건이다.

## 12.1 테스트 분류 정의

### Unit Test

순수 로직 테스트.

예:

* 세션화
* 시간대별 집계
* 도메인 정규화
* localDate 계산
* duration 계산

### Component Test

한 컴포넌트와 가까운 실제 인프라를 함께 테스트.

예:

* SQLite repository
* Room DAO
* outbox processor
* ViewModel + fake repository
* WorkManager worker

### Integration Test

여러 컴포넌트 연결 테스트.

예:

* client sync payload -> server API -> DB 저장
* raw_event -> focus_session -> daily_summary
* API upload 중복 방지

### UI Test

실제 UI 조작 테스트.

예:

* WPF 기간 필터 클릭
* Android Dashboard 필터 클릭
* Android Usage Access Settings 이동

---

## 13. Windows 테스트 계획

## 13.1 Unit Tests

필수 테스트:

* `Sessionizer_WhenAppChanges_ClosesPreviousAndStartsNewSession`
* `Sessionizer_WhenSameWindowContinues_ExtendsCurrentSession`
* `TimeBucketAggregator_SplitsSessionAcrossHours`
* `LocalDateCalculator_WhenSessionCrossesMidnight_UsesDeviceTimezone`
* `DomainNormalizer_ExtractsRegistrableDomain`
* `IdleCalculator_WhenLastInputExceedsThreshold_MarksIdle`

## 13.2 Component Tests

필수 테스트:

* SQLite repository insert/query/update
* focus_session 저장 후 조회
* web_session 저장 후 focus_session 연결
* outbox pending -> success -> synced 상태 변경
* DashboardViewModel 기간 필터 변경 시 summary 갱신

## 13.3 Integration Tests

필수 테스트:

* raw_event 입력 -> focus_session 생성
* focus_session -> daily summary local aggregation
* sync outbox -> fake server success
* sync outbox -> fake server fail -> retry count 증가

## 13.4 UI Tests

도구: FlaUI 검토

필수 smoke:

* 앱 실행 시 Dashboard 표시
* 오늘 필터 클릭 시 summary 카드 표시
* 최근 6시간 필터 클릭 시 차트 갱신
* Settings 화면 진입
* sync error 상태 표시

---

## 14. Android 테스트 계획

## 14.1 Unit Tests

필수 테스트:

* `UsageSessionizer_WhenActivityResumedAndPaused_CreatesSession`
* `UsageSessionizer_WhenSameAppEventsAreClose_MergesSessions`
* `UsageSessionizer_WhenDifferentAppStarts_ClosesPreviousSession`
* `DailySummaryCalculator_GroupsByLocalDate`
* `DashboardFormatter_FormatsDurationCorrectly`

## 14.2 Component Tests

필수 테스트:

* Room DAO insert/query
* Room DAO date range query
* UsageRepository fake events -> sessions
* SyncOutbox worker success
* SyncOutbox worker failure retry
* DashboardViewModel state update

## 14.3 Android UI Tests

XML/View 기반이므로 Compose test가 아니라 Espresso를 사용한다.

필수 Espresso 테스트:

* DashboardActivity 표시
* 오늘/어제/최근 7일 필터 클릭
* summary card 표시
* 빈 데이터 상태 표시
* Sessions list 표시

UI Automator 테스트:

* Usage Access Settings 화면 이동 smoke
* 권한 필요 안내 화면 표시

---

## 15. Server 테스트 계획

## 15.1 Unit Tests

필수 테스트:

* daily summary calculator
* app family mapper
* duration aggregation
* duplicate detection policy

## 15.2 Integration Tests

도구:

* xUnit
* WebApplicationFactory
* test database

필수 테스트:

* device register API
* upload focus sessions API
* duplicate clientSessionId ignored
* upload web sessions API
* daily summary generation
* summary query API

---

## 16. 빌드/검증 원칙

## 16.1 공통 원칙

각 작업은 다음 순서를 반드시 따른다.

```text
1. 요구사항 확인
2. 실패하는 테스트 작성
3. 최소 구현
4. 테스트 실행
5. 실패 원인 분석
6. 제품 코드 또는 테스트 수정
7. 모든 관련 테스트 통과
8. 빌드 실행
9. 실행 확인
10. 문서 업데이트
```

## 16.2 Windows 검증 명령 예시

정확한 solution 구조가 만들어진 뒤 명령은 조정한다.

예상 명령:

```powershell
dotnet test
```

WPF 앱 빌드:

```powershell
dotnet build
```

## 16.3 Android 검증 명령 예시

Windows 환경에서는 Gradle Wrapper를 사용한다.

```powershell
.\gradlew.bat testDebugUnitTest
.\gradlew.bat assembleDebug
```

에뮬레이터 또는 실기기 연결 시:

```powershell
.\gradlew.bat connectedDebugAndroidTest
```

프로젝트에 이미 준비된 `test androidapp` 도구가 있다면 해당 도구를 우선 사용한다.

---

## 17. Codex 작업 규칙

Codex 에이전트는 다음 규칙을 따른다.

1. 작업 전 repo 구조를 먼저 탐색한다.
2. 기존 Gradle/solution/style을 임의로 깨지 않는다.
3. 필요한 경우 skill 탐색 도구를 사용한다.

   * 예: `vercel find skill` 또는 사용 가능한 skill 검색 기능
4. 새 프레임워크를 도입하기 전 PRD의 기술 스택과 일치하는지 확인한다.
5. 구현 전 테스트를 먼저 작성한다.
6. 테스트 실패를 무시하지 않는다.
7. 테스트 통과 없이 다음 기능으로 넘어가지 않는다.
8. 빌드 실패 상태로 작업을 완료하지 않는다.
9. 안전/프라이버시 제외 범위를 구현하지 않는다.
10. 임시 workaround를 넣었다면 TODO와 이유를 남긴다.

---

## 18. 서브 에이전트 작업 분리 제안

Codex에서 서브 에이전트로 나눌 경우 다음 역할을 권장한다.

## 18.1 Architecture Agent

책임:

* 공통 도메인 모델 정리
* DB 스키마 초안
* API DTO 계약
* sync protocol 정의
* milestone 관리

완료 조건:

* domain 문서 작성
* DTO 목록 작성
* local/server DB 차이 명확화

## 18.2 Windows Agent

책임:

* WPF MVVM 앱 구조
* Windows collector
* SQLite local storage
* WPF dashboard
* Windows tests

완료 조건:

* unit/component tests 통과
* WPF 빌드 성공
* 기본 dashboard 실행 가능

## 18.3 Android Agent

책임:

* 기존 Gradle 스타일 유지
* XML/View UI
* UsageStats collector
* Room DB
* WorkManager
* Android tests

완료 조건:

* `gradlew testDebugUnitTest` 통과
* `gradlew assembleDebug` 성공
* 필요 시 `connectedDebugAndroidTest` 통과

## 18.4 Server Agent

책임:

* ASP.NET Core API
* PostgreSQL schema
* upload API
* daily summary API
* server integration tests

완료 조건:

* `dotnet test` 통과
* API integration tests 통과
* idempotency 검증

## 18.5 QA/Test Agent

책임:

* 테스트 누락 감시
* 실패 테스트 분석
* smoke checklist 작성
* CI 명령 정리

완료 조건:

* Windows/Android/Server 테스트 매트릭스 완성
* release checklist 작성

---

## 19. 마일스톤 로드맵

## Milestone 0: Repository/Workspace Bootstrap

목표:

* 프로젝트 구조 정리
* Windows app, Android app, Server 위치 확정
* 테스트 프로젝트 생성

작업:

* repo 구조 확인
* 기존 Android Gradle 설정 확인
* Windows solution 생성 또는 확인
* Server solution 생성 또는 확인
* docs 폴더 생성
* PRD 저장

완료 조건:

* 전체 repo 구조 문서화
* 각 프로젝트 build 명령 확인
* 빈 테스트 하나씩 통과

---

## Milestone 1: Common Domain & Contracts

목표:

* 플랫폼 공통 개념 고정
* DTO 계약 확정
* TDD 대상 로직 분리

작업:

* FocusSession model 정의
* WebSession model 정의
* Device model 정의
* DailySummary model 정의
* upload DTO 정의
* time/date 정책 문서화

테스트:

* duration 계산
* UTC/local date 변환
* hour bucket split

완료 조건:

* 공통 도메인 테스트 통과
* 계약 문서 작성

---

## Milestone 2: Windows Local Collector MVP

목표:

* Windows에서 앱/창 세션 수집

작업:

* user32.dll P/Invoke wrapper
* foreground window snapshot model
* collector service
* sessionizer
* idle detector

테스트:

* fake foreground snapshots로 sessionizer unit test
* idle threshold unit test
* 자정 넘김 테스트

완료 조건:

* Windows unit tests 통과
* 실제 Windows 10에서 foreground app 로그 확인

---

## Milestone 3: Windows Local DB + Outbox

목표:

* Windows 수집 데이터를 SQLite에 안정적으로 저장

작업:

* SQLite schema
* repository
* sync_outbox
* migration strategy

테스트:

* repository component test
* outbox retry test
* duplicate local insert 방지 테스트

완료 조건:

* SQLite tests 통과
* local DB 파일 생성/조회 가능

---

## Milestone 4: Windows WPF Dashboard MVP

목표:

* Windows에서 실시간 통계 확인

작업:

* WPF MVVM 구조
* DashboardViewModel
* 기간 필터
* Summary cards
* LiveCharts2 연동
* App sessions table

테스트:

* DashboardViewModel tests
* chart mapper tests
* WPF UI smoke test

완료 조건:

* WPF 앱 빌드 성공
* 테스트 통과
* 실제 실행 시 오늘/최근 1시간 통계 표시

---

## Milestone 5: Server Integrated DB + API MVP

목표:

* 서버에 통합 저장소 구축

작업:

* PostgreSQL schema
* EF Core entities
* device registration API
* focus session upload API
* web session upload API
* idempotency

테스트:

* WebApplicationFactory integration tests
* duplicate upload tests
* summary calculator unit tests

완료 조건:

* server tests 통과
* Windows client payload를 서버가 저장 가능

---

## Milestone 6: Windows Sync

목표:

* Windows 로컬 DB -> 서버 동기화

작업:

* sync worker
* retry policy
* auth/device token
* sync checkpoint

테스트:

* fake API sync success
* fake API sync failure retry
* duplicate upload safe

완료 조건:

* Windows local data가 서버에 업로드됨
* 재시도/중복 테스트 통과

---

## Milestone 7: Android Project Setup

목표:

* 기존 `WoongAndroidBasicProject` 스타일 기반 Android 프로젝트 구성

작업:

* Gradle/Version Catalog 확인
* XML/View 기반 activity 구성
* ViewBinding 설정
* Room/WorkManager/test dependencies 추가

테스트:

* 빈 JUnit test
* 빈 Espresso test
* Gradle unit test task

완료 조건:

* `gradlew testDebugUnitTest` 통과
* `gradlew assembleDebug` 성공

---

## Milestone 8: Android Usage Collection + Room

목표:

* Android 앱별 사용 세션 수집

작업:

* UsageStats permission check
* UsageStats collector
* UsageSessionizer
* Room entities/DAO
* WorkManager collect worker

테스트:

* fake usage events sessionizer test
* Room DAO test
* worker test

완료 조건:

* 앱별 focus_session 생성 가능
* 로컬 DB 저장 가능
* unit/component tests 통과

---

## Milestone 9: Android XML Dashboard MVP

목표:

* Android에서 모바일 사용 통계 표시

작업:

* DashboardActivity/Fragment
* summary cards
* RecyclerView recent sessions
* MPAndroidChart charts
* Settings screen

테스트:

* ViewModel tests
* Espresso Dashboard smoke
* 권한 안내 화면 테스트

완료 조건:

* Android dashboard 표시
* 기간 필터 동작
* Gradle test/build 통과

---

## Milestone 10: Android Sync + Morning Summary

목표:

* Android 데이터를 서버에 업로드하고 통합 요약 조회

작업:

* sync outbox
* Retrofit/OkHttp client
* WorkManager sync worker
* daily summary API 연동
* morning summary notification 또는 화면

테스트:

* sync worker tests
* fake server tests
* duplicate upload tests
* summary display tests

완료 조건:

* Android local sessions 서버 업로드
* 서버 summary Android에서 조회
* 테스트/빌드 통과

---

## Milestone 11: Integrated Daily Summary

목표:

* Windows + Android 통합 일간 리포트 제공

작업:

* server daily aggregation job
* top app 통합
* app family mapping
* top domain 계산
* idle 제외 active time 계산

테스트:

* Windows + Android mixed data summary test
* timezone test
* duplicate data test

완료 조건:

* 같은 사용자의 여러 기기 데이터가 하나의 daily summary로 통합됨
* Android/Windows에서 조회 가능

---

## Milestone 12: Hardening & Release Candidate

목표:

* 안정성/프라이버시/성능 강화

작업:

* DB migration 점검
* 로그 보관 정책
* sync 실패 UI
* 권한 안내 문구
* CPU/메모리 점검
* 테스트 안정화
* README 작성

테스트:

* 전체 unit/component/integration tests
* WPF UI smoke
* Android connected tests
* manual checklist

완료 조건:

* 모든 테스트 통과
* Windows build 성공
* Android assemble 성공
* 서버 integration tests 통과
* 수동 실행 체크리스트 통과

---

## 20. Definition of Done

각 기능의 완료 조건은 다음과 같다.

1. 요구사항이 코드/테스트에 반영됨
2. 실패 테스트를 먼저 작성했음
3. 테스트를 통과시키는 구현이 있음
4. 관련 unit/component/integration test 통과
5. 빌드 성공
6. 안전/프라이버시 제외 범위를 침범하지 않음
7. 관련 문서 업데이트
8. 임시 구현이 있다면 TODO와 이유 기록

---

## 21. Codex에게 전달할 최종 지시문

아래 원칙을 모든 작업에 적용한다.

```text
이 프로젝트는 Windows + Android + Server 기반 사용 시간 측정 시스템이다.

반드시 TDD-first로 작업한다.
기능 구현 전 테스트를 먼저 작성한다.
테스트가 실패하면 실패 원인을 분석하고, 제품 코드가 틀렸다면 제품 코드를 고친다.
테스트를 억지로 느슨하게 만들어 통과시키지 않는다.

Windows는 C# WPF + MVVM이 기본이다.
Android는 Kotlin + XML/View 기반이며 기존 WoongAndroidBasicProject의 Gradle/버전 관리 스타일을 따른다.
Server는 ASP.NET Core + PostgreSQL이다.

Windows 10 환경에서 작업한다.
Android 빌드는 gradlew를 사용한다.
프로젝트에 준비된 test androidapp 도구가 있으면 우선 사용한다.
테스트가 모두 통과하고 빌드가 성공해야 실행 확인 단계로 넘어간다.

로컬 DB와 서버 통합 DB를 혼동하지 않는다.
Windows SQLite와 Android Room은 각 기기 로컬 데이터만 저장한다.
PostgreSQL 서버 DB만 Windows + Android 통합 데이터를 저장한다.
두 로컬 DB는 서로 직접 알지 않고, 공통 API DTO 계약으로 서버와 동기화한다.

전역 키로깅, 비밀번호/메시지/폼 입력 수집, Android 전역 터치 좌표 수집, 몰래 감시 기능은 구현하지 않는다.

필요한 경우 사용 가능한 skill 탐색 도구를 사용한다.
예: vercel find skill 또는 환경에서 제공되는 skill 검색 기능.
다만 skill 사용으로 인해 PRD의 기술 스택을 임의 변경해서는 안 된다.
```

---

## 22. 첫 작업 추천

Codex 첫 작업은 다음 순서로 시작한다.

1. repo 구조 탐색
2. docs/prd.md에 이 문서 저장
3. Windows/Android/Server 프로젝트 존재 여부 확인
4. 없다면 skeleton 생성 계획 제안
5. 공통 도메인 모델 테스트부터 작성
6. 테스트 통과 후 Windows Collector MVP로 진행

첫 번째 실제 구현 작업은 다음이 적합하다.

> Common domain: FocusSession, TimeRange, TimeBucketAggregator, LocalDateCalculator 테스트 작성 및 구현

이 작업은 OS/API/DB에 의존하지 않으므로 TDD 기반을 잡기에 가장 안전하다.

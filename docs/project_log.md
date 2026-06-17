# ColorCodePicker 프로젝트 일지

## 프로젝트 개요
*   **목표:** 다중 모니터를 지원하는 스포이드(Eyedropper) 및 RGB/HEX ↔ Munsell 색상 변환 단일 실행 파일(.exe) 데스크톱 애플리케이션 개발.
*   **기술 스택:** C# / .NET 8 WPF, WPF UI(lepoco), NetSparkleUpdater(GitHub 연동 자동 업데이트)
*   **GitHub 저장소:** iy7847/Color-Code

## 진행 상황 기록
*   **2026-06-17:** 프로젝트 초기화 (.NET 8 WPF). 문서화 체계(docs) 구성. WPF UI 패키지 설치.
*   **2026-06-17:** 핵심 UI 구성, Unicolour 라이브러리를 통한 RGB/HEX/Munsell 변환 로직 구현.
*   **2026-06-17:** Win32 API 기반 다중 모니터 대응 투명 스포이드(Eyedropper) 기능 개발 완료.
*   **2026-06-17:** NetSparkleUpdater를 통한 GitHub (iy7847/Color-Code) 릴리즈 기반 자동 업데이트 로직 연동 완료.

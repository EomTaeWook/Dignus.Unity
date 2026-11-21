# Dignus.Unity

[English](README.md) | 한국어

DI, 씬 수명 주기, 리소스, 오브젝트 풀, 코루틴과 바인딩을 제공하는 Unity 통합 라이브러리.

## 요구 사항

Unity 2022.3 이상이 필요하다. 특성 기반 리소스 로드는 Unity Resources 폴더를 사용한다.

## 설치

```xml
<PackageReference Include="Dignus.Unity" Version="1.1.6" />
```

Unity Package Manager Git 주소:

```text
https://github.com/EomTaeWook/Dignus.Unity.git?path=/publish/upm/com.dignus.unity#v1.1.6
```

## 빠른 시작

씬의 Awake()보다 먼저 실행되는 시작 코드에서 필요한 애플리케이션 어셈블리를 한 번씩 등록한다.

```csharp
using Dignus.Unity.DependencyInjection;

var container = DignusUnityServiceContainer.RegisterDependencies(typeof(LobbySceneController).Assembly);
container.Build();
```

## 사용법

가이드의 초기화·모델·컨트롤러·씬 연결 예제를 따른다. 선택적 InjectedComponents는 Awake() 전에 설정한다. 관리자 기반 전환의 OnDestroyScene()에서 컨트롤러 소유 자원을 정리한다.

## 문서

- [API와 연결 예제](publish/Dignus.Unity.ko.md)
- [UPM 패키지 가이드](publish/Dignus.Unity.ko.md#설치)

## 라이선스

MIT 라이선스. [LICENSE](LICENSE)를 참조한다.

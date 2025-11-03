# VirtualKeyboard.Input.Korean

한글 두벌식 입력 방식을 지원하는 조합기(Composer) 라이브러리입니다.

## 개요

VirtualKeyboard.Input.Korean은 한글의 초성, 중성, 종성을 조합하여 완성된 한글 음절을 생성하는 IME 조합기입니다. 두벌식 입력 방식을 기반으로 복합 자모(ㄳ, ㅘ 등)의 조합과 분해를 지원하며, 자연스러운 한글 입력 경험을 제공합니다.

## 주요 기능

- ✅ **두벌식 한글 조합**: 초성 + 중성 + 종성의 자동 조합
- ✅ **복합 자모 지원**: 
  - 복합 중성: ㅘ, ㅙ, ㅚ, ㅝ, ㅞ, ㅟ, ㅢ
  - 복합 종성: ㄳ, ㄵ, ㄶ, ㄺ, ㄻ, ㄼ, ㄽ, ㄾ, ㄿ, ㅀ, ㅄ
- ✅ **자동 음절 분리**: 초성+중성+종성 상태에서 중성 입력 시 자동으로 새 음절 시작
- ✅ **백스페이스 분해**: 복합 자모를 단계별로 분해
- ✅ **자연스러운 입력**: Windows/macOS 표준 한글 IME와 동일한 동작

## 사용 방법

### 1. 기본 사용

```csharp
using VirtualKeyboard.Input;
using VirtualKeyboard.Input.Korean.Composer;

// 한글 조합기 생성
var composer = new KoreanComposer();
var ime = new IME(composer);

// "가" 입력
var result = ime.Input('ㄱ');
Console.WriteLine($"조합 중: {result.ComposingText}");  // "ㄱ"

result = ime.Input('ㅏ');
Console.WriteLine($"조합 중: {result.ComposingText}");  // "가"

// 확정
result = ime.Commit();
Console.WriteLine($"확정: {result.CommittedText}");     // "가"
```

### 2. 복합 종성 입력 예제

```csharp
var ime = new IME(new KoreanComposer());

// "값" 입력
ime.Input('ㄱ');  // "ㄱ"
ime.Input('ㅏ');  // "가"
ime.Input('ㅂ');  // "갑"
ime.Input('ㅅ');  // "값" (ㅂ + ㅅ = ㅄ)

var result = ime.Commit();
Console.WriteLine(result.CommittedText);  // "값"
```

### 3. 자동 음절 분리

```csharp
var ime = new IME(new KoreanComposer());

// "간나" 입력
ime.Input('ㄱ');  // 조합: "ㄱ"
ime.Input('ㅏ');  // 조합: "가"
ime.Input('ㄴ');  // 조합: "간"
ime.Input('ㄴ');  // 확정: "간", 조합: "ㄴ" (자동 분리)
ime.Input('ㅏ');  // 조합: "나"
```

### 4. 복합 종성 자동 분해

```csharp
var ime = new IME(new KoreanComposer());

// "값이" 입력
ime.Input('ㄱ');  // "ㄱ"
ime.Input('ㅏ');  // "가"
ime.Input('ㅂ');  // "갑"
ime.Input('ㅅ');  // "값"
ime.Input('ㅣ');  // 확정: "갑", 조합: "시" (ㅄ을 ㅂ+ㅅ로 분해, ㅅ이 새 음절의 초성)

// 최종: "갑시"
```

### 5. 백스페이스로 단계별 분해

```csharp
var ime = new IME(new KoreanComposer());

ime.Input('ㄱ');  // "ㄱ"
ime.Input('ㅏ');  // "가"
ime.Input('ㅂ');  // "갑"
ime.Input('ㅅ');  // "값"

var result = ime.Backspace();
Console.WriteLine(result.ComposingText);  // "갑" (ㅄ → ㅂ)

result = ime.Backspace();
Console.WriteLine(result.ComposingText);  // "가" (ㅂ 제거)

result = ime.Backspace();
Console.WriteLine(result.ComposingText);  // "ㄱ" (ㅏ 제거)

result = ime.Backspace();
Console.WriteLine(result.ComposingText);  // "" (완전 삭제)
```

### 6. 복합 중성 입력

```csharp
var ime = new IME(new KoreanComposer());

// "과" 입력
ime.Input('ㄱ');  // "ㄱ"
ime.Input('ㅗ');  // "고"
ime.Input('ㅏ');  // "과" (ㅗ + ㅏ = ㅘ)

// "귀" 입력
ime.Reset();
ime.Input('ㄱ');  // "ㄱ"
ime.Input('ㅜ');  // "구"
ime.Input('ㅣ');  // "귀" (ㅜ + ㅣ = ㅟ)
```

### 7. 실시간 텍스트 입력 처리

```csharp
using System.Text;
using VirtualKeyboard.Input;
using VirtualKeyboard.Input.Korean.Composer;

class KoreanTextInput
{
    private readonly IME _ime;
    private readonly StringBuilder _textBuffer;
    
    public KoreanTextInput()
    {
        _ime = new IME(new KoreanComposer());
        _textBuffer = new StringBuilder();
    }
    
    public string ComposingText { get; private set; } = "";
    public string Text => _textBuffer.ToString();
    
    public void Input(char key)
    {
        var result = _ime.Input(key);
        
        if (result.Success)
        {
            // 확정된 텍스트가 있으면 버퍼에 추가
            if (!string.IsNullOrEmpty(result.CommittedText))
            {
                _textBuffer.Append(result.CommittedText);
            }
            
            // 조합 중인 텍스트 업데이트
            ComposingText = result.ComposingText;
            
            // UI 업데이트 트리거
            OnTextChanged();
        }
    }
    
    public void Backspace()
    {
        var result = _ime.Backspace();
        
        if (result.Success)
        {
            ComposingText = result.ComposingText;
            OnTextChanged();
        }
        else if (_textBuffer.Length > 0)
        {
            // 조합 중이 아니면 버퍼에서 한 글자 삭제
            _textBuffer.Length--;
            OnTextChanged();
        }
    }
    
    public void Commit()
    {
        var result = _ime.Commit();
        
        if (!string.IsNullOrEmpty(result.CommittedText))
        {
            _textBuffer.Append(result.CommittedText);
        }
        
        ComposingText = "";
        OnTextChanged();
    }
    
    private void OnTextChanged()
    {
        // UI 업데이트 로직
        Console.WriteLine($"텍스트: {Text} | 조합중: {ComposingText}");
    }
}

// 사용 예
var input = new KoreanTextInput();
input.Input('ㅎ');  // 텍스트: "" | 조합중: "ㅎ"
input.Input('ㅏ');  // 텍스트: "" | 조합중: "하"
input.Input('ㄴ');  // 텍스트: "" | 조합중: "한"
input.Input('ㄱ');  // 텍스트: "한" | 조합중: "ㄱ"
input.Input('ㅡ');  // 텍스트: "한" | 조합중: "그"
input.Input('ㄹ');  // 텍스트: "한" | 조합중: "글"
input.Commit();     // 텍스트: "한글" | 조합중: ""
```

## 아키텍처

### 핵심 클래스

#### `KoreanComposer`
한글 두벌식 조합기의 메인 구현 클래스입니다.

```csharp
public class KoreanComposer : IInputComposer
{
    public string Name => "한글 조합기";
    public string Language => "ko-KR";
    public string Description => "한글 자모 조합 (초성, 중성, 종성)";
    
    // IInputComposer 인터페이스 구현
    public CompositionResult ProcessInput(CompositionContext context, string input);
    public CompositionResult ProcessBackspace(CompositionContext context);
    public CompositionResult Commit(CompositionContext context);
    public CompositionResult Cancel(CompositionContext context);
    // ... 기타 메서드
}
```

#### `KoreanState`
한글 조합 상태를 관리하는 클래스입니다.

```csharp
public class KoreanState : ICompositionState
{
    // 초성 인덱스 (-1이면 없음)
    public int ChoseongIndex { get; set; }
    
    // 중성 인덱스 (-1이면 없음)
    public int JungseongIndex { get; set; }
    
    // 종성 인덱스 (0이면 없음)
    public int JongseongIndex { get; set; }
    
    // 조합 중인지 여부
    public bool IsComposing { get; }
    
    // 상태 확인 속성
    public bool HasChoseongOnly { get; }
    public bool HasChoseongAndJungseong { get; }
    public bool IsComplete { get; }
    public bool HasJongseongOnly { get; }
}
```

#### `HangulUtil`
한글 조합/분해를 위한 유틸리티 클래스입니다.

```csharp
public static class HangulUtil
{
    // 음절 조합
    public static char Compose(int choseong, int jungseong, int jongseong);
    
    // 음절 분해
    public static (int choseong, int jungseong, int jongseong) Decompose(char syllable);
    
    // 복합 중성 조합/분해
    public static bool TryCombineJungseong(char first, char second, out char combined);
    public static bool TryDecomposeJungseong(char jungseong, out char first, out char second);
    
    // 복합 종성 조합/분해
    public static bool TryCombineJongseong(char first, char second, out char combined);
    public static bool TryDecomposeJongseong(char jongseong, out char first, out char second);
    
    // 한글 음절 확인
    public static bool IsHangulSyllable(char ch);
}
```

#### `HangulLibrary`
한글 유니코드 관련 상수와 조회 테이블을 제공합니다.

```csharp
public static class HangulLibrary
{
    // 한글 음절 범위
    public const int HANGUL_BASE = 0xAC00;  // '가'
    public const int HANGUL_END = 0xD7A3;   // '힣'
    
    // 자모 배열
    public static readonly char[] CHOSEONG;    // 19개
    public static readonly char[] JUNGSEONG;   // 21개
    public static readonly char[] JONGSEONG;   // 28개 (없음 포함)
    
    // 빠른 조회 메서드
    public static bool IsJamo(char ch);
    public static bool IsChoseong(char ch);
    public static bool IsJungseong(char ch);
    public static bool IsJongseong(char ch);
    
    public static int GetChoseongIndex(char ch);
    public static int GetJungseongIndex(char ch);
    public static int GetJongseongIndex(char ch);
}
```

## 한글 조합 규칙

### 기본 조합 규칙

1. **초성만**: `ㄱ`
2. **초성 + 중성**: `가`
3. **초성 + 중성 + 종성**: `간`

### 복합 자모 조합

#### 복합 중성 (7개)
- `ㅗ + ㅏ → ㅘ` (과)
- `ㅗ + ㅐ → ㅙ` (괘)
- `ㅗ + ㅣ → ㅚ` (괴)
- `ㅜ + ㅓ → ㅝ` (궈)
- `ㅜ + ㅔ → ㅞ` (궤)
- `ㅜ + ㅣ → ㅟ` (귀)
- `ㅡ + ㅣ → ㅢ` (긔)

#### 복합 종성 (11개)
- `ㄱ + ㅅ → ㄳ` (값)
- `ㄴ + ㅈ → ㄵ` (않)
- `ㄴ + ㅎ → ㄶ` (않)
- `ㄹ + ㄱ → ㄺ` (닭)
- `ㄹ + ㅁ → ㄻ` (삶)
- `ㄹ + ㅂ → ㄼ` (넓)
- `ㄹ + ㅅ → ㄽ` (핥)
- `ㄹ + ㅌ → ㄾ` (핥)
- `ㄹ + ㅍ → ㄿ` (읊)
- `ㄹ + ㅎ → ㅀ` (읓)
- `ㅂ + ㅅ → ㅄ` (없)

### 자동 음절 분리

종성이 있는 완성된 음절에 중성을 입력하면 자동으로 음절이 분리됩니다:

```
간 + ㄴ → 간, ㄴ (새 음절 시작)
간 + ㅏ → 간, 나 (종성 ㄴ이 새 음절의 초성으로)
값 + ㅣ → 갑, 시 (복합 종성 ㅄ을 ㅂ+ㅅ로 분해)
```

## 입력 예제

### 예제 1: "안녕하세요" 입력

```csharp
var ime = new IME(new KoreanComposer());
var result = new StringBuilder();

void Process(char c)
{
    var r = ime.Input(c);
    if (!string.IsNullOrEmpty(r.CommittedText))
        result.Append(r.CommittedText);
}

// 안
Process('ㅇ');
Process('ㅏ');
Process('ㄴ');

// 녕
Process('ㄴ');
Process('ㅕ');
Process('ㅇ');

// 하
Process('ㅎ');
Process('ㅏ');

// 세
Process('ㅅ');
Process('ㅔ');

// 요
Process('ㅇ');
Process('ㅛ');

// 확정
var final = ime.Commit();
result.Append(final.CommittedText);

Console.WriteLine(result.ToString());  // "안녕하세요"
```

### 예제 2: "닭갈비" 입력 (복합 종성 포함)

```csharp
var ime = new IME(new KoreanComposer());

// 닭
ime.Input('ㄷ');  // "ㄷ"
ime.Input('ㅏ');  // "다"
ime.Input('ㄹ');  // "달"
ime.Input('ㄱ');  // "닭" (ㄹ+ㄱ=ㄺ)

// 갈
ime.Input('ㄱ');  // 확정: "닭", 조합: "ㄱ"
ime.Input('ㅏ');  // "가"
ime.Input('ㄹ');  // "갈"

// 비
ime.Input('ㅂ');  // 확정: "갈", 조합: "ㅂ"
ime.Input('ㅣ');  // "비"

// 최종 확정
var result = ime.Commit();
// 결과: "닭갈비"
```

### 예제 3: 백스페이스로 단계별 수정

```csharp
var ime = new IME(new KoreanComposer());

// "값" 입력
ime.Input('ㄱ');
ime.Input('ㅏ');
ime.Input('ㅂ');
ime.Input('ㅅ');  // "값"

// 백스페이스로 단계별 삭제
ime.Backspace();  // "값" → "갑" (ㅄ → ㅂ 분해)
ime.Backspace();  // "갑" → "가" (ㅂ 제거)

// 다시 입력
ime.Input('ㄴ');  // "간"

var result = ime.Commit();
Console.WriteLine(result.CommittedText);  // "간"
```

## 제한사항

- 현재 **두벌식 입력만 지원** (세벌식 미지원)
- 옛한글 미지원 (현대 한글 음절만 지원)
- 변환 후보 미지원 (일본어/중국어 IME와 달리 한글은 직접 조합 방식)

## 참고

- 한글 유니코드 범위: U+AC00 ~ U+D7A3 (가 ~ 힣)
- 초성 19개, 중성 21개, 종성 28개 (없음 포함)
- 총 11,172개의 한글 음절 조합 가능


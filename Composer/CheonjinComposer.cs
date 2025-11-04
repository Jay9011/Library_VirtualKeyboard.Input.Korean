using System.Collections.Generic;
using System.Text;
using VirtualKeyboard.Input.Interfaces;
using VirtualKeyboard.Input.Korean.Models;
using VirtualKeyboard.Input.Korean.Utils;
using VirtualKeyboard.Input.Models;

namespace VirtualKeyboard.Input.Korean.Composer
{
    /// <summary>
    /// 천지인 한글 조합기
    /// 삼성 천지인 입력 방식 (7개 기본 자음 + 3개 기본 모음 조합)
    /// </summary>
    public class CheonjinComposer : IInputComposer
    {
        #region 상수

        /// <summary>
        /// 에러 메시지 상수
        /// </summary>
        private static class ErrorMessages
        {
            public const string UnsupportedInput = "지원하지 않는 입력";
            public const string UnprocessableInput = "처리할 수 없는 입력";
            public const string NotComposing = "조합 중이 아님";
            public const string NoCandidateSupport = "천지인은 변환 후보를 지원하지 않습니다";
            public const string InvalidStateType = "잘못된 상태 타입";
        }

        /// <summary>
        /// 자음 → 기본 자음 역매핑 캐시 (성능 최적화)
        /// </summary>
        private static readonly Dictionary<char, char> _consonantToBaseMap = BuildConsonantToBaseMap();

        #endregion

        #region IInputComposer 속성

        public string Name => "천지인 조합기";
        public string Language => "ko-KR";
        public string Description => "천지인 한글 입력 방식 (ㆍ, ㅡ, ㅣ 조합)";

        #endregion

        #region IInputComposer 기본 메서드

        public ICompositionState CreateState()
        {
            return new CheonjinState();
        }

        public void Reset()
        {
            // 상태는 외부(Context)에서 관리하므로 특별한 작업 없음
        }

        public bool CanProcess(string input)
        {
            return CheonjinLibrary.CanProcess(input);
        }

        #endregion

        #region 입력 처리

        public CompositionResult ProcessInput(CompositionContext context, string input)
        {
            if (!CanProcess(input))
                return CompositionResult.Failed(ErrorMessages.UnsupportedInput);

            if (!(context?.State is CheonjinState state))
                return CompositionResult.Failed(ErrorMessages.InvalidStateType);
            char inputChar = input[0];

            // 자음 입력 (ㄱ, ㄴ, ㄷ, ㅂ, ㅅ, ㅈ, ㅇ - 반복시 순환)
            if (CheonjinLibrary.IsBaseConsonant(inputChar))
            {
                return ProcessCheonjinConsonant(state, inputChar);
            }

            // 모음 입력 (ㆍ, ㅡ, ㅣ - 조합)
            if (CheonjinLibrary.IsBaseVowel(inputChar))
            {
                return ProcessCheonjinVowel(state, inputChar);
            }

            return CompositionResult.Failed(ErrorMessages.UnprocessableInput);
        }

        #endregion

        #region 초기화 및 유틸리티

        /// <summary>
        /// 자음 → 기본 자음 역매핑 딕셔너리 생성
        /// </summary>
        private static Dictionary<char, char> BuildConsonantToBaseMap()
        {
            var map = new Dictionary<char, char>();
            foreach (var baseChar in CheonjinLibrary.BASE_CONSONANTS)
            {
                if (CheonjinLibrary.TryGetConsonantCycle(baseChar, out var cycle))
                {
                    foreach (var c in cycle)
                    {
                        map[c] = baseChar;
                    }
                }
            }
            return map;
        }

        /// <summary>
        /// 자음의 기본 자음 반환 (역매핑) - O(1) 성능
        /// </summary>
        private char GetBaseConsonant(char consonant)
        {
            return _consonantToBaseMap.TryGetValue(consonant, out char baseConsonant)
                ? baseConsonant
                : consonant;
        }

        #endregion

        #region 자음 처리

        /// <summary>
        /// 천지인 자음 처리 (같은 자음 반복시 순환)
        /// </summary>
        private CompositionResult ProcessCheonjinConsonant(CheonjinState state, char baseConsonant)
        {
            bool isSameConsonant = state.LastInputConsonant == baseConsonant;

            // 1. PendingJongseong이 있는 상태 처리
            if (state.PendingJongseong != '\0')
            {
                return ProcessPendingJongseongConsonant(state, baseConsonant);
            }

            // 2. 복합 종성 상태에서 같은 자음 반복 → 복합 종성 분해 후 순환
            if (state.IsComplete && isSameConsonant)
            {
                var result = TryProcessComplexJongseongCycle(state, baseConsonant);
                if (result.HasValue) return result.Value;
            }

            // 3. 종성이 있는 상태에서 같은 자음 반복 → 종성 순환
            if (state.IsComplete && isSameConsonant)
            {
                var result = TryProcessSimpleJongseongCycle(state, baseConsonant);
                if (result.HasValue) return result.Value;
            }

            // 4. 종성이 있는 상태에서 다른 자음 입력 → 복합 종성 조합 시도
            if (state.IsComplete && !isSameConsonant)
            {
                return ProcessJongseongCombination(state, baseConsonant);
            }

            // 5. 초성 + 중성 상태에서 자음 입력 → 종성 처리
            if (state.HasChoseongAndJungseong && state.JongseongIndex == 0)
            {
                var result = TryAddNewJongseong(state, baseConsonant);
                if (result.HasValue) return result.Value;
            }

            // 6. 초성만 있는 상태에서 같은 자음 반복 → 순환
            if (state.HasChoseongOnly && isSameConsonant)
            {
                return ProcessChoseongCycle(state, baseConsonant);
            }

            // 7. 새로운 자음 시작 (기본 케이스)
            return ProcessNewConsonant(state, baseConsonant);
        }

        /// <summary>
        /// PendingJongseong이 있는 상태에서 자음 입력 처리
        /// </summary>
        private CompositionResult ProcessPendingJongseongConsonant(CheonjinState state, char baseConsonant)
        {
            char pendingBase = GetBaseConsonant(state.PendingJongseong);

            // 같은 기본 자음이면 순환
            if (pendingBase == baseConsonant)
            {
                return CyclePendingJongseong(state, baseConsonant);
            }

            // 다른 기본 자음이면 현재 음절 확정 후 새 글자 시작
            return CommitAndStartNewConsonant(state, baseConsonant);
        }

        /// <summary>
        /// PendingJongseong 순환 처리
        /// </summary>
        private CompositionResult CyclePendingJongseong(CheonjinState state, char baseConsonant)
        {
            int cycleLength = CheonjinUtil.GetConsonantCycleLength(baseConsonant);
            int nextIndex = (state.ConsonantCycleIndex + 1) % cycleLength;
            char nextConsonant = CheonjinUtil.GetConsonantAtIndex(baseConsonant, nextIndex);

            state.ConsonantCycleIndex = nextIndex;
            state.PendingJongseong = nextConsonant;
            state.LastInputConsonant = baseConsonant;

            // 복합 종성 조합 재시도
            char currentJongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];
            if (HangulUtil.TryCombineJongseong(currentJongseong, nextConsonant, out char combined))
            {
                int combinedIndex = HangulLibrary.GetJongseongIndex(combined);
                if (combinedIndex > 0)
                {
                    state.JongseongIndex = combinedIndex;
                    state.PendingJongseong = '\0';
                }
            }

            return CompositionResult.Succeeded(
                BuildSyllable(state),
                action: ECompositionAction.Update
            );
        }

        /// <summary>
        /// 현재 음절 확정 후 새 자음으로 시작
        /// </summary>
        private CompositionResult CommitAndStartNewConsonant(CheonjinState state, char baseConsonant)
        {
            string committed = BuildSyllable(state);
            state.Reset();

            char newConsonant = CheonjinUtil.GetFirstConsonant(baseConsonant);
            state.LastInputConsonant = baseConsonant;
            state.ConsonantCycleIndex = 0;
            state.CurrentConsonant = newConsonant;
            state.ChoseongIndex = HangulLibrary.GetChoseongIndex(newConsonant);

            return CompositionResult.Succeeded(
                newConsonant.ToString(),
                committedText: committed,
                action: ECompositionAction.Input
            );
        }

        /// <summary>
        /// 복합 종성 분해 후 순환 시도
        /// </summary>
        private CompositionResult? TryProcessComplexJongseongCycle(CheonjinState state, char baseConsonant)
        {
            char currentJongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];

            // 복합 종성이면 분해
            if (!HangulUtil.TryDecomposeJongseong(currentJongseong, out char first, out char second))
                return null;

            // 복합 종성의 두 번째 자음이 현재 순환 중인 자음인지 확인
            char secondBase = GetBaseConsonant(second);
            if (secondBase != baseConsonant)
                return null;

            // 첫 번째만 종성으로, 두 번째는 순환 계속
            state.JongseongIndex = HangulLibrary.GetJongseongIndex(first);

            int cycleLength = CheonjinUtil.GetConsonantCycleLength(baseConsonant);
            int nextIndex = (state.ConsonantCycleIndex + 1) % cycleLength;
            char nextConsonant = CheonjinUtil.GetConsonantAtIndex(baseConsonant, nextIndex);

            state.ConsonantCycleIndex = nextIndex;
            state.PendingJongseong = nextConsonant;

            // 복합 종성 재시도
            if (HangulUtil.TryCombineJongseong(first, nextConsonant, out char combined))
            {
                int combinedIndex = HangulLibrary.GetJongseongIndex(combined);
                if (combinedIndex > 0)
                {
                    state.JongseongIndex = combinedIndex;
                    state.PendingJongseong = '\0';
                }
            }

            return CompositionResult.Succeeded(
                BuildSyllable(state),
                action: ECompositionAction.Update
            );
        }

        /// <summary>
        /// 단일 종성 순환 시도
        /// </summary>
        private CompositionResult? TryProcessSimpleJongseongCycle(CheonjinState state, char baseConsonant)
        {
            char currentJongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];
            char currentBase = GetBaseConsonant(currentJongseong);

            if (currentBase != baseConsonant)
                return null;

            // 종성 순환: 다음 자음으로
            int cycleLength = CheonjinUtil.GetConsonantCycleLength(baseConsonant);
            int nextIndex = (state.ConsonantCycleIndex + 1) % cycleLength;
            char nextConsonant = CheonjinUtil.GetConsonantAtIndex(baseConsonant, nextIndex);

            state.ConsonantCycleIndex = nextIndex;
            state.CurrentConsonant = nextConsonant;

            int nextJongseongIndex = HangulLibrary.GetJongseongIndex(nextConsonant);
            if (nextJongseongIndex <= 0)
                return null;

            state.JongseongIndex = nextJongseongIndex;
            return CompositionResult.Succeeded(
                BuildSyllable(state),
                action: ECompositionAction.Update
            );
        }

        /// <summary>
        /// 종성이 있는 상태에서 다른 자음 입력 → 복합 종성 조합 시도
        /// </summary>
        private CompositionResult ProcessJongseongCombination(CheonjinState state, char baseConsonant)
        {
            char currentJongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];
            char newConsonant = CheonjinUtil.GetFirstConsonant(baseConsonant);

            // 복합 종성 조합 시도
            if (HangulUtil.TryCombineJongseong(currentJongseong, newConsonant, out char combined))
            {
                int combinedIndex = HangulLibrary.GetJongseongIndex(combined);
                if (combinedIndex > 0)
                {
                    state.JongseongIndex = combinedIndex;
                    state.LastInputConsonant = baseConsonant;
                    state.ConsonantCycleIndex = 0;

                    return CompositionResult.Succeeded(
                        BuildSyllable(state),
                        action: ECompositionAction.Update
                    );
                }
            }

            // 조합 불가능하면 PendingJongseong에 저장하고 대기
            state.PendingJongseong = newConsonant;
            state.LastInputConsonant = baseConsonant;
            state.ConsonantCycleIndex = 0;

            return CompositionResult.Succeeded(
                BuildSyllable(state),
                action: ECompositionAction.Input
            );
        }

        /// <summary>
        /// 초성 + 중성 상태에서 종성 추가 시도
        /// </summary>
        private CompositionResult? TryAddNewJongseong(CheonjinState state, char baseConsonant)
        {
            char consonant = CheonjinUtil.GetFirstConsonant(baseConsonant);
            int jongseongIndex = HangulLibrary.GetJongseongIndex(consonant);

            if (jongseongIndex <= 0)
                return null;

            state.JongseongIndex = jongseongIndex;
            state.LastInputConsonant = baseConsonant;
            state.ConsonantCycleIndex = 0;
            state.CurrentConsonant = consonant;

            return CompositionResult.Succeeded(
                BuildSyllable(state),
                action: ECompositionAction.Input
            );
        }

        /// <summary>
        /// 초성만 있는 상태에서 자음 순환
        /// </summary>
        private CompositionResult ProcessChoseongCycle(CheonjinState state, char baseConsonant)
        {
            char nextConsonant = CheonjinUtil.GetNextConsonant(baseConsonant, state.ConsonantCycleIndex);
            state.ConsonantCycleIndex++;
            state.CurrentConsonant = nextConsonant;
            state.ChoseongIndex = HangulLibrary.GetChoseongIndex(nextConsonant);

            return CompositionResult.Succeeded(
                nextConsonant.ToString(),
                action: ECompositionAction.Update
            );
        }

        /// <summary>
        /// 새로운 자음 시작
        /// </summary>
        private CompositionResult ProcessNewConsonant(CheonjinState state, char baseConsonant)
        {
            char consonant = CheonjinUtil.GetFirstConsonant(baseConsonant);

            // 이미 조합 중이면 확정 후 새 글자 시작
            if (state.IsComposing)
            {
                return CommitAndStartNewConsonant(state, baseConsonant);
            }

            // 새 글자 시작
            state.LastInputConsonant = baseConsonant;
            state.ConsonantCycleIndex = 0;
            state.CurrentConsonant = consonant;
            state.ChoseongIndex = HangulLibrary.GetChoseongIndex(consonant);

            return CompositionResult.Succeeded(
                consonant.ToString(),
                action: ECompositionAction.Input
            );
        }

        #endregion

        #region 모음 처리

        /// <summary>
        /// 천지인 모음 조합 처리 (ㆍ, ㅡ, ㅣ)
        /// </summary>
        private CompositionResult ProcessCheonjinVowel(CheonjinState state, char vowel)
        {
            // PendingJongseong이 있는 상태에서 모음 입력 → PendingJongseong을 초성으로 사용
            if (state.PendingJongseong != '\0')
            {
                // PendingJongseong 저장
                char pendingConsonant = state.PendingJongseong;

                // 현재 음절 확정 (PendingJongseong 제외)
                state.PendingJongseong = '\0';
                string committed = BuildSyllable(state);

                // 새 글자 시작
                state.Reset();
                int newChoseongIndex = HangulLibrary.GetChoseongIndex(pendingConsonant);
                state.ChoseongIndex = newChoseongIndex;

                state.VowelSequenceInternal.Add(vowel);
                if (CheonjinUtil.TryCombineVowel(state.VowelSequenceInternal, out char combinedVowel))
                {
                    state.JungseongIndex = HangulLibrary.GetJungseongIndex(combinedVowel);
                    state.HasVowel = true;
                    state.VowelSequenceInternal.Clear();
                    state.VowelSequenceInternal.Add(combinedVowel);
                }

                string newSyllable = BuildSyllable(state);

                return CompositionResult.Succeeded(
                    newSyllable,
                    committedText: committed,
                    action: ECompositionAction.Input
                );
            }

            // 종성이 있는 상태에서 모음 입력 → 종성 분해
            if (state.IsComplete)
            {
                char jongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];

                // 복합 종성이면 분해
                if (HangulUtil.TryDecomposeJongseong(jongseong, out char first, out char second))
                {
                    // 첫 번째는 종성으로 유지, 두 번째는 새 글자의 초성
                    int firstIndex = HangulLibrary.GetJongseongIndex(first);
                    state.JongseongIndex = firstIndex;

                    string currentSyllable = BuildSyllable(state);

                    // 새 글자 시작
                    state.Reset();
                    int newChoseongIndex = HangulLibrary.GetChoseongIndex(second);
                    state.ChoseongIndex = newChoseongIndex;

                    state.VowelSequenceInternal.Add(vowel);
                    if (CheonjinUtil.TryCombineVowel(state.VowelSequenceInternal, out char combinedVowel))
                    {
                        state.JungseongIndex = HangulLibrary.GetJungseongIndex(combinedVowel);
                        state.HasVowel = true;
                    }

                    string newSyllable = BuildSyllable(state);

                    return CompositionResult.Succeeded(
                        newSyllable,
                        committedText: currentSyllable,
                        action: ECompositionAction.Input
                    );
                }
                else
                {
                    // 단일 종성이면 새 글자의 초성으로
                    int newChoseongIndex = HangulLibrary.GetChoseongIndex(jongseong);
                    state.JongseongIndex = 0;

                    string currentSyllable = BuildSyllable(state);

                    state.Reset();
                    state.ChoseongIndex = newChoseongIndex;

                    state.VowelSequenceInternal.Add(vowel);
                    if (CheonjinUtil.TryCombineVowel(state.VowelSequenceInternal, out char combinedVowel))
                    {
                        state.JungseongIndex = HangulLibrary.GetJungseongIndex(combinedVowel);
                        state.HasVowel = true;
                    }

                    string newSyllable = BuildSyllable(state);

                    return CompositionResult.Succeeded(
                        newSyllable,
                        committedText: currentSyllable,
                        action: ECompositionAction.Input
                    );
                }
            }

            // 초성 없이 모음만 있는 경우
            if (state.ChoseongIndex < 0 && state.VowelSequenceInternal.Count > 0)
            {
                // 먼저 기존 시퀀스에 새 모음을 추가하여 조합 시도
                state.VowelSequenceInternal.Add(vowel);

                if (CheonjinUtil.TryCombineVowel(state.VowelSequenceInternal, out char combinedVowel))
                {
                    state.JungseongIndex = HangulLibrary.GetJungseongIndex(combinedVowel);
                    state.HasVowel = true;

                    // 조합 성공 시 시퀀스를 완성된 모음으로 업데이트
                    state.VowelSequenceInternal.Clear();
                    state.VowelSequenceInternal.Add(combinedVowel);

                    return CompositionResult.Succeeded(
                        combinedVowel.ToString(),
                        action: ECompositionAction.Update
                    );
                }

                // 조합 실패 시에도 대기 (나중에 다른 모음이 추가되면 조합될 수 있음)
                // 예: ㆍㆍ (조합 실패) + ㅣ -> ㆍㆍㅣ (ㅕ)
                return CompositionResult.Succeeded(
                    string.Join("", state.VowelSequenceInternal),
                    action: ECompositionAction.Update
                );
            }

            // 모음 시퀀스에 추가
            state.VowelSequenceInternal.Add(vowel);
            int originalCount = state.VowelSequenceInternal.Count;

            // 조합 시도
            if (CheonjinUtil.TryCombineVowel(state.VowelSequenceInternal, out char result))
            {
                int jungseongIndex = HangulLibrary.GetJungseongIndex(result);

                if (jungseongIndex >= 0)
                {
                    state.JungseongIndex = jungseongIndex;
                    state.HasVowel = true;

                    // 조합 성공 시 시퀀스를 완성된 모음으로 업데이트
                    // 이렇게 하면 완성된 모음에 추가 모음을 더해 새로운 조합 가능
                    state.VowelSequenceInternal.Clear();
                    state.VowelSequenceInternal.Add(result);

                    string syllable = BuildSyllable(state);

                    return CompositionResult.Succeeded(
                        syllable,
                        action: originalCount == 1
                            ? ECompositionAction.Input
                            : ECompositionAction.Update
                    );
                }
            }

            // 조합 실패
            // 초성이 있는 경우: 복합 중성 분해 시도 (천지인 특수 로직)
            if (state.ChoseongIndex >= 0 && state.JungseongIndex >= 0)
            {
                char currentJungseong = HangulLibrary.JUNGSEONG[state.JungseongIndex];

                // 복합 중성 분해 시도 (두벌식)
                if (HangulUtil.TryDecomposeJungseong(currentJungseong, out char first, out char second))
                {
                    // second + 새 모음이 천지인 조합 가능한지 확인
                    var testSequence = new List<char> { second, vowel };
                    if (CheonjinUtil.TryCombineVowel(testSequence, out char combinedVowel))
                    {
                        // 조합 가능! 추가된 vowel 제거하고 first만 남기고 현재 음절 확정
                        state.VowelSequenceInternal.RemoveAt(state.VowelSequenceInternal.Count - 1);  // 추가된 vowel 제거
                        state.JungseongIndex = HangulLibrary.GetJungseongIndex(first);
                        state.VowelSequenceInternal.Clear();
                        state.VowelSequenceInternal.Add(first);

                        string committed = BuildSyllable(state);
                        state.Reset();

                        // 새 음절: second + vowel 조합 결과만 저장
                        state.JungseongIndex = HangulLibrary.GetJungseongIndex(combinedVowel);
                        state.HasVowel = true;
                        state.VowelSequenceInternal.Clear();
                        state.VowelSequenceInternal.Add(combinedVowel);

                        return CompositionResult.Succeeded(
                            combinedVowel.ToString(),
                            committedText: committed,
                            action: ECompositionAction.Input
                        );
                    }
                    else
                    {
                        // second + vowel 조합 불가 -> 현재 음절 확정, vowel만 남김
                        state.VowelSequenceInternal.RemoveAt(state.VowelSequenceInternal.Count - 1);  // 추가된 vowel 제거
                        string committed = BuildSyllable(state);
                        state.Reset();

                        // vowel만 새 음절로
                        state.VowelSequenceInternal.Add(vowel);

                        // ㆍ는 한글 중성이 아니므로 그대로 출력
                        return CompositionResult.Succeeded(
                            vowel.ToString(),
                            committedText: committed,
                            action: ECompositionAction.Input
                        );
                    }
                }

                // 복합 중성 분해 실패 또는 조합 불가 - 현재 상태 유지
                return CompositionResult.Succeeded(
                    BuildSyllableInProgress(state),
                    action: ECompositionAction.Input
                );
            }

            // 초성이 있지만 중성이 아직 없는 경우
            if (state.ChoseongIndex >= 0)
            {
                // 현재 상태 유지 - 다음 모음 입력으로 조합될 수 있음
                return CompositionResult.Succeeded(
                    BuildSyllableInProgress(state),
                    action: ECompositionAction.Input
                );
            }

            // 초성 없이 모음만 있는데 조합 실패
            // 천지인에서는 나중에 다른 모음이 추가되면 조합될 수 있으므로 대기
            // 예: ㆍㆍ (조합 실패) + ㅣ -> ㆍㆍㅣ (ㅕ)
            return CompositionResult.Succeeded(
                string.Join("", state.VowelSequenceInternal),
                action: ECompositionAction.Input
            );
        }

        #endregion

        #region 백스페이스 처리

        public CompositionResult ProcessBackspace(CompositionContext context)
        {
            if (!(context?.State is CheonjinState state))
                return CompositionResult.Failed(ErrorMessages.InvalidStateType);

            if (!state.IsComposing && state.PendingJongseong == '\0')
                return CompositionResult.Failed(ErrorMessages.NotComposing);

            // PendingJongseong이 있으면 먼저 제거
            if (state.PendingJongseong != '\0')
            {
                state.PendingJongseong = '\0';
                state.LastInputConsonant = '\0';
                state.ConsonantCycleIndex = 0;

                string syllable = BuildSyllable(state);
                return CompositionResult.Succeeded(
                    syllable,
                    action: ECompositionAction.Delete
                );
            }

            // 종성이 있으면 종성 전체 제거 (블록 단위)
            if (state.JongseongIndex > 0)
            {
                state.JongseongIndex = 0;
                state.ConsonantCycleIndex = 0;
                state.LastInputConsonant = '\0';
                state.CurrentConsonant = '\0';
                string result = BuildSyllable(state);
                return CompositionResult.Succeeded(
                    result,
                    action: ECompositionAction.Delete
                );
            }

            // 모음이 있으면 모음 처리
            if (state.VowelSequenceInternal.Count > 0 && state.JungseongIndex >= 0)
            {
                char jungseong = HangulLibrary.JUNGSEONG[state.JungseongIndex];

                // 복합 중성 분해 시도 (두벌식 분해)
                if (HangulUtil.TryDecomposeJungseong(jungseong, out char first, out char _))
                {
                    // 첫 번째 중성만 남김
                    state.JungseongIndex = HangulLibrary.GetJungseongIndex(first);

                    // VowelSequence도 업데이트 (천지인으로 재구성하기 어려우므로 단순화)
                    state.VowelSequenceInternal.Clear();
                    state.VowelSequenceInternal.Add(first);

                    if (state.ChoseongIndex >= 0)
                    {
                        string syllable = BuildSyllable(state);
                        return CompositionResult.Succeeded(
                            syllable,
                            action: ECompositionAction.Delete
                        );
                    }
                    else
                    {
                        return CompositionResult.Succeeded(
                            first.ToString(),
                            action: ECompositionAction.Delete
                        );
                    }
                }

                // 복합 중성이 아니면 모음 전체 제거
                state.VowelSequenceInternal.Clear();
                state.JungseongIndex = -1;
                state.HasVowel = false;

                if (state.ChoseongIndex >= 0)
                {
                    char consonant = HangulLibrary.CHOSEONG[state.ChoseongIndex];
                    return CompositionResult.Succeeded(
                        consonant.ToString(),
                        action: ECompositionAction.Delete
                    );
                }

                // 초성도 없으면 완전 삭제
                return CompositionResult.Succeeded(
                    "",
                    action: ECompositionAction.Delete
                );
            }

            // 자음만 있으면 초성 전체 제거 (블록 단위)
            if (state.ChoseongIndex >= 0)
            {
                state.Reset();
                return CompositionResult.Succeeded(
                    "",
                    action: ECompositionAction.Delete
                );
            }

            // 그 외의 경우 완전 삭제
            state.Reset();
            return CompositionResult.Succeeded(
                "",
                action: ECompositionAction.Delete
            );
        }

        #endregion

        #region 확정 및 취소

        public CompositionResult Commit(CompositionContext context)
        {
            var state = (CheonjinState)context.State;

            if (!state.IsComposing)
                return CompositionResult.NoChange();

            string result = BuildSyllable(state);
            state.Reset();

            return CompositionResult.Succeeded(
                "",
                committedText: result,
                action: ECompositionAction.Commit
            );
        }

        public CompositionResult Cancel(CompositionContext context)
        {
            var state = (CheonjinState)context.State;

            if (!state.IsComposing)
                return CompositionResult.NoChange();

            state.Reset();
            return CompositionResult.Succeeded(
                "",
                action: ECompositionAction.Cancel
            );
        }

        #endregion

        #region 기타 메서드

        public (bool handled, CompositionResult result) TryProcessSpecialKey(
            CompositionContext context, char key)
        {
            // 천지인 조합기는 특수 키를 직접 처리하지 않음
            return (false, default);
        }

        public CompositionResult SelectCandidate(CompositionContext context, int candidateIndex)
        {
            // 천지인은 변환 후보를 지원하지 않음
            return CompositionResult.Failed(ErrorMessages.NoCandidateSupport);
        }

        #endregion

        #region 음절 생성

        /// <summary>
        /// 현재 상태로 음절 생성
        /// </summary>
        private string BuildSyllable(CheonjinState state)
        {
            // 초성만
            if (state.HasChoseongOnly)
            {
                return HangulLibrary.CHOSEONG[state.ChoseongIndex].ToString();
            }

            // 초성 + 중성 (+ 종성)
            if (state.ChoseongIndex >= 0 && state.JungseongIndex >= 0)
            {
                char syllable = HangulUtil.Compose(
                    state.ChoseongIndex,
                    state.JungseongIndex,
                    state.JongseongIndex
                );

                if (syllable != '\0')
                {
                    // 복합 종성 대기 중인 자음이 있으면 추가 (최대 2글자)
                    if (state.PendingJongseong != '\0')
                    {
                        return syllable.ToString() + state.PendingJongseong;
                    }
                    return syllable.ToString();
                }

                return "";
            }

            // 중성만
            if (state.JungseongIndex >= 0)
            {
                return HangulLibrary.JUNGSEONG[state.JungseongIndex].ToString();
            }

            // 모음 시퀀스만 (조합 안된 경우 - ㆍ 등)
            if (state.VowelSequenceInternal.Count > 0)
            {
                return string.Join("", state.VowelSequenceInternal);
            }

            return "";
        }

        /// <summary>
        /// 조합 중인 음절 생성 (모음이 아직 완성되지 않은 경우)
        /// </summary>
        private string BuildSyllableInProgress(CheonjinState state)
        {
            // 초성 + 모음 시퀀스 (아직 완성 안됨)
            if (state.ChoseongIndex >= 0 && state.VowelSequenceInternal.Count > 0)
            {
                var result = new StringBuilder();
                result.Append(HangulLibrary.CHOSEONG[state.ChoseongIndex]);
                result.Append(string.Join("", state.VowelSequenceInternal));
                return result.ToString();
            }

            // 그 외는 BuildSyllable과 동일
            return BuildSyllable(state);
        }

        #endregion
    }
}


using VirtualKeyboard.Input.Interfaces;
using VirtualKeyboard.Input.Korean.Models;
using VirtualKeyboard.Input.Korean.Utils;
using VirtualKeyboard.Input.Models;

namespace VirtualKeyboard.Input.Korean.Composer
{
    /// <summary>
    /// 한글 조합기 (두벌식)
    /// </summary>
    public class KoreanComposer : IInputComposer
    {
        public string Name => "한글 조합기";
        public string Language => "ko-KR";
        public string Description => "한글 자모 조합 (초성, 중성, 종성)";

        public ICompositionState CreateState()
        {
            return new KoreanState();
        }

        public void Reset()
        {
            // 상태는 외부(Context)에서 관리하므로 특별한 작업 없음
        }

        public bool CanProcess(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length != 1)
                return false;

            char ch = input[0];
            return HangulLibrary.IsJamo(ch);
        }

        public CompositionResult ProcessInput(CompositionContext context, string input)
        {
            if (!CanProcess(input))
                return CompositionResult.Failed("지원하지 않는 입력");

            var state = (KoreanState)context.State;
            char jamo = input[0];

            // 초성 입력
            if (HangulLibrary.IsChoseong(jamo))
            {
                return ProcessChoseong(state, jamo);
            }

            // 중성 입력
            if (HangulLibrary.IsJungseong(jamo))
            {
                return ProcessJungseong(state, jamo);
            }

            return CompositionResult.Failed("처리할 수 없는 입력");
        }

        /// <summary>
        /// 초성 처리
        /// </summary>
        private CompositionResult ProcessChoseong(KoreanState state, char choseong)
        {
            int choseongIndex = HangulLibrary.GetChoseongIndex(choseong);
            if (choseongIndex < 0)
                return CompositionResult.Failed("잘못된 초성");

            // 상태 1: 종성만 있으면 확정 후 새 초성
            if (state.HasJongseongOnly)
            {
                char jongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];
                state.Reset();
                state.ChoseongIndex = choseongIndex;

                return CompositionResult.Succeeded(
                    choseong.ToString(),
                    committedText: jongseong.ToString(),
                    action: ECompositionAction.Input
                );
            }

            // 상태 2: 초성이 없으면 새로 시작
            if (state.ChoseongIndex < 0)
            {
                state.ChoseongIndex = choseongIndex;
                return CompositionResult.Succeeded(
                    choseong.ToString(),
                    action: ECompositionAction.Input
                );
            }

            // 상태 3: 초성만 있으면 복합 종성 조합 시도
            if (state.HasChoseongOnly)
            {
                char currentChoseong = HangulLibrary.CHOSEONG[state.ChoseongIndex];

                // 복합 종성 조합 가능?
                if (HangulUtil.TryCombineJongseong(currentChoseong, choseong, out char combined))
                {
                    int combinedIndex = HangulLibrary.GetJongseongIndex(combined);

                    // 종성만 있는 상태로 전환
                    state.ChoseongIndex = -1;
                    state.JungseongIndex = -1;
                    state.JongseongIndex = combinedIndex;

                    return CompositionResult.Succeeded(
                        combined.ToString(),
                        action: ECompositionAction.Update
                    );
                }

                // 조합 불가능하면 확정 후 새 초성
                string committed = currentChoseong.ToString();
                state.Reset();
                state.ChoseongIndex = choseongIndex;

                return CompositionResult.Succeeded(
                    choseong.ToString(),
                    committedText: committed,
                    action: ECompositionAction.Input
                );
            }

            // 상태 4: 중성까지 있으면 종성으로 시도
            if (state.HasChoseongAndJungseong)
            {
                int jongseongIndex = HangulLibrary.GetJongseongIndex(choseong);
                if (jongseongIndex > 0)
                {
                    state.JongseongIndex = jongseongIndex;
                    string syllable = BuildSyllable(state);
                    return CompositionResult.Succeeded(
                        syllable,
                        action: ECompositionAction.Input
                    );
                }
            }

            // 상태 5: 종성까지 있으면 복합 종성 시도
            if (state.IsComplete)
            {
                char currentJongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];

                // 복합 종성 조합 시도
                if (HangulUtil.TryCombineJongseong(currentJongseong, choseong, out char combined))
                {
                    int combinedIndex = HangulLibrary.GetJongseongIndex(combined);
                    if (combinedIndex > 0)
                    {
                        state.JongseongIndex = combinedIndex;
                        string syllable = BuildSyllable(state);
                        return CompositionResult.Succeeded(
                            syllable,
                            action: ECompositionAction.Update
                        );
                    }
                }

                // 조합 불가능하면 새 글자 시작 (현재 글자 확정하고 새 초성)
                string currentSyllable = BuildSyllable(state);
                state.Reset();
                state.ChoseongIndex = choseongIndex;

                return CompositionResult.Succeeded(
                    choseong.ToString(),
                    committedText: currentSyllable,
                    action: ECompositionAction.Input
                );
            }

            return CompositionResult.Failed("처리할 수 없는 상태");
        }

        /// <summary>
        /// 중성 처리
        /// </summary>
        private CompositionResult ProcessJungseong(KoreanState state, char jungseong)
        {
            int jungseongIndex = HangulLibrary.GetJungseongIndex(jungseong);
            if (jungseongIndex < 0)
                return CompositionResult.Failed("잘못된 중성");

            // 상태 1: 초성이 있으면 조합
            if (state.ChoseongIndex >= 0 && state.JungseongIndex < 0)
            {
                state.JungseongIndex = jungseongIndex;
                string syllable = BuildSyllable(state);
                return CompositionResult.Succeeded(
                    syllable,
                    action: ECompositionAction.Input
                );
            }

            // 상태 2: 중성 + 중성 복합 모음 조합 시도
            if (state.ChoseongIndex >= 0 && state.JungseongIndex >= 0 && state.JongseongIndex == 0)
            {
                char currentJungseong = HangulLibrary.JUNGSEONG[state.JungseongIndex];

                if (HangulUtil.TryCombineJungseong(currentJungseong, jungseong, out char combined))
                {
                    int combinedIndex = HangulLibrary.GetJungseongIndex(combined);
                    if (combinedIndex >= 0)
                    {
                        state.JungseongIndex = combinedIndex;
                        string syllable = BuildSyllable(state);
                        return CompositionResult.Succeeded(
                            syllable,
                            action: ECompositionAction.Update
                        );
                    }
                }
            }

            // 상태 3: 종성이 있으면 분리 (종성 → 초성으로 전환)
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
                    state.JungseongIndex = jungseongIndex;

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
                    state.JungseongIndex = jungseongIndex;

                    string newSyllable = BuildSyllable(state);

                    return CompositionResult.Succeeded(
                        newSyllable,
                        committedText: currentSyllable,
                        action: ECompositionAction.Input
                    );
                }
            }

            // 상태 4: 종성만 있으면 분해 (ㄳ + ㅏ → ㄱ 확정, 사 조합)
            if (state.HasJongseongOnly)
            {
                char jongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];

                // 복합 종성이면 분해
                if (HangulUtil.TryDecomposeJongseong(jongseong, out char first, out char second))
                {
                    // 첫 번째는 확정, 두 번째는 새 글자의 초성
                    int newChoseongIndex = HangulLibrary.GetChoseongIndex(second);
                    state.Reset();
                    state.ChoseongIndex = newChoseongIndex;
                    state.JungseongIndex = jungseongIndex;

                    string newSyllable = BuildSyllable(state);

                    return CompositionResult.Succeeded(
                        newSyllable,
                        committedText: first.ToString(),
                        action: ECompositionAction.Input
                    );
                }
                else
                {
                    // 단일 종성이면 새 글자의 초성으로
                    int newChoseongIndex = HangulLibrary.GetChoseongIndex(jongseong);
                    state.Reset();
                    state.ChoseongIndex = newChoseongIndex;
                    state.JungseongIndex = jungseongIndex;

                    string newSyllable = BuildSyllable(state);

                    return CompositionResult.Succeeded(
                        newSyllable,
                        action: ECompositionAction.Input
                    );
                }
            }

            // 초성 없이 중성만 입력되면 그대로 출력
            if (state.ChoseongIndex < 0)
            {
                return CompositionResult.Succeeded(
                    jungseong.ToString(),
                    action: ECompositionAction.Input
                );
            }

            return CompositionResult.Failed("처리할 수 없는 상태");
        }

        public CompositionResult ProcessBackspace(CompositionContext context)
        {
            var state = (KoreanState)context.State;

            if (!state.IsComposing)
                return CompositionResult.Failed("조합 중이 아님");

            // 종성 있으면 제거
            if (state.JongseongIndex > 0)
            {
                char jongseong = HangulLibrary.JONGSEONG[state.JongseongIndex];

                // 복합 종성이면 분해
                if (HangulUtil.TryDecomposeJongseong(jongseong, out char first, out _))
                {
                    int firstIndex = HangulLibrary.GetJongseongIndex(first);
                    state.JongseongIndex = firstIndex;
                    string syllable = BuildSyllable(state);
                    return CompositionResult.Succeeded(
                        syllable,
                        action: ECompositionAction.Delete
                    );
                }
                else
                {
                    state.JongseongIndex = 0;
                    string syllable = BuildSyllable(state);
                    return CompositionResult.Succeeded(
                        syllable,
                        action: ECompositionAction.Delete
                    );
                }
            }

            // 중성 있으면 제거 또는 분해
            if (state.JungseongIndex >= 0)
            {
                char jungseong = HangulLibrary.JUNGSEONG[state.JungseongIndex];

                // 복합 중성이면 분해
                if (HangulUtil.TryDecomposeJungseong(jungseong, out char first, out _))
                {
                    int firstIndex = HangulLibrary.GetJungseongIndex(first);
                    state.JungseongIndex = firstIndex;
                    string syllable = BuildSyllable(state);
                    return CompositionResult.Succeeded(
                        syllable,
                        action: ECompositionAction.Delete
                    );
                }
                else
                {
                    // 단일 중성이면 제거
                    state.JungseongIndex = -1;
                    string result = state.ChoseongIndex >= 0
                        ? HangulLibrary.CHOSEONG[state.ChoseongIndex].ToString()
                        : "";

                    return CompositionResult.Succeeded(
                        result,
                        action: ECompositionAction.Delete
                    );
                }
            }

            // 초성만 있으면 완전 제거
            if (state.ChoseongIndex >= 0)
            {
                state.Reset();
                return CompositionResult.Succeeded(
                    "",
                    action: ECompositionAction.Delete
                );
            }

            return CompositionResult.Failed("삭제할 내용 없음");
        }

        public CompositionResult Commit(CompositionContext context)
        {
            var state = (KoreanState)context.State;

            if (!state.IsComposing)
                return CompositionResult.NoChange();

            string result = BuildSyllable(state);
            return CompositionResult.Succeeded(
                "",
                committedText: result,
                action: ECompositionAction.Commit
            );
        }

        public CompositionResult Cancel(CompositionContext context)
        {
            var state = (KoreanState)context.State;

            if (!state.IsComposing)
                return CompositionResult.NoChange();

            return CompositionResult.Succeeded(
                "",
                action: ECompositionAction.Cancel
            );
        }

        public (bool handled, CompositionResult result) TryProcessSpecialKey(CompositionContext context, char key)
        {
            // 한글 조합기는 특수 키를 직접 처리하지 않음
            return (false, default);
        }

        public CompositionResult SelectCandidate(CompositionContext context, int candidateIndex)
        {
            // 한글은 변환 후보가 없음
            return CompositionResult.Failed("한글은 변환 후보를 지원하지 않습니다");
        }

        /// <summary>
        /// 현재 상태로 음절 생성
        /// </summary>
        private string BuildSyllable(KoreanState state)
        {
            // 초성만
            if (state.HasChoseongOnly)
            {
                return HangulLibrary.CHOSEONG[state.ChoseongIndex].ToString();
            }

            // 초성 + 중성 (+ 종성 선택)
            if (state.ChoseongIndex >= 0 && state.JungseongIndex >= 0)
            {
                char syllable = HangulUtil.Compose(
                    state.ChoseongIndex,
                    state.JungseongIndex,
                    state.JongseongIndex
                );

                return syllable != '\0' ? syllable.ToString() : "";
            }

            // 중성만 (초성 없이)
            if (state.JungseongIndex >= 0)
            {
                return HangulLibrary.JUNGSEONG[state.JungseongIndex].ToString();
            }

            return "";
        }
    }
}


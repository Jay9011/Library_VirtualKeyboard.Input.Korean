using VirtualKeyboard.Input.Interfaces;
using System.Collections.Generic;

namespace VirtualKeyboard.Input.Korean.Models
{
    /// <summary>
    /// 천지인 조합 상태
    /// </summary>
    public class CheonjinState : ICompositionState
    {
        #region 상수

        /// <summary>
        /// 초성/중성 인덱스가 없음을 나타내는 상수
        /// </summary>
        private const int NO_INDEX = -1;

        /// <summary>
        /// 종성이 없음을 나타내는 상수
        /// </summary>
        private const int NO_JONGSEONG = 0;

        /// <summary>
        /// 자음이 없음을 나타내는 상수
        /// </summary>
        private const char NO_CHAR = '\0';

        #endregion

        #region 자음 관련 상태

        /// <summary>
        /// 마지막 입력된 기본 자음 (ㄱ, ㄴ, ㄷ, ㅂ, ㅅ, ㅈ, ㅇ)
        /// </summary>
        public char LastInputConsonant { get; set; } = NO_CHAR;

        /// <summary>
        /// 현재 자음 순환 인덱스
        /// 0: 첫 번째, 1: 두 번째, 2: 세 번째
        /// </summary>
        public int ConsonantCycleIndex { get; set; } = 0;

        /// <summary>
        /// 현재 조합된 자음
        /// </summary>
        public char CurrentConsonant { get; set; } = NO_CHAR;

        #endregion

        #region 모음 관련 상태

        /// <summary>
        /// 입력된 기본 모음 시퀀스 (ㆍ, ㅡ, ㅣ) - 내부 리스트
        /// </summary>
        private readonly List<char> _vowelSequence = new List<char>();

        /// <summary>
        /// 입력된 기본 모음 시퀀스 (읽기 전용)
        /// </summary>
        public IReadOnlyList<char> VowelSequence => _vowelSequence;

        /// <summary>
        /// 내부적으로 VowelSequence에 접근하기 위한 속성
        /// </summary>
        internal List<char> VowelSequenceInternal => _vowelSequence;

        /// <summary>
        /// 모음이 입력되었는지 여부
        /// </summary>
        public bool HasVowel { get; set; } = false;

        #endregion

        #region 최종 조합 상태

        /// <summary>
        /// 최종 조합된 초성 인덱스 (-1이면 없음)
        /// </summary>
        public int ChoseongIndex { get; set; } = NO_INDEX;

        /// <summary>
        /// 최종 조합된 중성 인덱스 (-1이면 없음)
        /// </summary>
        public int JungseongIndex { get; set; } = NO_INDEX;

        /// <summary>
        /// 최종 조합된 종성 인덱스 (0이면 없음)
        /// </summary>
        public int JongseongIndex { get; set; } = NO_JONGSEONG;

        /// <summary>
        /// 복합 종성 조합 대기 중인 자음 ('\0'이면 없음)
        /// 예: "안ㅅ" 상태에서 'ㅅ'
        /// </summary>
        public char PendingJongseong { get; set; } = NO_CHAR;

        #endregion

        #region 상태 확인 속성

        /// <summary>
        /// 조합 중인지 여부
        /// </summary>
        public bool IsComposing =>
            ChoseongIndex >= NO_INDEX + 1 || JungseongIndex >= NO_INDEX + 1 ||
            VowelSequence.Count > 0 || PendingJongseong != NO_CHAR;

        /// <summary>
        /// 초성만 있는 상태
        /// </summary>
        public bool HasChoseongOnly =>
            ChoseongIndex >= NO_INDEX + 1 && JungseongIndex == NO_INDEX && JongseongIndex == NO_JONGSEONG;

        /// <summary>
        /// 초성 + 중성만 있는 상태
        /// </summary>
        public bool HasChoseongAndJungseong =>
            ChoseongIndex >= NO_INDEX + 1 && JungseongIndex >= NO_INDEX + 1 && JongseongIndex == NO_JONGSEONG;

        /// <summary>
        /// 완성된 음절 (초성 + 중성 + 종성)
        /// </summary>
        public bool IsComplete =>
            ChoseongIndex >= NO_INDEX + 1 && JungseongIndex >= NO_INDEX + 1 && JongseongIndex > NO_JONGSEONG;

        #endregion

        #region ICompositionState 구현

        /// <summary>
        /// 상태 초기화
        /// </summary>
        public void Reset()
        {
            LastInputConsonant = NO_CHAR;
            ConsonantCycleIndex = 0;
            CurrentConsonant = NO_CHAR;
            _vowelSequence.Clear();
            HasVowel = false;
            ChoseongIndex = NO_INDEX;
            JungseongIndex = NO_INDEX;
            JongseongIndex = NO_JONGSEONG;
            PendingJongseong = NO_CHAR;
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public ICompositionState Clone()
        {
            var cloned = new CheonjinState
            {
                LastInputConsonant = LastInputConsonant,
                ConsonantCycleIndex = ConsonantCycleIndex,
                CurrentConsonant = CurrentConsonant,
                HasVowel = HasVowel,
                ChoseongIndex = ChoseongIndex,
                JungseongIndex = JungseongIndex,
                JongseongIndex = JongseongIndex,
                PendingJongseong = PendingJongseong
            };
            cloned._vowelSequence.AddRange(_vowelSequence);
            return cloned;
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        /// </summary>
        public override string ToString()
        {
            return $"Cheonjin[기본자음:{LastInputConsonant}, 순환:{ConsonantCycleIndex}, " +
                   $"초:{ChoseongIndex}, 중:{JungseongIndex}, 종:{JongseongIndex}, " +
                   $"모음시퀀스:{string.Join("", VowelSequence)}]";
        }

        #endregion
    }
}


using VirtualKeyboard.Input.Interfaces;
using System.Collections.Generic;

namespace VirtualKeyboard.Input.Korean.Models
{
    /// <summary>
    /// 천지인 조합 상태
    /// </summary>
    public class CheonjinState : ICompositionState
    {
        #region 자음 관련 상태

        /// <summary>
        /// 마지막 입력된 기본 자음 (ㄱ, ㄴ, ㄷ, ㅂ, ㅅ, ㅈ, ㅇ)
        /// </summary>
        public char LastInputConsonant { get; set; } = '\0';

        /// <summary>
        /// 현재 자음 순환 인덱스
        /// 0: 첫 번째, 1: 두 번째, 2: 세 번째
        /// </summary>
        public int ConsonantCycleIndex { get; set; } = 0;

        /// <summary>
        /// 현재 조합된 자음
        /// </summary>
        public char CurrentConsonant { get; set; } = '\0';

        #endregion

        #region 모음 관련 상태

        /// <summary>
        /// 입력된 기본 모음 시퀀스 (ㆍ, ㅡ, ㅣ)
        /// </summary>
        public List<char> VowelSequence { get; set; } = new List<char>();

        /// <summary>
        /// 모음이 입력되었는지 여부
        /// </summary>
        public bool HasVowel { get; set; } = false;

        #endregion

        #region 최종 조합 상태

        /// <summary>
        /// 최종 조합된 초성 인덱스 (-1이면 없음)
        /// </summary>
        public int ChoseongIndex { get; set; } = -1;

        /// <summary>
        /// 최종 조합된 중성 인덱스 (-1이면 없음)
        /// </summary>
        public int JungseongIndex { get; set; } = -1;

        /// <summary>
        /// 최종 조합된 종성 인덱스 (0이면 없음)
        /// </summary>
        public int JongseongIndex { get; set; } = 0;

        /// <summary>
        /// 복합 종성 조합 대기 중인 자음 ('\0'이면 없음)
        /// 예: "안ㅅ" 상태에서 'ㅅ'
        /// </summary>
        public char PendingJongseong { get; set; } = '\0';

        #endregion

        #region 상태 확인 속성

        /// <summary>
        /// 조합 중인지 여부
        /// </summary>
        public bool IsComposing =>
            ChoseongIndex >= 0 || JungseongIndex >= 0 || VowelSequence.Count > 0 || PendingJongseong != '\0';

        /// <summary>
        /// 초성만 있는 상태
        /// </summary>
        public bool HasChoseongOnly =>
            ChoseongIndex >= 0 && JungseongIndex < 0 && JongseongIndex == 0;

        /// <summary>
        /// 초성 + 중성만 있는 상태
        /// </summary>
        public bool HasChoseongAndJungseong =>
            ChoseongIndex >= 0 && JungseongIndex >= 0 && JongseongIndex == 0;

        /// <summary>
        /// 완성된 음절 (초성 + 중성 + 종성)
        /// </summary>
        public bool IsComplete =>
            ChoseongIndex >= 0 && JungseongIndex >= 0 && JongseongIndex > 0;

        #endregion

        #region ICompositionState 구현

        /// <summary>
        /// 상태 초기화
        /// </summary>
        public void Reset()
        {
            LastInputConsonant = '\0';
            ConsonantCycleIndex = 0;
            CurrentConsonant = '\0';
            VowelSequence.Clear();
            HasVowel = false;
            ChoseongIndex = -1;
            JungseongIndex = -1;
            JongseongIndex = 0;
            PendingJongseong = '\0';
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public ICompositionState Clone()
        {
            return new CheonjinState
            {
                LastInputConsonant = LastInputConsonant,
                ConsonantCycleIndex = ConsonantCycleIndex,
                CurrentConsonant = CurrentConsonant,
                VowelSequence = new List<char>(VowelSequence),
                HasVowel = HasVowel,
                ChoseongIndex = ChoseongIndex,
                JungseongIndex = JungseongIndex,
                JongseongIndex = JongseongIndex,
                PendingJongseong = PendingJongseong
            };
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


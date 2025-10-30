using VirtualKeyboard.Input.Interfaces;

namespace VirtualKeyboard.Input.Korean.Models
{
    /// <summary>
    /// 한글 조합 상태
    /// </summary>
    public class KoreanState : ICompositionState
    {
        /// <summary>
        /// 초성 인덱스 (-1이면 없음)
        /// </summary>
        public int ChoseongIndex { get; set; } = -1;

        /// <summary>
        /// 중성 인덱스 (-1이면 없음)
        /// </summary>
        public int JungseongIndex { get; set; } = -1;

        /// <summary>
        /// 종성 인덱스 (0이면 없음)
        /// </summary>
        public int JongseongIndex { get; set; } = 0;

        /// <summary>
        /// 조합 중인지 여부
        /// </summary>
        public bool IsComposing => ChoseongIndex >= 0 || JungseongIndex >= 0 || JongseongIndex > 0;

        /// <summary>
        /// 초성만 있는 상태
        /// </summary>
        public bool HasChoseongOnly => ChoseongIndex >= 0 && JungseongIndex < 0;

        /// <summary>
        /// 초성 + 중성만 있는 상태
        /// </summary>
        public bool HasChoseongAndJungseong => ChoseongIndex >= 0 && JungseongIndex >= 0 && JongseongIndex == 0;

        /// <summary>
        /// 완성된 음절 (초성 + 중성 + 종성)
        /// </summary>
        public bool IsComplete => ChoseongIndex >= 0 && JungseongIndex >= 0 && JongseongIndex > 0;

        /// <summary>
        /// 종성만 있는 상태 (복합 자모)
        /// 예: "ㄳ", "ㄺ" 등
        /// </summary>
        public bool HasJongseongOnly => ChoseongIndex < 0 && JungseongIndex < 0 && JongseongIndex > 0;

        /// <summary>
        /// 상태 초기화
        /// </summary>
        public void Reset()
        {
            ChoseongIndex = -1;
            JungseongIndex = -1;
            JongseongIndex = 0;
        }

        /// <summary>
        /// 복사본 생성
        /// </summary>
        public ICompositionState Clone()
        {
            return new KoreanState
            {
                ChoseongIndex = ChoseongIndex,
                JungseongIndex = JungseongIndex,
                JongseongIndex = JongseongIndex
            };
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        /// </summary>
        public override string ToString()
        {
            return $"Korean[초:{ChoseongIndex}, 중:{JungseongIndex}, 종:{JongseongIndex}]";
        }
    }
}


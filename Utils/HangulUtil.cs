namespace VirtualKeyboard.Input.Korean.Utils
{
    /// <summary>
    /// 한글 조합/분해 유틸리티 (두벌식 전용)
    /// </summary>
    public static class HangulUtil
    {
        #region 한글 조합/분해

        /// <summary>
        /// 한글 음절인지 확인
        /// </summary>
        public static bool IsHangulSyllable(char ch)
        {
            return ch >= HangulLibrary.HANGUL_BASE && ch <= HangulLibrary.HANGUL_END;
        }

        /// <summary>
        /// 한글 음절 분해
        /// </summary>
        /// <param name="syllable">분해할 한글 음절</param>
        /// <returns>(초성 인덱스, 중성 인덱스, 종성 인덱스)</returns>
        public static (int choseong, int jungseong, int jongseong) Decompose(char syllable)
        {
            if (!IsHangulSyllable(syllable))
                return (-1, -1, -1);

            int code = syllable - HangulLibrary.HANGUL_BASE;
            int choseong = code / (HangulLibrary.JUNGSEONG_COUNT * HangulLibrary.JONGSEONG_COUNT);
            int jungseong = (code % (HangulLibrary.JUNGSEONG_COUNT * HangulLibrary.JONGSEONG_COUNT)) / HangulLibrary.JONGSEONG_COUNT;
            int jongseong = code % HangulLibrary.JONGSEONG_COUNT;

            return (choseong, jungseong, jongseong);
        }

        /// <summary>
        /// 한글 음절 조합
        /// </summary>
        /// <param name="choseong">초성 인덱스</param>
        /// <param name="jungseong">중성 인덱스</param>
        /// <param name="jongseong">종성 인덱스</param>
        /// <returns>조합된 한글 음절 (실패 시 '\0')</returns>
        public static char Compose(int choseong, int jungseong, int jongseong)
        {
            if (choseong < 0 || choseong >= HangulLibrary.CHOSEONG_COUNT)
                return '\0';
            if (jungseong < 0 || jungseong >= HangulLibrary.JUNGSEONG_COUNT)
                return '\0';
            if (jongseong < 0 || jongseong >= HangulLibrary.JONGSEONG_COUNT)
                return '\0';

            int code = HangulLibrary.HANGUL_BASE +
                       (choseong * HangulLibrary.JUNGSEONG_COUNT * HangulLibrary.JONGSEONG_COUNT) +
                       (jungseong * HangulLibrary.JONGSEONG_COUNT) +
                       jongseong;

            return (char)code;
        }

        #endregion

        #region 두벌식 복합 자모 조합/분해

        /// <summary>
        /// 복합 중성 조합 시도 (예: ㅗ + ㅏ = ㅘ)
        /// 두벌식 입력 방식 전용
        /// </summary>
        public static bool TryCombineJungseong(char first, char second, out char combined)
        {
            combined = '\0';

            switch (first)
            {
                case 'ㅗ':
                    if (second == 'ㅏ') { combined = 'ㅘ'; return true; }
                    if (second == 'ㅐ') { combined = 'ㅙ'; return true; }
                    if (second == 'ㅣ') { combined = 'ㅚ'; return true; }
                    break;

                case 'ㅜ':
                    if (second == 'ㅓ') { combined = 'ㅝ'; return true; }
                    if (second == 'ㅔ') { combined = 'ㅞ'; return true; }
                    if (second == 'ㅣ') { combined = 'ㅟ'; return true; }
                    break;

                case 'ㅡ':
                    if (second == 'ㅣ') { combined = 'ㅢ'; return true; }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 복합 중성 분해 (예: ㅘ → ㅗ, ㅏ)
        /// 두벌식 입력 방식 전용
        /// </summary>
        public static bool TryDecomposeJungseong(char jungseong, out char first, out char second)
        {
            first = '\0';
            second = '\0';

            switch (jungseong)
            {
                case 'ㅘ': first = 'ㅗ'; second = 'ㅏ'; return true;
                case 'ㅙ': first = 'ㅗ'; second = 'ㅐ'; return true;
                case 'ㅚ': first = 'ㅗ'; second = 'ㅣ'; return true;
                case 'ㅝ': first = 'ㅜ'; second = 'ㅓ'; return true;
                case 'ㅞ': first = 'ㅜ'; second = 'ㅔ'; return true;
                case 'ㅟ': first = 'ㅜ'; second = 'ㅣ'; return true;
                case 'ㅢ': first = 'ㅡ'; second = 'ㅣ'; return true;
                default: return false;
            }
        }

        /// <summary>
        /// 복합 종성 조합 시도 (예: ㄱ + ㅅ = ㄳ)
        /// 두벌식 입력 방식 전용
        /// </summary>
        public static bool TryCombineJongseong(char first, char second, out char combined)
        {
            combined = '\0';

            switch (first)
            {
                case 'ㄱ':
                    if (second == 'ㅅ') { combined = 'ㄳ'; return true; }
                    break;

                case 'ㄴ':
                    if (second == 'ㅈ') { combined = 'ㄵ'; return true; }
                    if (second == 'ㅎ') { combined = 'ㄶ'; return true; }
                    break;

                case 'ㄹ':
                    if (second == 'ㄱ') { combined = 'ㄺ'; return true; }
                    if (second == 'ㅁ') { combined = 'ㄻ'; return true; }
                    if (second == 'ㅂ') { combined = 'ㄼ'; return true; }
                    if (second == 'ㅅ') { combined = 'ㄽ'; return true; }
                    if (second == 'ㅌ') { combined = 'ㄾ'; return true; }
                    if (second == 'ㅍ') { combined = 'ㄿ'; return true; }
                    if (second == 'ㅎ') { combined = 'ㅀ'; return true; }
                    break;

                case 'ㅂ':
                    if (second == 'ㅅ') { combined = 'ㅄ'; return true; }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 복합 종성 분해 (예: ㄳ → ㄱ, ㅅ)
        /// 두벌식 입력 방식 전용
        /// </summary>
        public static bool TryDecomposeJongseong(char jongseong, out char first, out char second)
        {
            first = '\0';
            second = '\0';

            switch (jongseong)
            {
                case 'ㄳ': first = 'ㄱ'; second = 'ㅅ'; return true;
                case 'ㄵ': first = 'ㄴ'; second = 'ㅈ'; return true;
                case 'ㄶ': first = 'ㄴ'; second = 'ㅎ'; return true;
                case 'ㄺ': first = 'ㄹ'; second = 'ㄱ'; return true;
                case 'ㄻ': first = 'ㄹ'; second = 'ㅁ'; return true;
                case 'ㄼ': first = 'ㄹ'; second = 'ㅂ'; return true;
                case 'ㄽ': first = 'ㄹ'; second = 'ㅅ'; return true;
                case 'ㄾ': first = 'ㄹ'; second = 'ㅌ'; return true;
                case 'ㄿ': first = 'ㄹ'; second = 'ㅍ'; return true;
                case 'ㅀ': first = 'ㄹ'; second = 'ㅎ'; return true;
                case 'ㅄ': first = 'ㅂ'; second = 'ㅅ'; return true;
                default: return false;
            }
        }

        #endregion
    }
}


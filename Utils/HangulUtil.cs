using System;

namespace VirtualKeyboard.Input.Korean.Utils
{
    /// <summary>
    /// 한글 유니코드 조합/분해 유틸리티
    /// </summary>
    public static class HangulUtil
    {
        // 유니코드 범위
        private const int HANGUL_BASE = 0xAC00;  // '가'
        private const int HANGUL_END = 0xD7A3;   // '힣'

        // 조합 상수
        private const int JONGSEONG_COUNT = 28;
        private const int JUNGSEONG_COUNT = 21;
        private const int CHOSEONG_COUNT = 19;

        /// <summary>
        /// 초성 목록 (19개)
        /// </summary>
        public static readonly char[] CHOSEONG = {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        /// <summary>
        /// 중성 목록 (21개)
        /// </summary>
        public static readonly char[] JUNGSEONG = {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        };

        /// <summary>
        /// 종성 목록 (27개 + 없음)
        /// </summary>
        public static readonly char[] JONGSEONG = {
            '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ',
            'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ',
            'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        /// <summary>
        /// 한글 음절인지 확인
        /// </summary>
        public static bool IsHangulSyllable(char ch)
        {
            return ch >= HANGUL_BASE && ch <= HANGUL_END;
        }

        /// <summary>
        /// 초성인지 확인
        /// </summary>
        public static bool IsChoseong(char ch)
        {
            return Array.IndexOf(CHOSEONG, ch) >= 0;
        }

        /// <summary>
        /// 중성인지 확인
        /// </summary>
        public static bool IsJungseong(char ch)
        {
            return Array.IndexOf(JUNGSEONG, ch) >= 0;
        }

        /// <summary>
        /// 종성인지 확인
        /// </summary>
        public static bool IsJongseong(char ch)
        {
            return Array.IndexOf(JONGSEONG, ch) >= 1; // 0번은 없음이므로 제외
        }

        /// <summary>
        /// 한글 음절 분해
        /// </summary>
        public static (int choseong, int jungseong, int jongseong) Decompose(char syllable)
        {
            if (!IsHangulSyllable(syllable))
                return (-1, -1, -1);

            int code = syllable - HANGUL_BASE;
            int choseong = code / (JUNGSEONG_COUNT * JONGSEONG_COUNT);
            int jungseong = (code % (JUNGSEONG_COUNT * JONGSEONG_COUNT)) / JONGSEONG_COUNT;
            int jongseong = code % JONGSEONG_COUNT;

            return (choseong, jungseong, jongseong);
        }

        /// <summary>
        /// 한글 음절 조합
        /// </summary>
        public static char Compose(int choseong, int jungseong, int jongseong)
        {
            if (choseong < 0 || choseong >= CHOSEONG_COUNT)
                return '\0';
            if (jungseong < 0 || jungseong >= JUNGSEONG_COUNT)
                return '\0';
            if (jongseong < 0 || jongseong >= JONGSEONG_COUNT)
                return '\0';

            int code = HANGUL_BASE +
                       (choseong * JUNGSEONG_COUNT * JONGSEONG_COUNT) +
                       (jungseong * JONGSEONG_COUNT) +
                       jongseong;

            return (char)code;
        }

        /// <summary>
        /// 초성 인덱스 찾기
        /// </summary>
        public static int GetChoseongIndex(char ch)
        {
            return Array.IndexOf(CHOSEONG, ch);
        }

        /// <summary>
        /// 중성 인덱스 찾기
        /// </summary>
        public static int GetJungseongIndex(char ch)
        {
            return Array.IndexOf(JUNGSEONG, ch);
        }

        /// <summary>
        /// 종성 인덱스 찾기
        /// </summary>
        public static int GetJongseongIndex(char ch)
        {
            return Array.IndexOf(JONGSEONG, ch);
        }

        /// <summary>
        /// 복합 중성 조합 시도 (예: ㅗ + ㅏ = ㅘ)
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
    }
}


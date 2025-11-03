using System.Collections.Generic;

namespace VirtualKeyboard.Input.Korean.Utils
{
    /// <summary>
    /// 한글 유니코드 관련 상수 및 조회 테이블
    /// .NET Standard 2.0 기준 최적화
    /// </summary>
    public static class HangulLibrary
    {
        #region 유니코드 범위 상수

        /// <summary>
        /// 한글 음절 시작 (가)
        /// </summary>
        public const int HANGUL_BASE = 0xAC00;

        /// <summary>
        /// 한글 음절 끝 (힣)
        /// </summary>
        public const int HANGUL_END = 0xD7A3;

        #endregion

        #region 조합 상수

        /// <summary>
        /// 종성 개수 (없음 포함)
        /// </summary>
        public const int JONGSEONG_COUNT = 28;

        /// <summary>
        /// 중성 개수
        /// </summary>
        public const int JUNGSEONG_COUNT = 21;

        /// <summary>
        /// 초성 개수
        /// </summary>
        public const int CHOSEONG_COUNT = 19;

        #endregion

        #region 자모 배열 (인덱스 → 문자 접근용)

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

        #endregion

        #region 조회 테이블 (문자 → 인덱스 접근용)

        /// <summary>
        /// 초성 문자 → 인덱스 매핑
        /// </summary>
        private static readonly Dictionary<char, int> ChoseongMap;

        /// <summary>
        /// 중성 문자 → 인덱스 매핑
        /// </summary>
        private static readonly Dictionary<char, int> JungseongMap;

        /// <summary>
        /// 종성 문자 → 인덱스 매핑
        /// </summary>
        private static readonly Dictionary<char, int> JongseongMap;

        /// <summary>
        /// 모든 한글 자모 (초성 + 중성 + 종성) - 한글 자모 여부 확인용
        /// </summary>
        private static readonly HashSet<char> AllJamoSet;

        #endregion

        #region Static Constructor (초기화)

        static HangulLibrary()
        {
            // 초성 Dictionary 초기화 (용량 지정으로 리사이징 방지)
            ChoseongMap = new Dictionary<char, int>(CHOSEONG_COUNT);
            for (int i = 0; i < CHOSEONG.Length; i++)
            {
                ChoseongMap[CHOSEONG[i]] = i;
            }

            // 중성 Dictionary 초기화
            JungseongMap = new Dictionary<char, int>(JUNGSEONG_COUNT);
            for (int i = 0; i < JUNGSEONG.Length; i++)
            {
                JungseongMap[JUNGSEONG[i]] = i;
            }

            // 종성 Dictionary 초기화 (0번은 없음이므로 제외)
            JongseongMap = new Dictionary<char, int>(JONGSEONG_COUNT - 1);
            for (int i = 1; i < JONGSEONG.Length; i++) // 0번('\0')은 제외
            {
                JongseongMap[JONGSEONG[i]] = i;
            }

            // 모든 한글 자모를 통합한 HashSet 생성
            AllJamoSet = new HashSet<char>();
            foreach (char ch in CHOSEONG)
                AllJamoSet.Add(ch);
            foreach (char ch in JUNGSEONG)
                AllJamoSet.Add(ch);
            for (int i = 1; i < JONGSEONG.Length; i++) // 0번('\0')은 제외
                AllJamoSet.Add(JONGSEONG[i]);
        }

        #endregion

        #region 빠른 조회 메서드

        /// <summary>
        /// 한글 자모인지 확인 (초성 + 중성 + 종성)
        /// </summary>
        public static bool IsJamo(char ch)
        {
            return AllJamoSet.Contains(ch);
        }

        /// <summary>
        /// 초성 인덱스 찾기
        /// </summary>
        public static int GetChoseongIndex(char ch)
        {
            return ChoseongMap.TryGetValue(ch, out int index) ? index : -1;
        }

        /// <summary>
        /// 중성 인덱스 찾기
        /// </summary>
        public static int GetJungseongIndex(char ch)
        {
            return JungseongMap.TryGetValue(ch, out int index) ? index : -1;
        }

        /// <summary>
        /// 종성 인덱스 찾기
        /// </summary>
        public static int GetJongseongIndex(char ch)
        {
            return JongseongMap.TryGetValue(ch, out int index) ? index : -1;
        }

        /// <summary>
        /// 초성인지 확인
        /// </summary>
        public static bool IsChoseong(char ch)
        {
            return ChoseongMap.ContainsKey(ch);
        }

        /// <summary>
        /// 중성인지 확인
        /// </summary>
        public static bool IsJungseong(char ch)
        {
            return JungseongMap.ContainsKey(ch);
        }

        /// <summary>
        /// 종성인지 확인
        /// </summary>
        public static bool IsJongseong(char ch)
        {
            return JongseongMap.ContainsKey(ch);
        }

        #endregion
    }
}

using System.Collections.Generic;

namespace VirtualKeyboard.Input.Korean.Utils
{
    /// <summary>
    /// 천지인 입력 방식 관련 상수 및 조회 테이블
    /// </summary>
    public static class CheonjinLibrary
    {
        #region 기본 자음/모음

        /// <summary>
        /// 천지인 기본 자음 (7개)
        /// </summary>
        public static readonly char[] BASE_CONSONANTS = { 'ㄱ', 'ㄴ', 'ㄷ', 'ㅂ', 'ㅅ', 'ㅈ', 'ㅇ' };

        /// <summary>
        /// 천지인 기본 모음 (3개)
        /// </summary>
        public static readonly char[] BASE_VOWELS = { 'ㆍ', 'ㅡ', 'ㅣ' };

        /// <summary>
        /// 천지인 아래아 문자
        /// </summary>
        public const char CHEONJIN_DOT = 'ㆍ';

        #endregion

        #region 자음 순환 테이블

        /// <summary>
        /// 천지인 자음 순환 매핑
        /// 같은 키를 반복 입력할 때의 순환 순서
        /// </summary>
        private static readonly Dictionary<char, char[]> ConsonantCycleMap = new Dictionary<char, char[]>
        {
            { 'ㄱ', new[] { 'ㄱ', 'ㅋ', 'ㄲ' } },
            { 'ㄴ', new[] { 'ㄴ', 'ㄹ' } },
            { 'ㄷ', new[] { 'ㄷ', 'ㅌ', 'ㄸ' } },
            { 'ㅂ', new[] { 'ㅂ', 'ㅍ', 'ㅃ' } },
            { 'ㅅ', new[] { 'ㅅ', 'ㅎ', 'ㅆ' } },
            { 'ㅈ', new[] { 'ㅈ', 'ㅊ', 'ㅉ' } },
            { 'ㅇ', new[] { 'ㅇ', 'ㅁ' } },
        };

        #endregion

        #region 모음 조합 테이블

        /// <summary>
        /// 천지인 모음 조합 테이블
        /// 기본 모음(ㆍ, ㅡ, ㅣ)의 시퀀스를 최종 중성으로 변환
        /// </summary>
        private static readonly Dictionary<string, char> VowelCombinationMap = new Dictionary<string, char>
        {
            // 기본 모음 (1개)
            { "ㅣ", 'ㅣ' },
            { "ㅡ", 'ㅡ' },
            
            // 2중 조합
            { "ㅣㆍ", 'ㅏ' },
            { "ㆍㅣ", 'ㅓ' },
            { "ㆍㅡ", 'ㅗ' },
            { "ㅡㆍ", 'ㅜ' },
            { "ㅡㅣ", 'ㅢ' },
            
            // 3중 조합
            { "ㅏㆍ", 'ㅑ' },
            { "ㆍㆍㅣ", 'ㅕ' },
            { "ㆍㆍㅡ", 'ㅛ' },
            { "ㅜㆍ", 'ㅠ' },

            // 3중 조합 추가 (완성된 모음 + 천지인)
            { "ㅚㆍ", 'ㅘ' },
            { "ㅠㅣ", 'ㅝ' },
            
            // 추가 이중모음 조합 (완성된 모음 + 기본 모음)
            { "ㅏㅣ", 'ㅐ' },
            { "ㅑㅣ", 'ㅒ' },
            { "ㅓㅣ", 'ㅔ' },
            { "ㅕㅣ", 'ㅖ' },
            { "ㅘㅣ", 'ㅙ' },
            { "ㅗㅣ", 'ㅚ' },
            { "ㅝㅣ", 'ㅞ' },
            { "ㅜㅣ", 'ㅟ' },
        };

        #endregion

        #region 빠른 조회용 HashSet

        private static readonly HashSet<char> BaseConsonantSet;
        private static readonly HashSet<char> BaseVowelSet;

        static CheonjinLibrary()
        {
            // 기본 자음 HashSet 초기화
            BaseConsonantSet = new HashSet<char>(BASE_CONSONANTS);

            // 기본 모음 HashSet 초기화
            BaseVowelSet = new HashSet<char>(BASE_VOWELS);
        }

        #endregion

        #region 입력 검증 메서드

        /// <summary>
        /// 천지인 기본 자음인지 확인
        /// </summary>
        public static bool IsBaseConsonant(char ch)
        {
            return BaseConsonantSet.Contains(ch);
        }

        /// <summary>
        /// 천지인 기본 모음인지 확인
        /// </summary>
        public static bool IsBaseVowel(char ch)
        {
            return BaseVowelSet.Contains(ch);
        }

        /// <summary>
        /// 천지인 입력 가능한 문자인지 확인 (CanProcess용)
        /// </summary>
        public static bool CanProcess(char ch)
        {
            return IsBaseConsonant(ch) || IsBaseVowel(ch);
        }

        /// <summary>
        /// 천지인 입력 가능한 문자열인지 확인
        /// </summary>
        public static bool CanProcess(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length != 1)
                return false;

            return CanProcess(input[0]);
        }

        #endregion

        #region 자음 순환 조회 메서드

        /// <summary>
        /// 자음 순환 배열 조회
        /// </summary>
        public static bool TryGetConsonantCycle(char baseConsonant, out char[] cycle)
        {
            return ConsonantCycleMap.TryGetValue(baseConsonant, out cycle);
        }

        /// <summary>
        /// 자음 순환 길이 조회
        /// </summary>
        public static int GetConsonantCycleLength(char baseConsonant)
        {
            if (ConsonantCycleMap.TryGetValue(baseConsonant, out var cycle))
                return cycle.Length;
            return 0;
        }

        #endregion

        #region 모음 조합 조회 메서드

        /// <summary>
        /// 모음 시퀀스 조합 결과 조회
        /// </summary>
        public static bool TryGetVowelCombination(string sequence, out char result)
        {
            return VowelCombinationMap.TryGetValue(sequence, out result);
        }

        /// <summary>
        /// 모음 조합 가능 여부 확인
        /// </summary>
        public static bool CanCombineVowel(string sequence)
        {
            return VowelCombinationMap.ContainsKey(sequence);
        }

        #endregion
    }
}


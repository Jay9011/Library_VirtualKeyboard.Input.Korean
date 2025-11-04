using System.Collections.Generic;

namespace VirtualKeyboard.Input.Korean.Utils
{
    /// <summary>
    /// 천지인 자모 조합 유틸리티
    /// </summary>
    public static class CheonjinUtil
    {
        #region 자음 순환 처리

        /// <summary>
        /// 기본 자음의 첫 번째 자음 반환
        /// </summary>
        /// <param name="baseConsonant">기본 자음 (ㄱ, ㄴ, ㄷ, ㅂ, ㅅ, ㅈ, ㅇ)</param>
        /// <returns>첫 번째 자음 (실패시 입력값 그대로 반환)</returns>
        public static char GetFirstConsonant(char baseConsonant)
        {
            if (CheonjinLibrary.TryGetConsonantCycle(baseConsonant, out var cycle))
                return cycle[0];
            return baseConsonant;
        }

        /// <summary>
        /// 기본 자음의 다음 자음 반환 (순환)
        /// </summary>
        /// <param name="baseConsonant">기본 자음</param>
        /// <param name="currentIndex">현재 인덱스</param>
        /// <returns>다음 자음 (순환)</returns>
        public static char GetNextConsonant(char baseConsonant, int currentIndex)
        {
            if (CheonjinLibrary.TryGetConsonantCycle(baseConsonant, out var cycle))
            {
                int nextIndex = (currentIndex + 1) % cycle.Length;
                return cycle[nextIndex];
            }
            return baseConsonant;
        }

        /// <summary>
        /// 기본 자음의 특정 인덱스 자음 반환
        /// </summary>
        /// <param name="baseConsonant">기본 자음</param>
        /// <param name="index">인덱스</param>
        /// <returns>해당 인덱스의 자음 (실패시 입력값 그대로 반환)</returns>
        public static char GetConsonantAtIndex(char baseConsonant, int index)
        {
            if (CheonjinLibrary.TryGetConsonantCycle(baseConsonant, out var cycle))
            {
                if (index >= 0 && index < cycle.Length)
                    return cycle[index];
            }
            return baseConsonant;
        }

        /// <summary>
        /// 자음 순환 길이 반환
        /// </summary>
        public static int GetConsonantCycleLength(char baseConsonant)
        {
            return CheonjinLibrary.GetConsonantCycleLength(baseConsonant);
        }

        #endregion

        #region 모음 조합 처리

        /// <summary>
        /// 천지인 모음 시퀀스를 조합
        /// </summary>
        /// <param name="sequence">기본 모음 시퀀스 (ㆍ, ㅡ, ㅣ)</param>
        /// <param name="result">조합된 중성</param>
        /// <returns>조합 성공 여부</returns>
        public static bool TryCombineVowel(List<char> sequence, out char result)
        {
            result = '\0';

            if (sequence == null || sequence.Count == 0)
                return false;

            string key = new string(sequence.ToArray());
            return CheonjinLibrary.TryGetVowelCombination(key, out result);
        }

        /// <summary>
        /// 천지인 모음 시퀀스 조합 가능 여부 확인
        /// </summary>
        public static bool CanCombineVowel(List<char> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return false;

            string key = new string(sequence.ToArray());
            return CheonjinLibrary.CanCombineVowel(key);
        }

        /// <summary>
        /// 모음 시퀀스에서 마지막 요소를 제거하고 재조합 시도
        /// </summary>
        /// <param name="sequence">모음 시퀀스</param>
        /// <param name="result">재조합된 결과</param>
        /// <returns>재조합 성공 여부</returns>
        public static bool TryRemoveLastAndRecombine(List<char> sequence, out char result)
        {
            result = '\0';

            if (sequence == null || sequence.Count == 0)
                return false;

            // 마지막 요소 임시 제거
            var lastIndex = sequence.Count - 1;
            var lastChar = sequence[lastIndex];
            sequence.RemoveAt(lastIndex);

            bool success = false;
            if (sequence.Count > 0)
            {
                success = TryCombineVowel(sequence, out result);
            }

            // 실패하면 다시 추가
            if (!success)
            {
                sequence.Add(lastChar);
            }

            return success;
        }

        #endregion

        #region 완성된 모음에서 이중모음 조합

        /// <summary>
        /// 완성된 모음에 ㅣ를 추가하여 이중모음 생성 가능 여부 확인
        /// (예: ㅏ + ㅣ → ㅐ)
        /// </summary>
        public static bool CanCombineWithI(char completedVowel, out char result)
        {
            result = '\0';

            string key = completedVowel.ToString() + 'ㅣ';
            return CheonjinLibrary.TryGetVowelCombination(key, out result);
        }

        #endregion
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Firebase Realtime Database REST 기반 온라인 리더보드(닉네임+점수).
/// 추가 패키지 없이 UnityWebRequest만 사용. 점수 등록(POST push) / 상위 조회(GET 전체 후 정렬).
/// DB URL은 인스펙터(_databaseUrl)에 PM이 설정. 미설정/오프라인이면 graceful 실패(콜백 false/null).
///
/// 치팅 주의: 클라이언트가 점수를 직접 전송하므로 조작 가능(캐주얼 인디 기준 감수).
/// </summary>
public class OnlineLeaderboard : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string nickname;
        public int score;
    }

    [Tooltip("Firebase Realtime Database URL (예: https://yourproj-default-rtdb.firebaseio.com). PM이 설정.")]
    [SerializeField] private string _databaseUrl = "";
    [Tooltip("점수를 저장할 노드 이름.")]
    [SerializeField] private string _node = "leaderboard";
    [Tooltip("요청 타임아웃(초).")]
    [SerializeField] private int _timeout = 10;

    public static OnlineLeaderboard Instance { get; private set; }

    /// <summary>DB URL이 설정되어 온라인 기능 사용 가능한지.</summary>
    public bool IsConfigured => !string.IsNullOrEmpty(_databaseUrl) && _databaseUrl.StartsWith("http");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>점수 등록. 성공 여부를 onDone으로 콜백.</summary>
    public void SubmitScore(string nickname, int score, Action<bool> onDone)
    {
        StartCoroutine(SubmitRoutine(nickname, score, onDone));
    }

    private IEnumerator SubmitRoutine(string nickname, int score, Action<bool> onDone)
    {
        if (!IsConfigured) { onDone?.Invoke(false); yield break; }

        var entry = new Entry { nickname = Sanitize(nickname), score = score };
        string json = JsonUtility.ToJson(entry);
        string url = $"{_databaseUrl}/{_node}.json";

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = _timeout;
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            if (!ok) Debug.LogWarning("[OnlineLeaderboard] 점수 등록 실패: " + req.error);
            onDone?.Invoke(ok);
        }
    }

    /// <summary>상위 N개 조회(점수 내림차순). 실패 시 null 콜백.</summary>
    public void FetchTop(int count, Action<List<Entry>> onResult)
    {
        StartCoroutine(FetchRoutine(count, onResult));
    }

    private IEnumerator FetchRoutine(int count, Action<List<Entry>> onResult)
    {
        if (!IsConfigured) { onResult?.Invoke(null); yield break; }

        string url = $"{_databaseUrl}/{_node}.json";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = _timeout;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[OnlineLeaderboard] 조회 실패: " + req.error);
                onResult?.Invoke(null);
                yield break;
            }

            var list = ParseEntries(req.downloadHandler.text);
            list.Sort((a, b) => b.score.CompareTo(a.score));
            if (count > 0 && list.Count > count) list = list.GetRange(0, count);
            onResult?.Invoke(list);
        }
    }

    /// <summary>Firebase가 반환하는 {key:{...}} 맵에서 평탄한 엔트리 객체들을 추출.</summary>
    private static List<Entry> ParseEntries(string json)
    {
        var list = new List<Entry>();
        if (string.IsNullOrEmpty(json) || json == "null") return list;

        // 각 엔트리는 중첩 없는 {"nickname":"..","score":N} 형태 → 내부 객체만 매칭
        var matches = System.Text.RegularExpressions.Regex.Matches(json, "\\{[^{}]*\\}");
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            try
            {
                var e = JsonUtility.FromJson<Entry>(m.Value);
                if (e != null && !string.IsNullOrEmpty(e.nickname)) list.Add(e);
            }
            catch { /* 무시 */ }
        }
        return list;
    }

    /// <summary>닉네임 정제: 따옴표/제어문자 제거, 길이 제한.</summary>
    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "익명";
        s = s.Replace("\"", "").Replace("\\", "").Replace("\n", "").Replace("\r", "").Trim();
        if (s.Length > 3) s = s.Substring(0, 3);
        return string.IsNullOrEmpty(s) ? "익명" : s;
    }
}

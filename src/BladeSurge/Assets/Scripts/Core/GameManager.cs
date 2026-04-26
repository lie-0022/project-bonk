using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Starting, Playing, Paused, GameOver, Win }

/// <summary>
/// 게임 상태 머신 싱글턴. 모든 상태 전환은 이 클래스를 통해서만 이루어진다.
/// 다른 시스템은 OnGameStateChanged 이벤트를 구독해 상태에 반응한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>상태 전환 시 브로드캐스트. 모든 시스템이 구독한다.</summary>
    public static event Action<GameState> OnGameStateChanged;

    /// <summary>현재 게임 상태. 읽기 전용 — 변경은 ChangeState()를 사용할 것.</summary>
    public GameState CurrentState { get; private set; } = GameState.Starting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ObjectPool.Instance.Initialize();
        PickupPool.Instance.Initialize();
        XPSystem.Instance.Initialize();
        GoldSystem.Instance.Initialize();

        ChangeState(GameState.Playing);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // ESC: Playing ↔ Paused 토글
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.Playing)
                ChangeState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }

        // R: GameOver / Win 상태에서 재시작
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Win)
                RestartGame();
        }
    }

    /// <summary>
    /// 게임 상태를 전환한다. 동일 상태로의 중복 전환은 무시한다.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Time.timeScale = (newState == GameState.Paused) ? 0f : 1f;
        OnGameStateChanged?.Invoke(newState);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

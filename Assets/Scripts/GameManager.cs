using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Game Over References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverScoreText;

    [Header("Drop Prefabs")]
    [SerializeField] private GameObject gemPrefab;

    private int score = 0;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false); // Hide game over panel on start
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    public int GetScore()
    {
        return score;
    }

    public void SpawnGem(Vector3 position)
    {
        if (gemPrefab != null)
        {
            Instantiate(gemPrefab, position, Quaternion.identity);
        }
    }

    public void OnPlayerDeath()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Gems Collected: " + score;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}

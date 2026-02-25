using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatManager : MonoBehaviour
{
    [Header("Yöneticiler")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private AudioManager audioManager;

    [Header("Kamera Efekti")]
    [SerializeField] private CameraShake cameraShake;

    [Header("Titreşim Ayarları")]
    [SerializeField] private float hitShakeIntensity = 0.05f;
    [SerializeField] private float hurtShakeIntensity = 0.15f;

    [Header("UI Bağlantıları")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider healthBar;

    [Header("Görsel Efektler")]
    [SerializeField] private SpriteRenderer monsterSprite;
    [SerializeField] private ParticleSystem bloodEffect;

    [Header("Savaş Ayarları")]
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private float warningTime = 0.5f;
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private float defenseWindow = 0.8f;

    private float maxHealth;
    private float currentMonsterHealth;
    private float damagePerHit;

    private MonsterState currentState = MonsterState.Idle;
    private AttackDirection currentAttackDirection = AttackDirection.None;
    private bool playerResponded = false;
    private int comboCount = 0;
    private bool isBattleOver = true;

    private enum MonsterState { Idle, Preparing, Attacking, Stunned }
    private enum AttackDirection { None, Right, Left, Up }

    void OnEnable() { InputManager.OnSwipeDetected += OnPlayerSwipe; }
    void OnDisable() { InputManager.OnSwipeDetected -= OnPlayerSwipe; }

    // --- GÜNCELLENMİŞ START BATTLE (Max ve Mevcut Can Ayrı Geliyor) ---
    public void StartBattle(float maxHP, float currentHP, float playerDmg, bool isBoss, float atkSpeed, float warnTime, float stunTime, string startMessage = "")
    {
        StopAllCoroutines();
        isBattleOver = false;
        currentState = MonsterState.Idle;

        // BURASI DEĞİŞTİ: Max ve Current ayrı ayarlanıyor
        maxHealth = maxHP;           // Gerçek toplam can (Örn: 100)
        currentMonsterHealth = currentHP; // Başlangıç canı (Örn: 70 - Kurt vurduysa)

        damagePerHit = playerDmg;

        attackInterval = atkSpeed;
        warningTime = warnTime;
        stunDuration = stunTime;

        comboCount = 0;
        UpdateHealthBar(); // Bu fonksiyon (current / max) yaptığı için bar artık eksik başlayacak!

        // Mesaj Gösterimi
        if (!string.IsNullOrEmpty(startMessage))
        {
            UpdateStatusText(startMessage, Color.cyan);
            if (monsterSprite != null) monsterSprite.color = new Color(1f, 0.5f, 0.5f);
        }
        else if (isBoss)
        {
            UpdateStatusText("⚠️ BOSS SAVAŞI!", Color.red);
            if (monsterSprite != null) monsterSprite.color = new Color(1f, 0.5f, 0.5f);
        }
        else
        {
            UpdateStatusText("AV BAŞLIYOR...", Color.white);
            if (monsterSprite != null) monsterSprite.color = Color.white;
        }

        StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        yield return new WaitForSeconds(2f); // Mesaj okunsun diye süreyi azıcık uzattım
        while (!isBattleOver)
        {
            if (currentState == MonsterState.Stunned)
            {
                yield return new WaitForSeconds(stunDuration);
                if (!isBattleOver)
                {
                    currentState = MonsterState.Idle;
                    if (monsterSprite != null) monsterSprite.color = Color.white;
                    statusText.text = "Canavar Kendine Geldi!";
                }
            }

            currentState = MonsterState.Idle;
            yield return new WaitForSeconds(attackInterval);
            if (isBattleOver) yield break;

            PrepareAttack();
            yield return new WaitForSeconds(warningTime);

            ExecuteAttack();
            yield return new WaitForSeconds(defenseWindow);

            if (!playerResponded && currentState == MonsterState.Attacking) OnPlayerMissed();
        }
    }

    // ... (STANDART FONKSİYONLAR AYNI) ...
    private void PrepareAttack()
    {
        currentState = MonsterState.Preparing; playerResponded = false;
        int rand = UnityEngine.Random.Range(0, 3);
        currentAttackDirection = (AttackDirection)(rand + 1);
        string msg = currentAttackDirection == AttackDirection.Right ? "SAĞDAN! ➡️" : currentAttackDirection == AttackDirection.Left ? "SOLDAN! ⬅️" : "YUKARIDAN! ⬇️";
        UpdateStatusText("⚠️ " + msg, Color.yellow);
    }
    private void ExecuteAttack() { currentState = MonsterState.Attacking; statusText.text = "‼️ SALDIRI!"; if (audioManager) audioManager.PlayAttack(); }
    private void OnPlayerSwipe(InputManager.SwipeDirection dir, float d, float t)
    {
        if (isBattleOver) return;
        if (currentState == MonsterState.Stunned) DealDamage();
        else if (currentState == MonsterState.Attacking)
        {
            if (playerResponded) return; playerResponded = true;
            if (CheckDefense(dir)) OnSuccessfulParry(); else OnFailedDefense();
        }
    }
    private bool CheckDefense(InputManager.SwipeDirection dir)
    {
        return currentAttackDirection switch { AttackDirection.Right => dir == InputManager.SwipeDirection.Left, AttackDirection.Left => dir == InputManager.SwipeDirection.Right, AttackDirection.Up => dir == InputManager.SwipeDirection.Down, _ => false };
    }
    private void OnSuccessfulParry() { currentState = MonsterState.Stunned; comboCount = 0; UpdateStatusText("✅ PARRY!", Color.green); if (audioManager) audioManager.PlayParry(); }
    private void OnFailedDefense() { currentState = MonsterState.Idle; UpdateStatusText("❌ HASAR ALDIN!", Color.red); if (gameManager) gameManager.TakeDamage(); if (audioManager) audioManager.PlayPlayerHit(); if (cameraShake) StartCoroutine(cameraShake.Shake(0.3f, hurtShakeIntensity)); }
    private void OnPlayerMissed() { OnFailedDefense(); }

    private void DealDamage()
    {
        comboCount++;
        currentMonsterHealth -= damagePerHit;
        if (bloodEffect != null) bloodEffect.Play();
        if (audioManager != null) audioManager.PlayMonsterHit();
        if (cameraShake != null) StartCoroutine(cameraShake.Shake(0.1f, hitShakeIntensity));
        UpdateHealthBar();
        UpdateStatusText($"⚔️ (x{comboCount})", new Color(1f, 0.5f, 0f));
        StartCoroutine(FlashEffect());
        if (currentMonsterHealth <= 0) Die();
    }

    private void Die()
    {
        isBattleOver = true;
        StopAllCoroutines();
        if (monsterSprite != null) monsterSprite.gameObject.SetActive(false);
        UpdateStatusText("🏆 ZAFER!", Color.green);
        if (gameManager != null) gameManager.OnVictory();
    }

    private void UpdateStatusText(string msg, Color col) { if (statusText != null) { statusText.text = msg; statusText.color = col; } }
    private void UpdateHealthBar() { if (healthBar != null) healthBar.value = currentMonsterHealth / maxHealth; }
    private IEnumerator FlashEffect() { if (monsterSprite) { monsterSprite.color = Color.red; yield return new WaitForSeconds(0.1f); monsterSprite.color = Color.white; } }
}
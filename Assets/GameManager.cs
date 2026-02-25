using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Panel Referansları")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject combatPanel;
    [SerializeField] private GameObject cardPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject shopPanel;

    [Header("Kart Sistemi")]
    [SerializeField] private GameObject cardObject;
    [SerializeField] private CardController cardController;
    [SerializeField] private List<CardData> allCards;

    [Header("Özel Hikaye Kartları (YENİ)")]
    [Tooltip("Askeri kurtardıysak Level 8'de çıkacak kart")]
    [SerializeField] private CardData soldierRewardCard;
    [SerializeField] private CardData merchantRewardCard;
    [SerializeField] private CardData swampRewardCard;

    [Header("Oyun Objeleri")]
    [SerializeField] private GameObject monsterObject;
    [SerializeField] private CombatManager combatManager;

    [Header("UI Textleri (Stats)")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text potionText;
    [SerializeField] private TMP_Text levelText;

    [Header("Butonlar")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button openShopButton;
    [SerializeField] private Button buyPotionButton;
    [SerializeField] private Button upgradeDamageButton;
    [SerializeField] private Button leaveShopButton;
    [SerializeField] private Button drinkButton;

    // --- OYUNCU DEĞERLERİ ---
    private int playerGold = 0;
    private int playerHealth = 100;
    private int maxHealth = 100;
    private int playerPotions = 1;
    private int playerDamage = 10;
    private int currentLevel = 1;

    // Mağaza Fiyatları
    private int potionPrice = 50;
    private int damageUpgradePrice = 150;

    // Hikaye Hafızası
    private Dictionary<string, bool> storyFlags = new Dictionary<string, bool>();
    private CardData currentCardData;

    void Start()
    {
        LoadGameData();
        UpdateStatsUI();
        UpdateLevelUI();
        ShowMenu();

        if (startButton != null) startButton.onClick.AddListener(OpenCardSelection);
        if (openShopButton != null) openShopButton.onClick.AddListener(ShowShop);
        if (buyPotionButton != null) buyPotionButton.onClick.AddListener(BuyPotion);
        if (upgradeDamageButton != null) upgradeDamageButton.onClick.AddListener(BuyDamageUpgrade);
        if (leaveShopButton != null) leaveShopButton.onClick.AddListener(ShowMenu);
        if (drinkButton != null) drinkButton.onClick.AddListener(DrinkPotionAction);
    }

    // --- KART SEÇİMİ (HİKAYE MANTIĞI BURADA) ---
    public void OpenCardSelection()
    {
        if (playerHealth <= 0) { ResetGame(); return; }
        SetActivePanel(cardPanel);
        cardObject.SetActive(true);
        PickCard();
    }

    private void PickCard()
    {
        // 1. HİKAYE KONTROLÜ: ASKERİN DÖNÜŞÜ
        // Level 8 ise + Asker Kurtarıldıysa + Ödül henüz verilmediyse
        if (currentLevel == 8 && storyFlags.ContainsKey("AskeriKurtardi") && !storyFlags.ContainsKey("AskerOduluVerildi"))
        {
            currentCardData = soldierRewardCard;
            storyFlags.Add("AskerOduluVerildi", true); // Not al, bir daha çıkmasın

            if (cardController != null) cardController.SetupCard(currentCardData);
            Debug.Log("🎖️ ÖZEL KART: Asker geri döndü!");
            return; // Rastgele seçime gitme, buradan çık
        }
        if (currentLevel == 12 && storyFlags.ContainsKey("CocugaYardim") && !storyFlags.ContainsKey("TuccarOduluVerildi"))
        {
            currentCardData = merchantRewardCard;
            storyFlags.Add("TuccarOduluVerildi", true); // Bir daha çıkmasın
            if (cardController != null) cardController.SetupCard(currentCardData);
            Debug.Log("💰 ÖZEL KART: Tüccar geldi!");
            return;
        }
        if (currentLevel == 17 && storyFlags.ContainsKey("ZehirBagisikligi") && !storyFlags.ContainsKey("BataklikGecildi"))
        {
            currentCardData = swampRewardCard;
            storyFlags.Add("BataklikGecildi", true); // Bir daha çıkmasın

            if (cardController != null) cardController.SetupCard(currentCardData);
            Debug.Log("🧪 ÖZEL KART: Zehir bağışıklığı işe yaradı!");
            return;
        }

        // 2. NORMAL RASTGELE KART
        if (allCards.Count > 0)
        {
            int randomIndex = Random.Range(0, allCards.Count);
            currentCardData = allCards[randomIndex];
            if (cardController != null) cardController.SetupCard(currentCardData);
        }
    }

    // --- SAVAŞ BAŞLATMA (BONUSLAR BURADA HESAPLANIYOR) ---
    public void StartGame()
    {
        SetActivePanel(combatPanel);
        monsterObject.SetActive(true);

        // 1. Temel Zorluk
        bool isBoss = (currentLevel % 5 == 0);
        float monsterMaxHealth = 50f + (currentLevel * 25f);
        if (isBoss) monsterMaxHealth *= 1.5f;

        // 2. Oyuncu Gücünü Hesapla (Bonuslar Dahil)
        float finalPlayerDamage = playerDamage;

        if (storyFlags.ContainsKey("KeskinKilic")) finalPlayerDamage += 2; // Bileyci bonusu
        if (storyFlags.ContainsKey("AskerOduluVerildi")) finalPlayerDamage += 5; // Asker kılıcı

        // 3. Vampir Laneti (Güçlü vur ama canın eksik başla)
        if (storyFlags.ContainsKey("VampirLaneti"))
        {
            finalPlayerDamage *= 1.5f; // %50 daha fazla hasar
            // Canı %20'ye düşürme, mevcut canın %20'sini sil
            int healthPenalty = (int)(maxHealth * 0.2f);
            if (playerHealth > healthPenalty)
            {
                playerHealth -= healthPenalty;
                UpdateStatsUI();
            }
            Debug.Log("🧛 Vampir Laneti: Kan feda edildi, güç arttı!");
        }

        // 4. Kurt Yardımı (Sadece Bossta)
        float currentMonsterHP = monsterMaxHealth;
        string customMessage = "";

        if (isBoss && storyFlags.ContainsKey("KurduKurtardi"))
        {
            currentMonsterHP -= (monsterMaxHealth * 0.3f); // Boss canının %30'u gider
            customMessage = "🐺 KURT SALDIRDI! (-%30 CAN)";
        }

        // 5. Hız Ayarları
        float atkSpeed = Mathf.Max(1.0f, 3.0f - (currentLevel * 0.1f));
        float warnTime = Mathf.Max(0.4f, 0.8f - (currentLevel * 0.03f));
        float stunTime = Mathf.Max(2.0f, 3.0f - (currentLevel * 0.05f));
        if (isBoss) atkSpeed *= 0.8f;

        // CombatManager'a Gönder
        if (combatManager != null)
        {
            combatManager.StartBattle(monsterMaxHealth, currentMonsterHP, finalPlayerDamage, isBoss, atkSpeed, warnTime, stunTime, customMessage);
        }
    }

    // --- BAKİYE KONTROLÜ ---
    public bool CanAffordCardChoice(bool isRightChoice)
    {
        if (currentCardData == null) return true;
        int goldChange = isRightChoice ? currentCardData.rightGoldEffect : currentCardData.leftGoldEffect;
        int potionChange = isRightChoice ? currentCardData.rightPotionEffect : currentCardData.leftPotionEffect;

        if (goldChange < 0 && (playerGold + goldChange < 0)) return false;
        if (potionChange < 0 && (playerPotions + potionChange < 0)) return false;
        return true;
    }

    public void ApplyCardEffect(bool isRightChoice)
    {
        if (currentCardData == null) return;
        CardData data = currentCardData;

        if (isRightChoice) ApplyEffects(data.rightGoldEffect, data.rightHealthEffect, data.rightPotionEffect, data.storyFlagOnRight);
        else ApplyEffects(data.leftGoldEffect, data.leftHealthEffect, data.leftPotionEffect, data.storyFlagOnLeft);

        SaveGameData();
    }

    private void ApplyEffects(int gold, int health, int potion, string flag)
    {
        AddGold(gold);
        AddHealth(health);
        AddPotion(potion);
        if (!string.IsNullOrEmpty(flag) && !storyFlags.ContainsKey(flag)) storyFlags.Add(flag, true);
    }

    // --- DİĞER SİSTEMLER ---
    public void DrinkPotionAction()
    {
        if (playerPotions <= 0 || playerHealth >= maxHealth) return;
        AddPotion(-1);
        AddHealth(30);
        SaveGameData();
    }

    public void BuyPotion() { if (playerGold >= potionPrice) { AddGold(-potionPrice); AddPotion(1); SaveGameData(); } }
    public void BuyDamageUpgrade() { if (playerGold >= damageUpgradePrice) { AddGold(-damageUpgradePrice); playerDamage += 5; SaveGameData(); } }

    public void TakeDamage() { AddHealth(-10); if (playerHealth <= 0) GameOver(); }
    private void AddGold(int amount) { playerGold += amount; UpdateStatsUI(); }
    private void AddHealth(int amount) { playerHealth += amount; if (playerHealth > maxHealth) playerHealth = maxHealth; UpdateStatsUI(); }
    private void AddPotion(int amount) { playerPotions += amount; if (playerPotions < 0) playerPotions = 0; UpdateStatsUI(); }

    private void GameOver()
    {
        if (combatManager != null) combatManager.StopAllCoroutines();
        ResetGame();
    }

    public void OnVictory()
    {
        AddGold(100);
        currentLevel++;
        UpdateLevelUI();
        SaveGameData();
        ReturnToMenu();
    }

    private void SaveGameData()
    {
        PlayerPrefs.SetInt("Gold", playerGold);
        PlayerPrefs.SetInt("Health", playerHealth);
        PlayerPrefs.SetInt("MaxHealth", maxHealth);
        PlayerPrefs.SetInt("Potions", playerPotions);
        PlayerPrefs.SetInt("Damage", playerDamage);
        PlayerPrefs.SetInt("Level", currentLevel);

        // Not: Hikaye bayraklarını (storyFlags) PlayerPrefs'e kaydetmek biraz karmaşıktır.
        // Şimdilik bayraklar oyun kapatılana kadar geçerli.
        // Kalıcı yapmak istersen her bayrak için PlayerPrefs.SetInt("Flag_AskeriKurtardi", 1) gibi bir yöntem gerekir.

        PlayerPrefs.Save();
    }

    private void LoadGameData()
    {
        playerGold = PlayerPrefs.GetInt("Gold", 0);
        maxHealth = PlayerPrefs.GetInt("MaxHealth", 100);
        playerHealth = PlayerPrefs.GetInt("Health", 100);
        playerPotions = PlayerPrefs.GetInt("Potions", 1);
        playerDamage = PlayerPrefs.GetInt("Damage", 10);
        currentLevel = PlayerPrefs.GetInt("Level", 1);
    }

    public void ShowMenu() { SetActivePanel(menuPanel); }
    public void ShowShop() { SetActivePanel(shopPanel); }
    private void SetActivePanel(GameObject activePanel) { if (menuPanel) menuPanel.SetActive(false); if (combatPanel) combatPanel.SetActive(false); if (cardPanel) cardPanel.SetActive(false); if (shopPanel) shopPanel.SetActive(false); if (monsterObject) monsterObject.SetActive(false); if (cardObject) cardObject.SetActive(false); if (activePanel != null) activePanel.SetActive(true); if (statsPanel) statsPanel.SetActive(true); }
    public void ReturnToMenu() { StartCoroutine(WaitAndShowMenu()); }
    private System.Collections.IEnumerator WaitAndShowMenu() { yield return new WaitForSeconds(2f); ShowMenu(); }
    private void UpdateStatsUI() { if (goldText) goldText.text = "Altin: " + playerGold; if (healthText) healthText.text = $"Can: {playerHealth}/{maxHealth}"; if (potionText) potionText.text = "İksir: " + playerPotions; }
    private void UpdateLevelUI() { if (levelText) levelText.text = "LEVEL: " + currentLevel; }
    private void ResetGame() { playerGold = 0; maxHealth = 100; playerHealth = 100; playerPotions = 1; playerDamage = 10; currentLevel = 1; storyFlags.Clear(); SaveGameData(); UpdateStatsUI(); UpdateLevelUI(); ShowMenu(); }
    [ContextMenu("Reset Save")] public void ResetSaveData() { PlayerPrefs.DeleteAll(); ResetGame(); }
}
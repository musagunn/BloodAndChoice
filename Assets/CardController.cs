using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CardController : MonoBehaviour, IDragHandler, IEndDragHandler
{
    // ... (Diğer değişkenler aynı kalsın) ...
    [Header("Bağlantılar")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text descriptionText; // <-- YENİ: Ortadaki soru yazısı
    [SerializeField] private TMP_Text leftChoiceText;
    [SerializeField] private TMP_Text rightChoiceText;

    // ... (Ayarlar, değişkenler aynı) ...
    [Header("Ayarlar")]
    [SerializeField] private float threshold = 100f;
    [SerializeField] private float rotationSpeed = 0.1f;

    private Vector3 originalPosition;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.localPosition;
    }

    void OnEnable()
    {
        if (rectTransform != null)
        {
            rectTransform.localPosition = originalPosition;
            transform.rotation = Quaternion.identity;
        }
        ResetCard();
    }

    // --- YENİ FONKSİYON: KARTI KUR ---
    public void SetupCard(CardData data)
    {
        if (data == null) return;

        // Yazıları veriden alıp ekrana bas
        if (descriptionText != null) descriptionText.text = data.description;
        if (leftChoiceText != null) leftChoiceText.text = data.leftText;
        if (rightChoiceText != null) rightChoiceText.text = data.rightText;
    }
    // --------------------------------

    // ... (OnDrag, OnEndDrag, ConfirmChoice, ResetCard AYNI KALACAK) ...

    // Sadece ConfirmChoice içindeki SetActive(false)'a dokunma, o doğru.
    // OnEndDrag vs. önceki kodun aynısı. Sadece yukarıdaki SetupCard'ı ve değişkeni ekle.

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
        float posX = transform.localPosition.x;
        transform.rotation = Quaternion.Euler(0, 0, -posX * rotationSpeed);
        UpdateTextVisibility(posX);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float posX = transform.localPosition.x;

        // --- SAĞ SEÇİM ---
        if (posX > threshold)
        {
            // ZABITA KONTROLÜ: Gücün yetiyor mu?
            if (gameManager != null && gameManager.CanAffordCardChoice(true))
            {
                Debug.Log("✅ Sağ Seçildi");
                gameManager.ApplyCardEffect(true); // Etkiyi uygula
                ConfirmChoice(); // Kartı kapat ve savaşı başlat
            }
            else
            {
                // Paran/İksirin yetmiyor! Kartı geri yolla.
                ResetToCenter();
            }
        }
        // --- SOL SEÇİM ---
        else if (posX < -threshold)
        {
            // ZABITA KONTROLÜ: Gücün yetiyor mu?
            if (gameManager != null && gameManager.CanAffordCardChoice(false))
            {
                Debug.Log("✅ Sol Seçildi");
                gameManager.ApplyCardEffect(false);
                ConfirmChoice();
            }
            else
            {
                // Paran/İksirin yetmiyor! Kartı geri yolla.
                ResetToCenter();
            }
        }
        // --- ORTADA KALDI ---
        else
        {
            ResetToCenter();
        }
    }

    // Kod tekrarı olmasın diye resetleme işini fonksiyona aldım
    private void ResetToCenter()
    {
        rectTransform.localPosition = originalPosition;
        transform.rotation = Quaternion.identity;
        ResetCard();
        Debug.Log("🔙 Kart merkeze döndü (İptal veya Yetersiz Bakiye)");
    }

    private void ConfirmChoice()
    {
        gameObject.SetActive(false);
        if (gameManager != null) gameManager.StartGame();
    }

    private void UpdateTextVisibility(float posX)
    {
        if (leftChoiceText != null) leftChoiceText.gameObject.SetActive(posX < -20);
        if (rightChoiceText != null) rightChoiceText.gameObject.SetActive(posX > 20);
    }

    private void ResetCard()
    {
        if (leftChoiceText != null) leftChoiceText.gameObject.SetActive(false);
        if (rightChoiceText != null) rightChoiceText.gameObject.SetActive(false);
    }
}
using System;
using UnityEngine;

/// <summary>
/// Mobil ve Mouse için Swipe (kaydırma) algılama sistemi
/// Sağ, Sol, Yukarı, Aşağı yön tespiti yapar
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("Swipe Ayarları")]
    [Tooltip("Swipe olarak algılanması için gereken minimum mesafe (piksel)")]
    [SerializeField] private float minSwipeDistance = 50f;

    [Tooltip("Swipe algılama süresi (saniye) - Çok yavaş kaydırmaları engellemek için")]
    [SerializeField] private float maxSwipeTime = 1f;

    // Başlangıç pozisyonu (parmağın veya mouse'un ekrana ilk değdiği nokta)
    private Vector2 startPosition;

    // Swipe başlangıç zamanı
    private float startTime;

    // Şu anda parmak/mouse basılı mı?
    private bool isSwipeActive = false;

    /// <summary>
    /// Swipe yönlerini temsil eden enum
    /// </summary>
    public enum SwipeDirection
    {
        None,   // Yön algılanmadı
        Right,  // Sağa kaydırma
        Left,   // Sola kaydırma
        Up,     // Yukarı kaydırma
        Down    // Aşağı kaydırma
    }

    // ===== EVENT SİSTEMİ =====
    // Diğer scriptler (CombatManager gibi) bu eventi dinleyerek swipe'ları yakalayabilir
    /// <summary>
    /// Swipe algılandığında tetiklenen event
    /// Parametreler: (SwipeDirection yön, float mesafe, float süre)
    /// </summary>
    public static event Action<SwipeDirection, float, float> OnSwipeDetected;

    void Update()
    {
        // Her frame'de input kontrolü yap
        DetectSwipe();
    }

    /// <summary>
    /// Swipe algılama ana fonksiyonu
    /// Hem dokunmatik hem mouse inputlarını destekler
    /// </summary>
    private void DetectSwipe()
    {
        // MOBİL: Dokunmatik ekran kontrolü
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // İlk parmağı al

            // Parmak ekrana yeni değdi mi?
            if (touch.phase == TouchPhase.Began)
            {
                StartSwipe(touch.position);
            }
            // Parmak ekrandan kalktı mı?
            else if (touch.phase == TouchPhase.Ended && isSwipeActive)
            {
                EndSwipe(touch.position);
            }
        }
        // PC: Mouse kontrolü (Test amaçlı)
        else
        {
            // Mouse sol tuşuna basıldı mı?
            if (Input.GetMouseButtonDown(0))
            {
                StartSwipe(Input.mousePosition);
            }
            // Mouse sol tuşu bırakıldı mı?
            else if (Input.GetMouseButtonUp(0) && isSwipeActive)
            {
                EndSwipe(Input.mousePosition);
            }
        }
    }

    /// <summary>
    /// Swipe başlangıcını kaydeder
    /// </summary>
    /// <param name="position">Başlangıç pozisyonu</param>
    private void StartSwipe(Vector2 position)
    {
        isSwipeActive = true;
        startPosition = position;
        startTime = Time.time; // Başlangıç zamanını kaydet
    }

    /// <summary>
    /// Swipe bitişinde yön hesaplar ve konsola yazdırır
    /// </summary>
    /// <param name="endPosition">Bitiş pozisyonu</param>
    private void EndSwipe(Vector2 endPosition)
    {
        isSwipeActive = false;

        // Swipe süresi çok mu uzun?
        float swipeDuration = Time.time - startTime;
        if (swipeDuration > maxSwipeTime)
        {
            Debug.Log("Swipe çok yavaştı, algılanmadı.");
            return;
        }

        // Başlangıç ve bitiş arasındaki mesafeyi hesapla
        Vector2 swipeVector = endPosition - startPosition;
        float swipeDistance = swipeVector.magnitude;

        // Swipe mesafesi yeterli mi?
        if (swipeDistance < minSwipeDistance)
        {
            Debug.Log("Swipe mesafesi çok kısa, algılanmadı.");
            return;
        }

        // Yönü hesapla
        SwipeDirection direction = GetSwipeDirection(swipeVector);

        // Konsola yazdır
        Debug.Log($"SWIPE ALGILANDI: {direction} | Mesafe: {swipeDistance:F0}px | Süre: {swipeDuration:F2}s");

        // EVENT'i tetikle - Diğer scriptler bu eventi dinliyor olabilir
        OnSwipeDetected?.Invoke(direction, swipeDistance, swipeDuration);
    }

    /// <summary>
    /// Swipe vektöründen yön hesaplar
    /// </summary>
    /// <param name="swipeVector">Başlangıç ve bitiş arasındaki vektör</param>
    /// <returns>Algılanan swipe yönü</returns>
    private SwipeDirection GetSwipeDirection(Vector2 swipeVector)
    {
        // Yatay mı (X ekseni) yoksa dikey mi (Y ekseni) daha baskın?
        // Mutlak değerleri karşılaştır
        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            // YATAY HAREKET (Sağ veya Sol)
            if (swipeVector.x > 0)
                return SwipeDirection.Right; // Sağa
            else
                return SwipeDirection.Left;  // Sola
        }
        else
        {
            // DİKEY HAREKET (Yukarı veya Aşağı)
            if (swipeVector.y > 0)
                return SwipeDirection.Up;    // Yukarı
            else
                return SwipeDirection.Down;  // Aşağı
        }
    }

    /// <summary>
    /// Gizmos ile swipe yönünü görselleştir (Sadece Editor'de görünür)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (isSwipeActive)
        {
            // Başlangıç noktasını kırmızı ile göster
            Gizmos.color = Color.red;
            Vector3 worldStart = Camera.main.ScreenToWorldPoint(new Vector3(startPosition.x, startPosition.y, 10));
            Gizmos.DrawSphere(worldStart, 0.2f);
        }
    }
}


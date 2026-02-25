using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // Titreþimin ne kadar süreceði ve ne kadar þiddetli olacaðý
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Rastgele bir x ve y pozisyonu üret
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Kamerayý o noktaya taþý
            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            yield return null; // Bir sonraki kareyi bekle
        }

        // Titreþim bitince kamerayý eski yerine koy
        transform.localPosition = originalPos;
    }
}
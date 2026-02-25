using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "BloodChoice/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Kart Bilgileri")]
    [TextArea] public string description;

    [Header("Sol Seçim")]
    public string leftText;
    public int leftHealthEffect;
    public int leftGoldEffect;
    public int leftPotionEffect; // YENÝ: Ýksir Etkisi (+1 veya -1)
    public string storyFlagOnLeft;

    [Header("Sað Seçim")]
    public string rightText;
    public int rightHealthEffect;
    public int rightGoldEffect;
    public int rightPotionEffect; // YENÝ: Ýksir Etkisi
    public string storyFlagOnRight;
}
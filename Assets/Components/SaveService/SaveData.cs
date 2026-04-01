using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public List<int> scores = new List<int>();
    public int BestScore;

    public int Lives = PlayerInventoryController.MaxLives;

    public int RedBottleCount;
    public int BlueBottleCount;
    public int GreenBottleCount;
    public int CandleCount;

    public bool RedGreenPotionActive;
    public bool RedBluePotionActive;
    public bool RedCandlePotionActive;
}

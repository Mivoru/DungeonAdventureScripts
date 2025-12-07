using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    public TMP_Text buttonText;
    public Button btn;
    public GameObject lockIcon; // Volitelné: Ikonka zámku pro zamèené levely

    private DungeonLevelData myData;

    // Tuto metodu zavolá Menu, když tlaèítko vytváøí
    public void Setup(DungeonLevelData data, bool isUnlocked)
    {
        myData = data;
        buttonText.text = data.levelName; // Napø. "Floor 1"
        btn.interactable = isUnlocked;

        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }

        // Vyèistíme staré posluchaèe a pøidáme nový
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (GameManager.instance != null)
        {
            Debug.Log($"Vstupuji do: {myData.levelName}");
            GameManager.instance.EnterDungeon(myData);

            // Zavøeme menu (najdeme ho v rodièích)
            GetComponentInParent<DungeonMenuUI>()?.CloseMenu();
        }
    }
}
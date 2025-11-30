using UnityEngine;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Vıška nad hlavou
    public float ppu = 16f; // Pixels Per Unit (pro ostrost)

    // Tady si nastav, jak velkı má bar bıt ve svìtì (napø. 0.01, 0.01, 1)
    // Skript se bude snait tuto velikost udret, a je Slime jakkoliv velkı.
    public Vector3 fixedWorldScale = new Vector3(0.01f, 0.01f, 1f);

    private Transform target;

    void Start()
    {
        target = transform.parent;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // 1. POZICE (Pixel Perfect)
            transform.rotation = Quaternion.identity; // Vdy vodorovnì
            Vector3 targetPos = target.position + offset;
            float pixelX = Mathf.Round(targetPos.x * ppu) / ppu;
            float pixelY = Mathf.Round(targetPos.y * ppu) / ppu;
            transform.position = new Vector3(pixelX, pixelY, targetPos.z);

            // 2. VELIKOST (Kompensace rodièe)
            // Vypoèítáme, jak musíme Canvas zmenšit/zvìtšit, aby vyrušil zmìnu velikosti rodièe.
            // Vzorec: CílováVelikost / VelikostRodièe

            float newX = fixedWorldScale.x / target.localScale.x;
            float newY = fixedWorldScale.y / target.localScale.y;

            // Pokud je rodiè otoèenı (záporné X), toto dìlení nám automaticky dá záporné èíslo,
            // co Canvas otoèí zpátky "naruby", take text bude èitelnı! (Minus a minus dává plus).

            transform.localScale = new Vector3(newX, newY, 1f);
        }
    }
}
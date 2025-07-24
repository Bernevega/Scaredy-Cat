using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanceKey : MonoBehaviour
{
    public static KeyCode[] randomKeyList { get; private set; }

    [SerializeField] TMP_Text keyText;
    [SerializeField] Image currentArea;
    [SerializeField] Image maxArea;
    [SerializeField] Image minArea;

    public float shrinkRate = 50f;
    public KeyCode reqKey = KeyCode.None;

    static DanceKey()
    {
        randomKeyList = new KeyCode[4];
        randomKeyList[0] = KeyCode.W;
        randomKeyList[1] = KeyCode.A;
        randomKeyList[2] = KeyCode.S;
        randomKeyList[3] = KeyCode.D;
    }

    public void OnUpdate(float dt)
    {
        Vector3 currentScale = currentArea.transform.localScale;
        float decreaseAmt = dt * shrinkRate;

        currentScale.x -= decreaseAmt;
        currentScale.y -= decreaseAmt;

        if (currentScale.x < 0) currentScale.x = 0;
        if (currentScale.y < 0) currentScale.y = 0;

        currentArea.transform.localScale = currentScale;
    }

    public void OnReset()
    {
        currentArea.transform.localScale = new Vector3(400, 400, 1);

        reqKey = randomKeyList[Random.Range(0, randomKeyList.Length)];

        switch (reqKey)
        {
            case KeyCode.W:
                keyText.text = "W";
                break;
            case KeyCode.A:
                keyText.text = "A";
                break;
            case KeyCode.S:
                keyText.text = "S";
                break;
            case KeyCode.D:
                keyText.text = "D";
                break;
        }
    }

    public bool InSuccessZone()
    {
        bool inZone = false;

        Vector3 currentScale = currentArea.transform.localScale;
        Vector3 maxScale = maxArea.transform.localScale;

        if (currentScale.x <= maxScale.x &&
            currentScale.y <= maxScale.y)
        {
            inZone = true;
        }

        return inZone;
    }

    public bool InFailZone()
    {
        bool inZone = false;

        Vector3 currentScale = currentArea.transform.localScale;
        Vector3 minScale = minArea.transform.localScale;

        if (currentScale.x <= minScale.x &&
            currentScale.y <= minScale.y)
        {
            inZone = true;
        }

        return inZone;
    }

    public float GetScale()
    {
        return currentArea.transform.localScale.x;
    }
}

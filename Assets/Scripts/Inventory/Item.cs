using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemScriptable itemInfo;
    [SerializeField] ItemBehaviour behaviour;
    public int stack { get; private set; } = 1;


    public void Setup(GameObject owner)
    {
        if (behaviour)
        {
            behaviour.Setup(owner);
        }
    }

    public void AddStack(int amt)
    {
        if (amt == 0 || (stack == 0 && amt < 0)) return;
        stack += amt;

        if (behaviour)
        {
            if (stack > 0)
                behaviour.OnStackAdd(amt);
            else
                behaviour.OnStackRemove(-amt);
        }
    }

    public void OnFixedUpdate()
    {
        if (behaviour == null) return;

        behaviour.OnFixedUpdate();
    }

    public ItemBehaviour GetBehaviour() { return behaviour; }
}


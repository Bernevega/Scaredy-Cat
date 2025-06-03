using UnityEngine;

public class ItemBehaviour : MonoBehaviour
{
    protected GameObject owner;
    public virtual void Setup(GameObject owner) { this.owner = owner; }
    public virtual void OnStackAdd(int amt) { }
    public virtual void OnStackRemove(int amt) { }
    public virtual void OnFixedUpdate() { }
}

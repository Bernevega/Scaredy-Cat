using System;
using UnityEngine;

public class TriggerEvent : MonoBehaviour
{
    // <Triggered collider, Other collider, Trigger enter>
    public Action<Collider, Collider, bool> eventOnTrigger;
    private Collider coll;

    private void Awake()
    {
        coll = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        eventOnTrigger?.Invoke(coll, other, true);
    }
    private void OnTriggerExit(Collider other)
    {
        eventOnTrigger?.Invoke(coll, other, false);
    }

}

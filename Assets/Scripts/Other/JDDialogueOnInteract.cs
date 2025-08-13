using UnityEngine;

public class JDDialogueOnInteract : MonoBehaviour
{
    [SerializeField] Interactable interactable;

    [Header("Item requirements (presence check only)")]
    [Tooltip("JD will say JDGive if BOTH are present. We do NOT add/remove anything.")]
    [SerializeField] ItemScriptable funnyHatItem;
    [SerializeField] ItemScriptable bowtieItem;

    [Header("Dialogue tree names (must match your JSON)")]
    [SerializeField] string treeStart   = "JDStart";
    [SerializeField] string treeOffer   = "JDCheese"; // he asks for hat+tie
    [SerializeField] string treeWaiting = "JDWait";   // while you don't have both
    [SerializeField] string treeGive    = "JDGive";   // when you have both
    [SerializeField] string treeThanks  = "JDThank";  // after trade complete

    // simple state flags (per save/scene)
    private bool didIntro      = false; // after JDStart
    private bool askedForTrade = false; // after JDCheese
    private bool tradeComplete = false; // after JDGive (we don't change inventory; just mark done)

    // tiny buffer for inventory lookups
    private readonly Item[] _tmp = new Item[1];

    private void Awake()
    {
        if (interactable != null)
            interactable.eventOnInteract += OnInteract;
        else
            Debug.LogWarning($"[JDDialogueOnInteract:{name}] Missing Interactable reference.");
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;
    }

    private void OnInteract(Interactor interactor, Interactable sender, InteractActionType type)
    {
        if (type != InteractActionType.Interact) return;

        var dm = SimpleDialogManager.Instance;
        if (dm == null) { Debug.LogWarning("[JDDialogueOnInteract] No SimpleDialogManager found."); return; }

        // Block if a dialogue is already open
        if (dm.dialogPanel != null && dm.dialogPanel.activeSelf)
            return;

        // 1) First interaction → JDStart
        if (!didIntro)
        {
            dm.StartDialogue(treeStart);
            didIntro = true;
            return;
        }

        // 2) Second interaction → JDCheese (request)
        if (!askedForTrade)
        {
            dm.StartDialogue(treeOffer);
            askedForTrade = true;
            return;
        }

        // 3) After offer: branch based on item presence (no modifications)
        if (!tradeComplete)
        {
            bool hasBoth = PlayerHasRequiredItems(interactor);
            if (hasBoth)
            {
                // We only change DIALOG, not items
                dm.StartDialogue(treeGive);
                tradeComplete = true; // mark done so we switch to Thanks next time
            }
            else
            {
                dm.StartDialogue(treeWaiting);
            }
            return;
        }

        // 4) After trade completed → loop a short thank-you line
        dm.StartDialogue(treeThanks);
    }

    private bool PlayerHasRequiredItems(Interactor interactor)
    {
        if (interactor == null) return false;
        var owner = interactor.GetOwner();
        if (owner == null) return false;

        var inv = owner.GetComponent<InventoryScript>();
        if (inv == null) return false;

        bool hasHat = funnyHatItem != null && inv.GetItem_WithScriptable(_tmp, funnyHatItem) > 0;
        bool hasTie = bowtieItem   != null && inv.GetItem_WithScriptable(_tmp, bowtieItem)   > 0;

        // Require BOTH (matches your JDWait text)
        return hasHat && hasTie;
    }
}

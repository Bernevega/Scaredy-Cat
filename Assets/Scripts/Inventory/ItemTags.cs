using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ItemTags
{
    None = 0,
    Basic = 1 << 0,
    StatusEffect = 1 << 1,
    Hidden = 1 << 2,
    NonStackable = 1 << 3,
}

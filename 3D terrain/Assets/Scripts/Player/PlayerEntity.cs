using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity : Entity//move mana to this script
{
    void Awake()
    {
        Init();
    }

    void Update()
    {
        Debug.Log(health);
    }
}

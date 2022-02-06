using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : Entity
{
    private float collectionStorage;
    private int collectionStorageMax = 1000;
    private float collectionAmount;
    public TextMesh buildingText;
    private Transform player;

    public void Init(BuildingManager builder) {
        base.Init();
        collectionAmount = WorldManaManager.CheckMana(transform.position);
        player = builder.transform;
    }

    void Update() {
        if(player == null) return;
        if(collectionStorage >= collectionStorageMax) return;
        collectionStorage += collectionAmount * Time.deltaTime;
        buildingText.text = "Mana: " + (int)collectionStorage;
        buildingText.transform.LookAt(RotateText());
        if(collectionStorage >= collectionStorageMax) collectionStorage = collectionStorageMax;
    }

    private Vector3 RotateText() {
        Vector3 target = 2*transform.position-player.position;
        target.y = transform.position.y;
        return target;
    }

    public int CollectStored() {
        int temp = (int)collectionStorage;
        collectionStorage -= temp;
        return temp;
    }

    
}

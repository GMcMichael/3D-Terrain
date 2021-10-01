using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public int interactionDistance = 5;
    private LayerMask interactionMask;
    private Transform playerCamera;
    private Mana playerMana;

    void Start() {
        interactionMask = LayerMask.GetMask("Building");
        playerCamera = transform.Find("Main Camera");
        playerMana = GetComponent<Mana>();
    }

    void Update() {
        Debug.DrawLine(playerCamera.position, playerCamera.position+playerCamera.forward*interactionDistance, Color.green);
        if(Input.GetKeyDown(KeyCode.E)) {
            if(Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hitInfo, interactionDistance, interactionMask)) {
                if(hitInfo.transform.gameObject.layer == LayerMask.NameToLayer("Building"))
                    playerMana.AddMana(hitInfo.transform.GetComponent<Building>().CollectStored());
            }
        }
    }
}

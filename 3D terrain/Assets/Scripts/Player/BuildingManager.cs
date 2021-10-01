using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public int buildDistance = 5;
    public int rotationSpeed = 5;
    public GameObject[] buildingTypes;
    private int currentBuilding;
    private Transform displayedBuilding = null;
    private bool displayed;
    private bool building;
    private bool changeDisplay = true;
    private Transform playerCamera;
    private LayerMask buildMask;
    private float yRot;
    private List<GameObject> BuiltBuildings = new List<GameObject>();
    private Transform buildingParent;
    public UnityEngine.UI.Text manaText;

    void Start() {
        playerCamera = transform.Find("Main Camera");
        buildMask = LayerMask.GetMask("World");
        buildingParent = GameObject.Find("/Buildings").transform;
        //manaText = GameObject.Find("ManaText").GetComponent<UnityEngine.UI.Text>();//dosent work on inactive gameobjects
    }

    void Update() {
        if(Input.GetKeyDown(KeyCode.B)) {
            building = !building;
             if(!building) {
                DisablePreview();
            }
        }
        if(building) {
            if(buildingTypes.Length < 1) return;
            if(Input.mouseScrollDelta.y != 0) {
                if(Input.GetKey(KeyCode.R)) yRot += (Input.mouseScrollDelta.y > 0 ? 1 : -1)*rotationSpeed*Time.deltaTime;
                else {
                    currentBuilding += Input.mouseScrollDelta.y > 0 ? 1 : -1;
                    changeDisplay = true;
                }
                if(currentBuilding < 0) currentBuilding = buildingTypes.Length-1;
                else if(currentBuilding > buildingTypes.Length-1) currentBuilding = 0;
            }
            PreviewBuilding();
            if(Input.GetMouseButtonDown(0)) {
                PlaceBuilding();
            }
        }
    }

    private void DisablePreview() {
        if(displayed) {
            displayedBuilding.gameObject.SetActive(false);
            manaText.gameObject.SetActive(false);
            displayed = false;
        }
    }

    private void PreviewBuilding() {
        if(changeDisplay) {
            if(displayed) Destroy(displayedBuilding.gameObject);
            displayedBuilding = Instantiate(buildingTypes[currentBuilding], transform.position+(transform.forward*5), Quaternion.identity).transform;
            changeDisplay = false;
        }
        if(!displayed){
            displayedBuilding.gameObject.SetActive(true);
            manaText.gameObject.SetActive(true);
            displayed = true;
        }
        displayedBuilding.rotation = Quaternion.Euler(0, yRot, 0);
        //Debug.DrawLine(playerCamera.position, playerCamera.position+(playerCamera.forward*buildDistance), Color.red);
        if(Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hitInfo, buildDistance, buildMask)) {
            displayedBuilding.position = hitInfo.point+new Vector3(0, displayedBuilding.localScale.y/2, 0);
            manaText.text = "Mana: " + (int)WorldManaManager.CheckMana(hitInfo.point);
        } else {
            DisablePreview();
        }
    }

    private void PlaceBuilding() {
        Instantiate(buildingTypes[currentBuilding], displayedBuilding.position, displayedBuilding.rotation, buildingParent).GetComponent<Building>().Init(this);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mana : MonoBehaviour
{
    private int mana;//TODO: Come up with unique name
    private UnityEngine.UI.Text manaText;

    void Awake() {
        manaText = GameObject.Find("Player Mana Count").GetComponent<UnityEngine.UI.Text>();
    }

    public void AddMana(int _mana) {
        mana += _mana;
        manaText.text = ""+mana;
    }

    public bool BuyItem(int cost) {
        if(mana >= cost) {
            AddMana(-cost);
            return true;
        }
        return false;
    }

    public void RefundItem(int cost) {
        AddMana(cost);
    }

    public int ManaAmount() {
        return mana;
    }
}

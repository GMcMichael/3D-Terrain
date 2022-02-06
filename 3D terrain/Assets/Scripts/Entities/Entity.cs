using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    protected int health;
    public int maxHealth = 100;

    public virtual void Init() {
        health = maxHealth;
    }
    public void TakeDamage(int amount, SkillSystem.Elements element) {
        health -= amount;
        if(health <= 0) DestroyEntity();
        //do things based on element: eg set on fire if fire or get poisoned
    }

    public void DestroyEntity() {
        Destroy(transform.gameObject);
    }
}

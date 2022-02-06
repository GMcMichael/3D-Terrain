using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillSystem : MonoBehaviour
{
    public enum Elements {//Maybe create an array if these interact to boost or lower damage/effectivness
        Mana,
        ManaCongregation,//Poison
        Fire,
        HellFire,
        Water,
        LifeBlood,
        Earth,
        InnerCore,
        Air,
        Tempest
    }

    public enum Effects {
        Sphere,
        Capsule,
        Box,
        Cone,
        Trail
    }

    public enum Skills {//probably remove this when I think this through
        CelestialSpace//creates a black hole
    }

    public void UseSkill(Skill skill, Elements element) {
        Vector3 skillPosition = transform.position;//get skill position, might need to find position after landing like with sticky light or it might be players position. maybe find position in skill itself
        skill.ActivateSkill(skillPosition, element);
    }
}

public class Skill : MonoBehaviour {//make an array of players skills in player controller, will hold both skills and advanced skills
    protected SkillSystem.Skills skillName;
    protected SkillSystem.Effects effect;
    protected SkillSystem.Elements lastElement;
    protected Vector3 position;
    protected int damage;
    protected Vector3 dims;
    protected int cooldown = 5;
    protected int lifeSpan;
    protected float aoeTimeStep = 0.5f;
    protected bool canUse = true;
    protected static LayerMask enemyLayerMask = LayerMask.GetMask("Enemy");

    public virtual bool ActivateSkill(Vector3 _position, SkillSystem.Elements element) {
        if(!canUse) return false;
        lastElement = element;
        position = _position;
        //activate effect and apply element
        StartCoroutine("StartCooldown");
        StartCoroutine("StartAOE");
        return true;
    }

    protected IEnumerator StartCooldown() {
        canUse = false;
        yield return new WaitForSeconds(cooldown);
        canUse = true;
    }

    protected IEnumerator StartAOE() {
        float timer = 0;
        while(timer < lifeSpan) {
            switch(effect) {
                case SkillSystem.Effects.Sphere:
                SphereArea();
                break;
                case SkillSystem.Effects.Capsule:
                CapsuleArea();
                break;
                case SkillSystem.Effects.Box:
                BoxArea();
                break;
                case SkillSystem.Effects.Cone:
                break;
                case SkillSystem.Effects.Trail:
                break;
            }
            yield return new WaitForSeconds(aoeTimeStep);
        }
        //remove any shaders and destroy any objects
    }

    protected void SphereArea() {
        DamageColliders(Physics.OverlapSphere(position, dims[0], enemyLayerMask));
    }

    protected void CapsuleArea() {
        Vector3 pos1 = new Vector3(position.x, position.y+dims[1], position.z);
        DamageColliders(Physics.OverlapCapsule(position, pos1, dims[0], enemyLayerMask));
    }

    protected void BoxArea() {
        DamageColliders(Physics.OverlapBox(position, dims, Quaternion.identity, enemyLayerMask));
    }

    protected void DamageColliders(Collider[] colliders) {
        foreach (Collider enemy in colliders)
        {
            enemy.GetComponent<Enemy>().TakeDamage(damage, lastElement);
        }
    }
}

public class AdvancedSkill : Skill {
    //create extra skill variables here
    public override bool ActivateSkill(Vector3 _position, SkillSystem.Elements element) {//just use base.ActivateSkill if I can
        if(!canUse) return false;
        lastElement = element;
        position = _position;
        //activate effect and apply element
        StartCoroutine("StartCooldown");
        StartCoroutine("StartAOE");
        return true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : Entity
{
    private Transform Player;
    public static LayerMask playerMask;
    private NavMeshAgent agent;
    private LineRenderer lineRenderer;
    public Transform target;
    private Vector3 destination;
    private int orbitDirection;
    private float orbitSpeed;
    private float orbitDist = 20;
    private int destinationBuffer = 1;
    private bool orbiting;
    private int changeDestTimer;
    private int changeDestCooldown = 100;
    private static int enemiesOrbiting;
    private static int maxEnemiesOrbiting = 5;
    private int attackCooldown = 300;
    private bool canAttack = true;
    public float attackDist = 20;
    private int attackDamage = 1;
    private SkillSystem.Elements attackElement = SkillSystem.Elements.Mana;
    
    void Awake()//make different enemies subclasses of this enemy class
    {
        Init();
        Player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        orbitDirection = Random.value < 0.5 ? -1 : 1;//set orbitDirection to -1 or 1
        orbitSpeed = agent.speed;
        lineRenderer = GetComponentInChildren<LineRenderer>();
        SetTarget(Player);
    }

    void Update()
    {
        //Debug.DrawLine(transform.position, transform.position+((target.position - transform.position).normalized*attackDist), Color.red);
        if(canAttack && Vector3.Distance(transform.position, target.position) <= attackDist)
            Attack();
        if(enemiesOrbiting >= maxEnemiesOrbiting && !orbiting) {//move randomly around player outside orbit distance
            if(changeDestTimer <= 0) {
                changeDestTimer = changeDestCooldown;
                //get random distance >= orbitDist and <= attackDist
                float distance = Random.Range(orbitDist, attackDist);
                //get random angle
                float angle = Random.Range(0, 360);
                //get random direction from target
                Vector3 dir = Quaternion.Euler(0, angle, 0) * target.transform.forward;
                //set destination
                destination = target.position + (dir*distance);
            } else changeDestTimer--;
        } else {//orbit player
            //get direction from player to enemy
            Vector3 dir = (transform.position - target.position).normalized;
            //check if enemy is nearing orbit distance
            float distToOrbit = Vector3.Distance(transform.position, target.position);
            if(distToOrbit < orbitDist+destinationBuffer) {
                //change the dir based on orbitDirection
                dir = Quaternion.Euler(0, orbitDirection*orbitSpeed, 0) * dir;
                ChangeOrbit(true);
            } else if(distToOrbit > orbitDist+destinationBuffer*2) ChangeOrbit(false);
            //Take player position and add orbitDist in the direction
            destination = target.position + (dir*orbitDist);
        }
        //set destination
        agent.SetDestination(destination);
    }

    private void Attack() {
        if(!Physics.Raycast(transform.position, (target.position - transform.position).normalized, out RaycastHit hit, attackDist) || hit.transform != target)//can only hit what its aming at, cant accidently hit somthing else
            return;
        canAttack = false;
        hit.transform.GetComponent<Entity>().TakeDamage(attackDamage, attackElement);
        lineRenderer.SetPositions(new Vector3[] {transform.position, target.position});
        lineRenderer.startWidth = 1;
        lineRenderer.endWidth = 0.5f;
        StartCoroutine("AttackCooldown");
    }
    private IEnumerator AttackCooldown() {
        int fadeSpeed = 20;
        float startReduction = lineRenderer.startWidth/fadeSpeed;
        float endReduction = lineRenderer.endWidth/fadeSpeed;
        int i;
        for (i = 0; i < attackCooldown; i++)
        {
            if(lineRenderer.startWidth > 0)
                lineRenderer.startWidth -= startReduction;
            else lineRenderer.startWidth = 0;
            if(lineRenderer.endWidth > 0)
                lineRenderer.endWidth -= endReduction;
            else lineRenderer.endWidth = 0;
            yield return new WaitForSecondsRealtime(0.01f);
        }
        canAttack = true;
    }

    public void SetTarget(Transform _target) {
        target = _target;
    }

    private void ChangeOrbitDirection() {
        orbitDirection *= -1;
    }

    private void ChangeOrbit(bool x) {
        if(orbiting == x) return;
        orbiting = !orbiting;
        if(x) enemiesOrbiting++;
        else enemiesOrbiting--;
    }
}

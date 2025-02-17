﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private Animator _animator;
    private AudioSource _audioSource;

    public float _hp;
    public GameObject swordOn;
    public GameObject swordOff;

    public bool _agressive = false;
    private bool _agrPast = false;
    private bool _attack = false;
    public bool _death = false;
    public bool control;
    private bool _coroutStart;

    private NavMeshAgent _agent;
    public GameObject _player;
    private PlayerCharacteristics _playerCharacteristics;
    private GameObject[] _buildsForPatrol;
    private ImportantBuildings _importantBuildings;
    private BuildEn _nextBuild;

    private bool _canAttack = false;

    private SpawnEnemyes _enemies;
    private List<EnemyLimbs> _limbs;
    private List<Rigidbody> ragdolls;

    private AudioClip[] _swordSound;

    public bool isSoldier;
    public bool canWalkInVillage;

    private bool isHasScriptEnemy;
    public bool canHit_hp;

    private void Start()
    {
        _enemies = GameObject.FindGameObjectWithTag("Enemies").GetComponent<SpawnEnemyes>();
        if (!isSoldier)
        {
            ++_enemies.allEnemies["AllySoldier"];
        }

        _animator = GetComponent<Animator>();

        _player = GameObject.FindGameObjectWithTag("Player");
        _playerCharacteristics = _player.GetComponent<PlayerCharacteristics>();
        _agent = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>();
        _swordSound = _enemies.swordSound;
        _hp = Random.Range(200, 400);

        _importantBuildings = GameObject.FindGameObjectWithTag("BuildingsImportant").GetComponent<ImportantBuildings>();
        _buildsForPatrol = new GameObject[] { _importantBuildings.EntranceToTavern, _importantBuildings.Garden, _importantBuildings.RightGate, _importantBuildings.RightUpGate, _importantBuildings.LeftUpGate };
        _nextBuild = (BuildEn)Random.Range(0, _buildsForPatrol.Length);

        SwordOff();
        _limbs = new List<EnemyLimbs>();
        _limbs.AddRange(GetComponentsInChildren<EnemyLimbs>());
        for (int i = 0; i < _limbs.Count; i++)
        {
            _limbs[i].parentEnemy = gameObject;
            _limbs[i].type = EnemyLimbs.TypeEnemy.enemy;
        }
        control = false;
        canWalkInVillage = true;

        isHasScriptEnemy = GetComponent<Enemy>();
        canHit_hp = true;

        //ragdolls.AddRange(GetComponentsInChildren<Rigidbody>());
        //foreach (Rigidbody rigidbody in ragdolls)
        //{
        //    rigidbody.isKinematic = true;
        //}
    }

    private void Update()
    {
        if (_death) { return; }
        if (_hp <= 0f)
        {
            for (int i = 0; i < _limbs.Count; i++)
            {
                _limbs[i].enabled = false;
            }

            _death = true;
            _attack = false;
            _agent.enabled = false;
            _animator.SetInteger("Death", Random.Range(0, 2));
            Invoke("Death", 4.0f);
            Invoke("Delete", 300.0f);
            return;
        }





        if (_agent.velocity.magnitude > 0f)
        {
            if (_agressive)
            {
                _agent.speed = Random.Range(3.8f, 4.2f);
                _animator.SetBool("IdleToWalk", false);
                _animator.SetBool("IdleToRun", true);
            }
            else
            {
                _agent.speed = Random.Range(1.6f, 1.8f);
                _animator.SetBool("IdleToRun", false);
                _animator.SetBool("IdleToWalk", true);
            }
        }
        else
        {
            _animator.SetBool("IdleToRun", false);
            _animator.SetBool("IdleToWalk", false);
        }

        if (control || isSoldier)
        {
            //return;
        }

        if (_playerCharacteristics.isBattle && Vector3.Distance(_player.transform.position, transform.position) <= 20f)
        {
            _agressive = true;
            Add(gameObject);
        }
        else if (Vector3.Distance(_player.transform.position, transform.position) > 20f || _playerCharacteristics._dead)
        {
            if (canWalkInVillage)
            {
                _agressive = false;
                _attack = false;
                _canAttack = false;
                _playerCharacteristics.allEnemies.Remove(gameObject);
                if (_agrPast != _agressive)
                {
                    StartCoroutine(CycleAfterBattle());
                }
            }
        }


        if (isHasScriptEnemy)
        {
            canHit_hp = _player.GetComponent<Enemy>()._hp > 0f;
        }
        if (_agressive && canHit_hp) { Attack(); }
        else
        {
            if (canWalkInVillage)
            {
                if (Vector3.Distance(_buildsForPatrol[(int)_nextBuild].transform.position, transform.position) < 5f)
                {
                    _nextBuild = (BuildEn)Random.Range(0, _buildsForPatrol.Length);
                }
                else
                {
                    _agent.SetDestination(_buildsForPatrol[(int)_nextBuild].transform.position);
                }
            }
            if (!canWalkInVillage)
            {
                //_agent.SetDestination(_buildsForPatrol[(int)_nextBuild].transform.position);
            }
        }
        _agrPast = _agressive;
    }

    public void Attack()
    {
        if (_agrPast != _agressive)
        {
            StartCoroutine(CycleBattle());
            return;
        }

        if (!_canAttack) { return; }
        if (Vector3.Distance(_player.transform.position, transform.position) >= 1.5f)
        {
            _attack = false;
            _agent.SetDestination(_player.transform.position);
        }
        else
        {
            _attack = true;
            if (!_coroutStart)
            {
                StartCoroutine(Battle());
            }
        }
    }

    IEnumerator Battle()
    {
        _agent.isStopped = true;
        _coroutStart = true;
        StartCoroutine(RotateToPlayer());
        while (_attack && canHit_hp)
        {
            _animator.SetTrigger("Attack" + Random.Range(0, 4));
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            _audioSource.PlayOneShot(_swordSound[Random.Range(0, _swordSound.Length)]);
            yield return new WaitForSeconds(Random.Range(0.9f, 1.6f));
        }
        _agent.isStopped = false;
        _coroutStart = false;
    }
    IEnumerator CycleBattle()
    {
        _animator.SetTrigger("SwordOn");
        _agent.isStopped = true;
        yield return new WaitForSeconds(1.0f);
        _canAttack = true;
        _agent.isStopped = false;
    }
    IEnumerator CycleAfterBattle()
    {
        _animator.SetTrigger("SwordOff");
        _agent.isStopped = true;
        yield return new WaitForSeconds(1.0f);
        _agent.isStopped = false;
    }
    IEnumerator RotateToPlayer()
    {
        while (_attack && _agressive)
        {
            Vector3 direction = (_player.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 3f);
            yield return null;
        }
    }
    private void Add(GameObject enemy)
    {
        if (!_playerCharacteristics.allEnemies.Contains(enemy))
        {
            _playerCharacteristics.allEnemies.Add(enemy);
        }
    }
    private void Death()
    {
        _animator.enabled = false;
        _playerCharacteristics.allEnemies.Remove(gameObject);

        //gameObject.GetComponent<CapsuleCollider>().enabled = false;
        //foreach (Rigidbody rigidbody in ragdolls)
        //{
        //    rigidbody.isKinematic = false;
        //}
    }
    private void Delete()
    {
        Destroy(gameObject);
    }

    private void SwordOn()
    {
        swordOff.SetActive(false);
        swordOn.SetActive(true);
    }
    private void SwordOff()
    {
        swordOn.SetActive(false);
        swordOff.SetActive(true);
    }
    // звук шагов и ударов


    public enum BuildEn
    {
        EntranceToTavern,
        Garden,
        RightGate,
        RightUpGate,
        LeftUpGate
    }
}
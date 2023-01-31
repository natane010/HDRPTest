using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace TK.Entities.Test
{
    public class TestEntitiesManager : MonoBehaviour
    {
        public static TestEntitiesManager _Instance;

        [SerializeField] private int _vfxCount = 512;

        [SerializeField] float _remaningOverLifetime = 10f;
        
        [SerializeField] private GameObject _vfxObject;
        private Entity _vfxEntitiesObject;
        private EntityManager _entityManager;
        private void Awake()
        {
            if (_Instance == null)
            {
                _Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var settings = GameObjectConversionSettings.FromWorld
                (World.DefaultGameObjectInjectionWorld, null);
            _vfxEntitiesObject = GameObjectConversionUtility.ConvertGameObjectHierarchy
                (_vfxObject, settings);
        }

        public void SpawnBall()
        {
            _entityManager.Instantiate(_vfxEntitiesObject);
        }
    }
}

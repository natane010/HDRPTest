using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace Test
{
    public class TestShootBullet : MonoBehaviour
    {
        [SerializeField] GameObject _vfxObject = null;
        [SerializeField] const float _rayDistrance = 1000f;
        [SerializeField] float _remaningoverlifeTime = 10f;
        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, _rayDistrance, LayerMask.GetMask("Ground")))
                {
                    if (_vfxObject == null)
                    {
                        return;
                    }
                    //_vfxObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    Destroy(Instantiate<GameObject>(_vfxObject, hit.point,
                        Quaternion.LookRotation(transform.position - hit.point), transform), _remaningoverlifeTime);
                }
            }
        }
    }
}    

using System.Collections.Generic;
using UnityEngine;

namespace TK.PhysicsBone
{
    public abstract class PhysicsBoneColliderBase : MonoBehaviour
    {
        public static HashSet<PhysicsBoneColliderBase> EnabledColliders = new HashSet<PhysicsBoneColliderBase>();

        protected void OnEnable()
        {
            EnabledColliders.Add(this);
        }
        protected void OnDisable()
        {
            EnabledColliders.Remove(this);
        }

        public abstract void Collide(ref Vector3 position, float spacing);
    }
}

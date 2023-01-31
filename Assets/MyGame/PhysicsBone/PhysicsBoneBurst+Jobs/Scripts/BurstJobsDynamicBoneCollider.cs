using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

namespace TK.BurstJob.DynamicBone
{
    [RequireComponent(typeof(Collider))]
    [BurstCompile]
    public sealed class BurstJobsDynamicBoneCollider : BurstJobsDynamicBoneColliderBase
    {
        [SerializeField]
        private Collider m_ReferenceCollider;
        public Collider referenceCollider
        {
            get
            {
                if (m_ReferenceCollider == null)
                    m_ReferenceCollider = GetComponent<Collider>();
                return m_ReferenceCollider;
            }
        }

        [SerializeField]
        private float m_Margin;
        public float margin { get { return m_Margin; } set { m_Margin = value; } }

        [SerializeField]
        private bool m_InsideMode;
        public bool insideMode { get { return m_InsideMode; } set { m_InsideMode = value; } }


        public override void Collide(ref Vector3 position, float spacing)
        {
            if (referenceCollider is SphereCollider)
            {
                SphereCollider collider = referenceCollider as SphereCollider;
                if (insideMode) BurstJobsDynamicBoneFunctionUtility.PointInsideSphere(ref position, collider, spacing + margin);
                else BurstJobsDynamicBoneFunctionUtility.PointOutsideSphere(ref position, collider, spacing + margin);
            }
            else if (referenceCollider is CapsuleCollider)
            {
                CapsuleCollider collider = referenceCollider as CapsuleCollider;
                if (insideMode) BurstJobsDynamicBoneFunctionUtility.PointInsideCapsule(ref position, collider, spacing + margin);
                else BurstJobsDynamicBoneFunctionUtility.PointOutsideCapsule(ref position, collider, spacing + margin);
            }
            else if (referenceCollider is BoxCollider)
            {
                BoxCollider collider = referenceCollider as BoxCollider;
                if (insideMode) BurstJobsDynamicBoneFunctionUtility.PointInsideBox(ref position, collider, spacing + margin);
                else BurstJobsDynamicBoneFunctionUtility.PointOutsideBox(ref position, collider, spacing + margin);
            }
            else if (referenceCollider is MeshCollider)
            {
                if (!CheckConvex(referenceCollider as MeshCollider))
                {
                    Debug.LogError("メッシュコライダー非対応", this);
                    enabled = false;
                    return;
                }
                if (insideMode)
                {
                    Debug.LogError("メッシュコライダー非対応", this);
                    insideMode = false;
                    return;
                }
                BurstJobsDynamicBoneFunctionUtility.PointOutsideCollider(ref position, referenceCollider, spacing + margin);
            }
        }

        

        private static bool CheckConvex(MeshCollider meshCollider)
        {
            return meshCollider.sharedMesh != null && meshCollider.convex;
        }

        private void Reset()
        {
            m_ReferenceCollider = GetComponent<Collider>();
        }
    }
    [BurstCompile]
    public abstract class BurstJobsDynamicBoneColliderBase : MonoBehaviour
    {
        public static List<BurstJobsDynamicBoneColliderBase> EnabledColliders = new List<BurstJobsDynamicBoneColliderBase>();

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

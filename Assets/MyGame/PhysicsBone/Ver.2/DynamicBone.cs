using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace TK.DynamicBone.job
{
    public struct HeadInfo
    {
        public int Index;
        public float3 ObjectMove;
        public float3 ObjectPrevPosition;
        public float3 Gravity;
        public float ObjectScale;
        public float Weight;
        public float3 Force;
        public float3 FinalForce;
        public int ParticleCount;
        public int DataOffsetInGlobalArray;

        public float3 RootParentBoneWorldPosition;
        public quaternion RootParentBoneWorldRotation;
    }


    public struct ParticleInfo
    {
        public int Index;
        public int ParentIndex;
        public float Damping;
        public float Elasticity;
        public float Stiffness;
        public float Inert;
        public float Friction;
        public float Radius;
        public float BoneLength;
        public bool IsCollide;
        public bool IsEndBone;
        public bool NotNull;

        public int ChildCount;

        public float3 EndOffset;
        public float3 InitLocalPosition;
        public quaternion InitLocalRotation;

        public float3 LocalPosition;
        public quaternion LocalRotation;

        public float3 TempWorldPosition;
        public float3 TempPrevWorldPosition;

        public float3 ParentScale;

        public float3 WorldPosition;
        public quaternion WorldRotation;
    }

    public class DynamicBone : MonoBehaviour
    {
        [SerializeField] private Transform rootBoneTransform = null;

        [SerializeField] [Range(0, 1)]
        private float damping = 0.1f;

        [SerializeField] private AnimationCurve dampingDistribution = null;

        [SerializeField]
        [Range(0, 1)]
        private float elasticity = 0.1f;

        [SerializeField] private AnimationCurve elasticityDistribution = null;

        [SerializeField] [Range(0, 1)]
        private float stiffness = 0.1f;

        [SerializeField] private AnimationCurve stiffnessDistribution = null;

        [SerializeField]
        [Range(0, 1)]
        private float inert = 0;

        [SerializeField] private AnimationCurve inertDistribution = null;

        [SerializeField]
        private float friction = 0;

        [SerializeField] private AnimationCurve frictionDistribution = null;

        [SerializeField]
        private float radius = 0;

        [SerializeField] private AnimationCurve radiusDistribution = null;
 
        [SerializeField]
        private Vector3 gravity = Vector3.zero;

        [SerializeField]
        private Vector3 force = Vector3.zero;

        [SerializeField] private float endLength = 0;
        
        [SerializeField] private Vector3 endOffset = Vector3.zero;
        
        [SerializeField]
        private DynamicBoneCollider[] colliderArray = null;
        
        [SerializeField]
        private Transform[] exclusionTransformArray = null;
        
        private float boneTotalLength;
        
        private float weight = 1.0f;

        [NonSerialized] public NativeArray<ParticleInfo> ParticleInfoArray;
        [NonSerialized] public Transform[] ParticleTransformArray;

        private int particleCount;
        private bool hasInitialized;

        public Transform RootBoneParentTransform { get; private set; }

        [HideInInspector] public HeadInfo HeadInfo;

        private void OnValidate()
        {
            if(!RootBoneParentTransform) return;
            if (!hasInitialized) return;

            damping = Mathf.Clamp01(damping);
            elasticity = Mathf.Clamp01(elasticity);
            stiffness = Mathf.Clamp01(stiffness);
            inert = Mathf.Clamp01(inert);
            friction = Mathf.Clamp01(friction);
            radius = Mathf.Max(radius, 0);

            if (Application.isEditor && Application.isPlaying)
            {
                InitTransforms();
                UpdateParameters();
                DynamicBoneManager.Instance.RefreshHeadInfo(in HeadInfo);
                DynamicBoneManager.Instance.RefreshParticleInfo(in ParticleInfoArray,
                    in HeadInfo.DataOffsetInGlobalArray);
            }
        }

        private void Awake()
        {
            if (!rootBoneTransform)
            {
                rootBoneTransform = transform;
            }
            RootBoneParentTransform = rootBoneTransform.parent;
            if(!RootBoneParentTransform) return;
            
            ParticleInfoArray = new NativeArray<ParticleInfo>(DynamicBoneManager.MaxParticleLimit, Allocator.Persistent);
            ParticleTransformArray = new Transform[DynamicBoneManager.MaxParticleLimit];

            SetupParticles();
            DynamicBoneManager.Instance.AddBone(this);
            hasInitialized = true;
        }

        public HeadInfo ResetHeadIndexAndDataOffset(int headIndex)
        {
            HeadInfo.Index = headIndex;
            HeadInfo.DataOffsetInGlobalArray = headIndex * DynamicBoneManager.MaxParticleLimit;
            return HeadInfo;
        }

        public void ClearJobData()
        {
            if (ParticleInfoArray.IsCreated)
            {
                ParticleInfoArray.Dispose();
            }

            ParticleTransformArray = null;
        }

        private void SetupParticles()
        {
            if (rootBoneTransform == null)
                return;

            particleCount = 0;
            HeadInfo = new HeadInfo
            {
                ObjectPrevPosition = rootBoneTransform.position,
                ObjectScale = math.abs(rootBoneTransform.lossyScale.x),
                Gravity = gravity,
                Weight = weight,
                Force = force,
                ParticleCount = 0,
            };

            particleCount = 0;
            boneTotalLength = 0;
            AppendParticles(rootBoneTransform, -1, 0, in HeadInfo);
            UpdateParameters();

            HeadInfo.ParticleCount = particleCount;
        }

        private void AppendParticles(Transform b, int parentIndex, float boneLength, in HeadInfo head)
        {
            ParticleInfo particle = new ParticleInfo
            {
                Index = particleCount,
                ParentIndex = parentIndex,
                NotNull = true
            };
            
            particleCount++;
            
            
            if (b != null)
            {
                particle.LocalPosition = particle.InitLocalPosition = b.localPosition;
                particle.LocalRotation = particle.InitLocalRotation = b.localRotation;
                particle.TempWorldPosition = particle.TempPrevWorldPosition = particle.WorldPosition = b.position;
                particle.WorldRotation = b.rotation;
                particle.ParentScale = b.parent.lossyScale;
            }
            else 
            {
                Transform pb = ParticleTransformArray[parentIndex];
                if (endLength > 0)
                {
                    Transform ppb = pb.parent;
                    if (ppb != null)
                        particle.EndOffset = pb.InverseTransformPoint((pb.position * 2 - ppb.position)) * endLength;
                    else
                        particle.EndOffset = new Vector3(endLength, 0, 0);
                }
                else
                {
                    particle.EndOffset =
                        pb.InverseTransformPoint(rootBoneTransform.TransformDirection(endOffset) + pb.position);
                }

                particle.TempWorldPosition = particle.TempPrevWorldPosition = pb.TransformPoint(particle.EndOffset);
                particle.IsEndBone = true;
            }
                   
            if (parentIndex >= 0)
            {
                boneLength += math.distance(ParticleTransformArray[parentIndex].position, particle.TempWorldPosition);
                particle.BoneLength = boneLength;
                boneTotalLength = math.max(boneTotalLength, boneLength);
            }
            
            int index = particle.Index;
            ParticleInfoArray[particle.Index] = particle;
            ParticleTransformArray[particle.Index] = b;
            
            if (b != null)
            {
                particle.ChildCount = b.childCount;
                for (int i = 0; i < b.childCount; ++i)
                {
                    bool exclude = false;
                    if (exclusionTransformArray != null)
                    {
                        for (int j = 0; j < exclusionTransformArray.Length; j++)
                        {
                            Transform e = exclusionTransformArray[j];
                            if (e == b.GetChild(i))
                            {
                                exclude = true;
                                break;
                            }
                        }
                    }

                    if (!exclude)
                    {
                        AppendParticles(b.GetChild(i), index, boneLength, in head);
                    }
                    else if ( endLength > 0 || endOffset != Vector3.zero)
                    {
                        AppendParticles(null, index, boneLength, in head);
                    }
                }

                if (b.childCount == 0 && (endLength > 0 || endOffset != Vector3.zero))
                {
                    AppendParticles(null, index, boneLength, in head);
                }
            }
        }

        private void InitTransforms()
        {
            for (int i = 0; i < ParticleInfoArray.Length; ++i)
            {
                ParticleInfo particleInfo = ParticleInfoArray[i];
                particleInfo.LocalPosition = particleInfo.InitLocalPosition;
                particleInfo.LocalRotation = particleInfo.InitLocalRotation;
                ParticleInfoArray[i] = particleInfo;
            }
        }

        private void UpdateParameters()
        {
            if (rootBoneTransform == null)
                return;

            for (int i = 0; i < particleCount; ++i)
            {
                ParticleInfo particle = ParticleInfoArray[i];
                
                particle.Damping = damping;
                particle.Elasticity = elasticity;
                particle.Stiffness = stiffness;
                particle.Inert = inert;
                particle.Friction = friction;
                particle.Radius = radius;

                if (boneTotalLength > 0)
                {
                    float a = particle.BoneLength / boneTotalLength;

                    if (dampingDistribution != null && dampingDistribution.keys.Length > 0)
                        particle.Damping *= dampingDistribution.Evaluate(a);
                    if (elasticityDistribution != null && elasticityDistribution.keys.Length > 0)
                        particle.Elasticity *= elasticityDistribution.Evaluate(a);
                    if (stiffnessDistribution != null && stiffnessDistribution.keys.Length > 0)
                        particle.Stiffness *= stiffnessDistribution.Evaluate(a);
                    if (inertDistribution != null && inertDistribution.keys.Length > 0)
                        particle.Inert *= inertDistribution.Evaluate(a);
                    if (frictionDistribution != null && frictionDistribution.keys.Length > 0)
                        particle.Friction *= frictionDistribution.Evaluate(a);
                    if (radiusDistribution != null && radiusDistribution.keys.Length > 0)
                        particle.Radius *= radiusDistribution.Evaluate(a);
                }

                particle.Damping = Mathf.Clamp01(particle.Damping);
                particle.Elasticity = Mathf.Clamp01(particle.Elasticity);
                particle.Stiffness = Mathf.Clamp01(particle.Stiffness);
                particle.Inert = Mathf.Clamp01(particle.Inert);
                particle.Friction = Mathf.Clamp01(particle.Friction);
                particle.Radius = Mathf.Max(particle.Radius, 0);

                ParticleInfoArray[i] = particle;
            }
        }

        public DynamicBoneCollider[] GetColliderArray()
        {
            return colliderArray;
        }

        private void OnDestroy()
        {
            ParticleInfoArray.Dispose();
        }
    }

    public static class Util
    {
        public static float3 LocalToWorldPosition(float3 parentPosition, quaternion parentRotation, float3 targetLocalPosition)
        {
            return parentPosition + math.mul(parentRotation, targetLocalPosition);
        }

        public static quaternion LocalToWorldRotation(quaternion parentRotation, quaternion targetLocalRotation)
        {
            return math.mul(parentRotation, targetLocalRotation);
        }

        public static float3 WorldToLocalPosition(float3 parentPosition, quaternion parentRotation, float3 targetWorldPosition)
        {
            return float3.zero;
        }

        public static quaternion WorldToLocalRotation(quaternion parentRotation, quaternion targetWorldRotation)
        {
            return quaternion.identity;
        }
    }
}


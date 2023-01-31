using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TK.DynamicBone.job
{
    public class DynamicBoneManager : MonoBehaviour
    {
        private static DynamicBoneManager instance;

        public static DynamicBoneManager Instance
        {
            get
            {
                if (!instance) instance = new GameObject("DynamicBoneManager").AddComponent<DynamicBoneManager>();
                return instance;
            }
        }

        [SerializeField] private float updateRate = 60;

        public const int MaxParticleLimit = 20;

        [BurstCompile]
        private struct ColliderSetupJob : IJobParallelForTransform
        {
            public NativeArray<ColliderInfo> ColliderArray;

            public void Execute(int index, TransformAccess transform)
            {
                ColliderInfo colliderInfo = ColliderArray[index];
                colliderInfo.Position = transform.position;
                colliderInfo.Rotation = transform.rotation;
                // colliderInfo.Scale = transform.localScale.x;
                ColliderArray[index] = colliderInfo;
            }
        }

        [BurstCompile]
        private struct BoneSetupJob : IJobParallelForTransform
        {
            public NativeArray<HeadInfo> HeadArray;

            public void Execute(int index, TransformAccess transform)
            {
                HeadInfo curHeadInfo = HeadArray[index];
                curHeadInfo.RootParentBoneWorldPosition = transform.position;
                curHeadInfo.RootParentBoneWorldRotation = transform.rotation;

                curHeadInfo.ObjectMove = transform.position - (Vector3) curHeadInfo.ObjectPrevPosition;

                curHeadInfo.ObjectPrevPosition = transform.position;
                float3 force = curHeadInfo.Gravity;
                float3 forceDir = math.normalizesafe(force);
                float3 pf = forceDir * math.max(math.dot(force, forceDir),
                    0); 
                force -= pf; 
                force = (force + curHeadInfo.Force) * curHeadInfo.ObjectScale;
                curHeadInfo.FinalForce = force;

                HeadArray[index] = curHeadInfo;
            }
        }

        [BurstCompile]
        private struct UpdateParticles1Job : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HeadInfo> HeadArray;
            public NativeArray<ParticleInfo> ParticleArray;

            public void Execute(int index)
            {
                int headIndex = index / MaxParticleLimit;
                HeadInfo curHeadInfo = HeadArray[headIndex];

                int offset = index % MaxParticleLimit;
                if (offset == 0)
                {
                    float3 parentPosition = curHeadInfo.RootParentBoneWorldPosition;
                    quaternion parentRotation = curHeadInfo.RootParentBoneWorldRotation;

                    for (int j = 0; j < curHeadInfo.ParticleCount; j++)
                    {
                        int pIdx = curHeadInfo.DataOffsetInGlobalArray + j;
                        ParticleInfo p = ParticleArray[pIdx];



                        float3 localPosition = p.LocalPosition * p.ParentScale;
                        quaternion localRotation = p.LocalRotation;
                        float3 worldPosition =
                            Util.LocalToWorldPosition(parentPosition, parentRotation, localPosition);
                        quaternion worldRotation =
                            Util.LocalToWorldRotation(parentRotation, localRotation);

                        
           
                        parentPosition = p.WorldPosition = worldPosition;
                        parentRotation = p.WorldRotation = worldRotation;

                        ParticleArray[pIdx] = p;
                    }
                }

                if (offset >= curHeadInfo.ParticleCount) return;

                int particleId = curHeadInfo.DataOffsetInGlobalArray + offset;
                ParticleInfo particle = ParticleArray[particleId];


                if (particle.ParentIndex >= 0)
                {
                    float3 v = particle.TempWorldPosition - particle.TempPrevWorldPosition;
                    float3 rMove = curHeadInfo.ObjectMove * particle.Inert;
                    particle.TempPrevWorldPosition = particle.TempWorldPosition + rMove;

                    float damping = particle.Damping;
                    if (particle.IsCollide)
                    {
                        damping += particle.Friction;
                        if (damping > 1)
                            damping = 1;
                        particle.IsCollide = false;
                    }

                    particle.TempWorldPosition += v * (1 - damping) + curHeadInfo.FinalForce + rMove;
                }
                else
                {
                    particle.TempPrevWorldPosition = particle.TempWorldPosition;
                    particle.TempWorldPosition = particle.WorldPosition;
                }

                ParticleArray[particleId] = particle;
            }
        }

        [BurstCompile]
        private struct UpdateParticle2Job : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HeadInfo> HeadArray;
            public NativeArray<ParticleInfo> ParticleArray;
            [ReadOnly] public NativeArray<ColliderInfo> ColliderArray;
            [ReadOnly] public NativeParallelMultiHashMap<int, int> BoneColliderMatchMap;

            public void Execute(int index)
            {
                if (index % MaxParticleLimit == 0) return;

                int headIndex = index / MaxParticleLimit;
                HeadInfo curHeadInfo = HeadArray[headIndex];

                int offset = index % MaxParticleLimit;
                if (offset >= curHeadInfo.ParticleCount) return;

                int particleId = curHeadInfo.DataOffsetInGlobalArray + offset;
                ParticleInfo particleInfo = ParticleArray[particleId];

                int parentParticleIndex = curHeadInfo.DataOffsetInGlobalArray + particleInfo.ParentIndex;
                ParticleInfo parentParticleInfo = ParticleArray[parentParticleIndex];
                
                float3 pos = particleInfo.WorldPosition;
                float3 parentPos = parentParticleInfo.WorldPosition;
                
                
                Matrix4x4 m = float4x4.TRS(parentParticleInfo.TempWorldPosition, parentParticleInfo.WorldRotation,
                    particleInfo.ParentScale);

                float restLen = !particleInfo.IsEndBone
                    ? math.distance(parentPos, pos)
                    : m.MultiplyVector(particleInfo.EndOffset).magnitude;
                
                
                float stiffness = math.lerp(1.0f, particleInfo.Stiffness, curHeadInfo.Weight);
                if (stiffness > 0 || particleInfo.Elasticity > 0)
                {
                    float4x4 em0 = float4x4.TRS(parentParticleInfo.TempWorldPosition, parentParticleInfo.WorldRotation,
                        particleInfo.ParentScale);
                    float3 restPos = math.mul(em0, new float4(particleInfo.LocalPosition.xyz, 1)).xyz;
                    
                    float3 d = restPos - particleInfo.TempWorldPosition;
                    particleInfo.TempWorldPosition += d * particleInfo.Elasticity;
                    
                    if (stiffness > 0)
                    {
                        d = restPos - particleInfo.TempWorldPosition;
                        float len = math.length(d);
                        float maxLen = restLen * (1 - stiffness) * 2;
                        if (len > maxLen)
                            particleInfo.TempWorldPosition += d * ((len - maxLen) / len);
                    }
                }

                float3 dd = parentParticleInfo.TempWorldPosition - particleInfo.TempWorldPosition;
                float leng = math.length(dd);
                if (leng > 0)
                {
                    particleInfo.TempWorldPosition += dd * ((leng - restLen) / leng);
                }


                ParticleArray[particleId] = particleInfo;
            }
        }

        [BurstCompile]
        private struct ApplyToTransformJob : IJobParallelForTransform
        {
            public NativeArray<ParticleInfo> ParticleArray;
        
            public void Execute(int index, TransformAccess transform)
            {
                ParticleInfo particleInfo = ParticleArray[index];
                
                particleInfo.WorldPosition = particleInfo.TempWorldPosition;
                transform.position = particleInfo.WorldPosition;
                transform.rotation = particleInfo.WorldRotation;
                ParticleArray[index] = particleInfo;
            }
        }

        private List<DynamicBone> boneList;
        private List<DynamicBoneCollider> colliderList;
        private NativeList<HeadInfo> headInfoList;
        private NativeList<ParticleInfo> particleInfoList;
        private NativeList<ColliderInfo> colliderInfoList;
        private NativeParallelMultiHashMap<int, int> boneColliderMatchMap;
        private TransformAccessArray colliderTransformAccessArray;
        private TransformAccessArray headTransformAccessArray;
        private TransformAccessArray particleTransformAccessArray;
        private float time;

        private void Awake()
        {
            boneList = new List<DynamicBone>();
            colliderList = new List<DynamicBoneCollider>();

            headInfoList = new NativeList<HeadInfo>(200, Allocator.Persistent);
            headTransformAccessArray = new TransformAccessArray(200, 64);

            particleInfoList = new NativeList<ParticleInfo>(Allocator.Persistent);
            particleTransformAccessArray = new TransformAccessArray(200 * MaxParticleLimit, 64);

            colliderInfoList = new NativeList<ColliderInfo>(Allocator.Persistent);
            colliderTransformAccessArray = new TransformAccessArray(200, 64);


            boneColliderMatchMap = new NativeParallelMultiHashMap<int, int>(200, Allocator.Persistent);
        }


        private void LateUpdate()
        {
            UpdateAll();
        }

        private void UpdateAll()
        {
            int runningDynamicBoneCount = headInfoList.Length;
            if (runningDynamicBoneCount == 0) return;

            int dataArrayLength = runningDynamicBoneCount * MaxParticleLimit;


            JobHandle colliderSetup = new ColliderSetupJob()
            {
                ColliderArray = colliderInfoList
            }.Schedule(colliderTransformAccessArray);

            JobHandle boneSetup = new BoneSetupJob
            {
                HeadArray = headInfoList,
            }.Schedule(headTransformAccessArray);

            JobHandle dependency = JobHandle.CombineDependencies(colliderSetup, boneSetup);

            dependency = new UpdateParticles1Job
            {
                HeadArray = headInfoList,
                ParticleArray = particleInfoList,
            }.Schedule(dataArrayLength, MaxParticleLimit, dependency);

            dependency = new UpdateParticle2Job
            {
                HeadArray = headInfoList,
                ParticleArray = particleInfoList,
                ColliderArray = colliderInfoList,
                BoneColliderMatchMap = boneColliderMatchMap
            }.Schedule(dataArrayLength, MaxParticleLimit, dependency);

            dependency = new ApplyToTransformJob
            {
                ParticleArray = particleInfoList,
            }.Schedule(particleTransformAccessArray, dependency);

            dependency.Complete();
        }

        public void AddBone(DynamicBone target)
        {
            int index = boneList.IndexOf(target);
            if (index != -1) return; 

            boneList.Add(target);

            target.HeadInfo.DataOffsetInGlobalArray = particleInfoList.Length;

            int headIndex = headInfoList.Length;
            target.HeadInfo.Index = headIndex;

            foreach (var c in target.GetColliderArray())
            {
                boneColliderMatchMap.Add(headIndex, c.ColliderInfo.Index);
            }

            headInfoList.Add(target.HeadInfo);
            particleInfoList.AddRange(target.ParticleInfoArray);
            headTransformAccessArray.Add(target.RootBoneParentTransform);
            for (int i = 0; i < MaxParticleLimit; i++)
            {
                particleTransformAccessArray.Add(target.ParticleTransformArray[i]);
            }

        }

        public void RemoveBone(DynamicBone target)
        {
            int index = boneList.IndexOf(target);
            if (index == -1) return;

            boneList.RemoveAt(index);
            int curHeadIndex = target.HeadInfo.Index;

            boneColliderMatchMap.Remove(curHeadIndex);

            bool isEndTarget = curHeadIndex == headInfoList.Length - 1;
            if (isEndTarget)
            {
                headInfoList.RemoveAtSwapBack(curHeadIndex);
                headTransformAccessArray.RemoveAtSwapBack(curHeadIndex);
                for (int i = MaxParticleLimit - 1; i >= 0; i--)
                {
                    int dataOffset = curHeadIndex * MaxParticleLimit + i;
                    particleInfoList.RemoveAtSwapBack(dataOffset);
                    particleTransformAccessArray.RemoveAtSwapBack(dataOffset);
                }
            }
            else
            {
                DynamicBone lastTarget = boneList[boneList.Count - 1];
                HeadInfo lastHeadInfo = lastTarget.ResetHeadIndexAndDataOffset(curHeadIndex);
                headInfoList.RemoveAtSwapBack(curHeadIndex);
                headInfoList[curHeadIndex] = lastHeadInfo;
                headTransformAccessArray.RemoveAtSwapBack(curHeadIndex);
                for (int i = MaxParticleLimit - 1; i >= 0; i--)
                {
                    int dataOffset = curHeadIndex * MaxParticleLimit + i;
                    particleInfoList.RemoveAtSwapBack(dataOffset);
                    particleTransformAccessArray.RemoveAtSwapBack(dataOffset);
                }
            }
            target.ClearJobData();
        }

        public void RefreshHeadInfo(in HeadInfo headInfo)
        {
            headInfoList[headInfo.Index] = headInfo;
        }

        public void RefreshParticleInfo(in NativeArray<ParticleInfo> particleInfoArray, in int headOffsetInGlobalArray)
        {
            for (int i = headOffsetInGlobalArray; i < particleInfoArray.Length + headOffsetInGlobalArray; i++)
            {
                particleInfoList[i] = particleInfoArray[i - headOffsetInGlobalArray];
            }
        }

        public void AddCollider(DynamicBoneCollider target)
        {
            int index = colliderList.IndexOf(target);

            if (index != -1) return; 

            colliderList.Add(target);

            int colliderIndex = colliderInfoList.Length;
            target.ColliderInfo.Index = colliderIndex;

            colliderInfoList.Add(target.ColliderInfo);
            colliderTransformAccessArray.Add(target.transform);
        }

        public void RemoveCollider(DynamicBoneCollider target)
        {
            int index = colliderList.IndexOf(target);
            if (index == -1) return; 
        }

        public void RefreshColliderInfo(in ColliderInfo colliderInfo)
        {
            colliderInfoList[colliderInfo.Index] = colliderInfo;
        }


        private void OnDestroy()
        {
            if (particleTransformAccessArray.isCreated) particleTransformAccessArray.Dispose();
            if (particleInfoList.IsCreated) particleInfoList.Dispose();
            if (headInfoList.IsCreated) headInfoList.Dispose();
            if (headTransformAccessArray.isCreated) headTransformAccessArray.Dispose();
            if (colliderInfoList.IsCreated) colliderInfoList.Dispose();
            if (colliderTransformAccessArray.isCreated) colliderTransformAccessArray.Dispose();
            if (boneColliderMatchMap.IsCreated) boneColliderMatchMap.Dispose();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif
            for (int i = 0; i < particleInfoList.Length; ++i)
            {
                ParticleInfo p = particleInfoList[i];
                if (p.NotNull && p.ParentIndex >= 0)
                {
                    ParticleInfo p0 = particleInfoList[p.ParentIndex];
                    Gizmos.DrawLine(p.WorldPosition, p0.WorldPosition);
                }
                if (p.Radius > 0)
                    Gizmos.DrawWireSphere(p.WorldPosition, p.Radius * 1);
            }
        }
    }
}
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Collections;

namespace TK.BonePhysics
{
    [BurstCompile]
    public class PhysicsBone : MonoBehaviour
    {
        [SerializeField] [Range(0f, 5f)] private float damping = 1;

        [SerializeField] private float gravityScale = 1f;

        [SerializeField] private AnimationCurve forceFaloff = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });

        [SerializeField] private bool doCollisionChecks = true;

        [SerializeField] [Range(1f, 1000)] private float collisionAccuracy = 1000f;

        [SerializeField] [Range(0.01f, 5)] private float collisionRadius = 0.1f;

        [SerializeField] private AnimationCurve collisionRadiusDistrib = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        private CollisionMode collisionMode = CollisionMode.CenterPushout;
        private enum CollisionMode { CenterPushout, Piled }

        [SerializeField] private LayerMask collisionLayerMask = ~0;

        [SerializeField] private Transform distanceCheckTarget;
        [SerializeField] private float updateDistance = 15f;

        private Transform[] bones;
        private Vector3[] boneLocalPositions;
        private Quaternion[] boneLocalRotations;
        private float[] boneLocalDistances;

        private Vector3[] lastFrameBonePositions;
        private Quaternion gravityRootRotation;
        private float rootLocalEulerY;


        private bool restored;


        public void Start()
        {
            bones = GetComponentsInChildren<Transform>();

            boneLocalPositions = new Vector3[bones.Length];
            lastFrameBonePositions = new Vector3[bones.Length];
            boneLocalRotations = new Quaternion[bones.Length];
            boneLocalDistances = new float[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                lastFrameBonePositions[i] = bones[i].position;
                boneLocalPositions[i] = bones[i].localPosition;
                boneLocalRotations[i] = bones[i].localRotation;
                if (i > 0) boneLocalDistances[i] = Vector3.Distance(bones[i].position, bones[i].parent.position);
            }
            rootLocalEulerY = bones[0].localEulerAngles.y;
            gravityRootRotation = bones[0].rotation;

        }

        public void LateUpdate()
        {
            if (assertDistanceUpdateCheck()) return;

            Vector3 deltaMove = lastFrameBonePositions[0] - bones[0].position;
            bones[0].Rotate(deltaMove * 100);

            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].position = lastFrameBonePositions[i];
                bones[0].localPosition = boneLocalPositions[0];

                handleGravity();

                if (i > 0)
                {
                    handleMovement(i);
                    handleStretching(i);
                    if (doCollisionChecks) handleCollision(i);
                }
            }

            for (int i = 0; i < bones.Length; i++)
            {
                lastFrameBonePositions[i] = bones[i].position;
            }
        }

        private bool assertDistanceUpdateCheck()
        {
            if (distanceCheckTarget != null && Vector3.Distance(transform.position, distanceCheckTarget.position) > updateDistance)
            {
                if (!restored)
                {
                    for (int i = 0; i < bones.Length; i++)
                    {
                        bones[i].localPosition = boneLocalPositions[i];
                        bones[i].localRotation = boneLocalRotations[i];
                    }
                    restored = true;
                }
                return true;
            }
            else
            {
                restored = false;
            }
            return false;
        }

        private void handleGravity()
        {
            float gravityForce = gravityScale / Mathf.Clamp(damping, 0, 4) * Time.deltaTime;

            Quaternion boneRotation = bones[0].rotation;
            bones[0].localEulerAngles = new Vector3(0, rootLocalEulerY, 0);
            float worldSpaceY = bones[0].eulerAngles.y;
            bones[0].rotation = boneRotation;
            Quaternion targetGravityRotation = Quaternion.Euler(new Vector3(gravityRootRotation.eulerAngles.x, worldSpaceY, gravityRootRotation.eulerAngles.z));

            bones[0].rotation = Quaternion.Lerp(bones[0].rotation, targetGravityRotation, gravityForce);
        }

        private void handleMovement(int i)
        {
            float percentage = ((float)i / bones.Length);
            float curveValue = (1 - forceFaloff.Evaluate(percentage));

            bones[i].localPosition = Vector3.Lerp(bones[i].localPosition, boneLocalPositions[i], curveValue * Time.deltaTime * 10 * (5 - damping));
            bones[i].localRotation = Quaternion.Lerp(bones[i].localRotation, boneLocalRotations[i], curveValue * Time.deltaTime * 10 * (5 - damping));
        }

        private void handleStretching(int i)
        {
            float distanceToParent = Vector3.Distance(bones[i].parent.position, bones[i].position);
            if (distanceToParent != boneLocalDistances[i])
            {
                float difference = distanceToParent - boneLocalDistances[i];
                Vector3 direction = (bones[i].parent.position - bones[i].position).normalized;
                bones[i].position = bones[i].position + (direction * difference);
            }
        }

        private void handleCollision(int i)
        {

            float percentage = (float)i / bones.Length;
            float distrib = collisionRadiusDistrib.Evaluate(percentage);
            // Prevent from crashing
            collisionAccuracy = Mathf.Clamp(collisionAccuracy, 1, float.MaxValue);

            Collider[] collisions = Physics.OverlapSphere(bones[i].position, collisionRadius * distrib, collisionLayerMask);
            foreach (Collider collider in collisions)
            {
                if (collider is MeshCollider) continue;
                Vector3 point = collider.ClosestPoint(bones[i].position);
                Vector3 outDirection = Vector3.zero;
                switch (collisionMode)
                {
                    case CollisionMode.CenterPushout:
                        outDirection = (bones[i].position - collider.bounds.center).normalized;
                        while (Vector3.Distance(point, bones[i].position) < collisionRadius)
                        {
                            bones[i].position = bones[i].position + (outDirection / collisionAccuracy);
                        }
                        break;
                    case CollisionMode.Piled:
                        outDirection = (bones[i].position - collider.ClosestPointOnBounds(bones[i].position)).normalized;
                        float targetDistance = Vector3.Distance(bones[i].position, collider.ClosestPointOnBounds(bones[i].position));
                        if (Vector3.Distance(point, bones[i].position) < collisionRadius)
                        {
                            Debug.DrawLine(bones[i].position, collider.ClosestPointOnBounds(bones[i].position), Color.red);
                            bones[i].position = bones[i].position + (outDirection * targetDistance);
                        }
                        break;
                }
            }
        }



#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            collisionAccuracy = Mathf.Clamp(collisionAccuracy, 1, float.MaxValue);

            Transform[] bn = GetComponentsInChildren<Transform>();
            Gizmos.color = Color.yellow;
            for (int i = 0; i < bn.Length; i++)
            {
                float percentage = (float)i / bn.Length;
                float distrib = collisionRadiusDistrib.Evaluate(percentage);
                Gizmos.DrawWireSphere(bn[i].position, collisionRadius * distrib);
            }
        }
#endif
    }

}
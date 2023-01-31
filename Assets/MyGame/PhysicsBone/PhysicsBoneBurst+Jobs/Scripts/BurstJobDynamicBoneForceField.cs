using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TK.BurstJob.DynamicBone
{
    public sealed class DynamicBoneEditorAttribute : PropertyAttribute { }

    [CreateAssetMenu(fileName = "BJDBForce", menuName = "DynamicBone/Force")]
    public sealed class BurstJobDynamicBoneForceData : ScriptableObject
    {
        [SerializeField]
        private float m_Force = 1;
        public float force { get { return m_Force; } set { m_Force = value; } }

        public enum TurbulenceMode
        {
            Curve,
            Perlin,
        }

        [SerializeField]
        private Vector3 m_Turbulence = new Vector3(1f, 0.5f, 2f);
        public Vector3 turbulence { get { return m_Turbulence; } set { m_Turbulence = value; } }

        [SerializeField]
        private TurbulenceMode m_TurbulenceMode = TurbulenceMode.Perlin;
        public TurbulenceMode turbulenceMode { get { return m_TurbulenceMode; } set { m_TurbulenceMode = value; } }

        #region Perlin
        [SerializeField]
        private Vector3 m_Frequency = new Vector3(1f, 1f, 1.5f);
        public Vector3 frequency { get { return m_Frequency; } set { m_Frequency = value; } }
        #endregion

        #region Curve
        [SerializeField]
        private float m_TimeCycle = 2f;
        public float timeCycle { get { return m_TimeCycle; } set { m_TimeCycle = Mathf.Max(0, value); } }

        [SerializeField, Curve01]
        private AnimationCurve m_CurveX = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField, Curve01]
        private AnimationCurve m_CurveY = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Curve01]
        private AnimationCurve m_CurveZ = AnimationCurve.EaseInOut(0, 1, 1, 0);
        #endregion

        public Vector3 GetForce(float time)
        {
            Vector3 tbl = turbulence;
            switch (turbulenceMode)
            {
                case TurbulenceMode.Curve:
                    time = Mathf.Repeat(time, m_TimeCycle) / m_TimeCycle;
                    tbl.x *= Curve(m_CurveX, time);
                    tbl.y *= Curve(m_CurveY, time);
                    tbl.z *= Curve(m_CurveZ, time);
                    break;
                case TurbulenceMode.Perlin:
                    tbl.x *= Perlin(time * frequency.x, 0);
                    tbl.y *= Perlin(time * frequency.y, 0.5f);
                    tbl.z *= Perlin(time * frequency.z, 1.0f);
                    break;
            }
            return new Vector3(0, 0, force) + tbl;
        }

        private float Perlin(float x, float y)
        {
            return Mathf.PerlinNoise(x, y) * 2 - 1;
        }
        private float Curve(AnimationCurve curve, float time)
        {
            return curve.Evaluate(time);
        }
    }

    public sealed class BurstJobDynamicBoneForceField : MonoBehaviour
    {
        [SerializeField, Range(0, 1)]
        private float m_Conductivity = 0.15f;
        public float conductivity { get { return m_Conductivity; } set { m_Conductivity = value; } }

        [SerializeField, DynamicBoneEditorAttribute]
        private BurstJobDynamicBoneForceData m_Force;
        public BurstJobDynamicBoneForceData force { get { return m_Force; } set { m_Force = value; } }

        public float time { get; set; }

        private void OnEnable()
        {
            time = 0;
        }
        private void Update()
        {
            time += Time.deltaTime;
        }

        public Vector3 GetForce(float normalizedLength)
        {
            return transform.TransformDirection(force.GetForce(time - conductivity * normalizedLength));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawGizmos();
        }
        public void DrawGizmos()
        {
            if (force == null || !isActiveAndEnabled) return;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            if (Application.isPlaying)
            {
                Vector3 forceVector = force.GetForce(Time.time);
                float width = forceVector.magnitude * 0.2f;
                BurstJobsDynamicBoneFunctionUtility.DrawGizmosArrow(Vector3.zero, forceVector, width, Vector3.up);
                BurstJobsDynamicBoneFunctionUtility.DrawGizmosArrow(Vector3.zero, forceVector, width, Vector3.right);
                Gizmos.DrawRay(Vector3.zero, forceVector);
            }
            else
            {
                Vector3 forceVector = new Vector3(0, 0, force.force);
                float width = force.force * 0.2f;
                BurstJobsDynamicBoneFunctionUtility.DrawGizmosArrow(Vector3.zero, forceVector, width, Vector3.up);
                BurstJobsDynamicBoneFunctionUtility.DrawGizmosArrow(Vector3.zero, forceVector, width, Vector3.right);
                Gizmos.DrawRay(Vector3.zero, forceVector);
            }
            Gizmos.DrawWireCube(new Vector3(0, 0, force.force), force.turbulence * 2);
        }
#endif
    }
}

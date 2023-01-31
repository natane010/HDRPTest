using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Jobs;

namespace BreakObject
{

    public struct CubeData
    {
        public Matrix4x4 matrix;
        public float meshCount;
    }

    [BurstCompile]
    public class TestBreakCubeAlpha : MonoBehaviour
    {
        /// <summary>
        /// �Z���i�q��Ƀf�[�^���Ǘ�����B
        /// </summary>
        CubeData cubeData;

        /// <summary>
        /// �W���u�Ǘ��p�̂���
        /// </summary>
        TransformAccessArray transformAccess;
        NativeArray<Matrix4x4> nArray;

        /// <summary>
        /// �ʒu�̍X�V�p�W���u
        /// </summary>
        struct BurstJob : IJobParallelForTransform
        {
            public void Execute(int index, TransformAccess transform)
            {
                transform.position += Vector3.forward * 0.01f;
            }
        }
        /// <summary>
        /// �����I�������}�g���b�N�X�ŊǗ����邽�߂̃W���u
        /// </summary>
        struct BurstJob2 : IJobParallelForTransform
        {
            public List<Matrix4x4> matrix4X4s;
            public void Execute(int index, TransformAccess transform)
            {
                matrix4X4s.Add(transform.localToWorldMatrix);
            }
        }
        private void Start()
        {
            cubeData = new CubeData
            {
                matrix = this.gameObject.transform.localToWorldMatrix,
                meshCount = 27
            };
        }
        private void Update()
        {

        }
        /// <summary>
        /// �W���u�̔j��
        /// </summary>
        private void OnDestroy()
        {
            transformAccess.Dispose();
            nArray.Dispose();
        }
    }
}

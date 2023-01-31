using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace TK.Chinamachine
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class VirtualCameraContoroller : MonoBehaviour
    {
        private CinemachineVirtualCamera virtualCamera;
        private CinemachineOrbitalTransposer orbitalTransposer;
        private Vector2 lastMousePosition;
        // �J�����̊p�x���i�[����ϐ��i�����l��0,0�����j
        private Vector2 cameraAngle = new Vector2(0, 0);

        public float forwardSpeed;
        public float riseSpeed;
        void Start()
        {
            this.virtualCamera = this.GetComponent<CinemachineVirtualCamera>();
            this.orbitalTransposer = this.virtualCamera.GetComponentInChildren<CinemachineOrbitalTransposer>();
        }

        // Update is called once per frame
        void Update()
        {
            forwardViewPoint();
            heightViewPoint();
        }

        // �O��̃J��������
        private void forwardViewPoint()
        {
            // �}�E�X�z�C�[���̉�]�l��ϐ� scroll �ɓn��
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Vector3 offset = this.virtualCamera.transform.forward * scroll * forwardSpeed;
            orbitalTransposer.m_FollowOffset -= offset;
            Debug.Log(offset.ToString());
        }


        // ���������̃J��������
        private void heightViewPoint()
        {
            // ���N���b�N������
            if (Input.GetMouseButtonDown(0))
            {
                // �}�E�X���W��ϐ�"lastMousePosition"�Ɋi�[
                lastMousePosition = Input.mousePosition;
            }
            // ���h���b�O���Ă����
            else if (Input.GetMouseButton(0))
            {
                float y = (lastMousePosition.y - Input.mousePosition.y);
                orbitalTransposer.m_FollowOffset.y += y * riseSpeed;
                // �}�E�X���W��ϐ�"lastMousePosition"�Ɋi�[
                lastMousePosition = Input.mousePosition;
            }
        }
    }
}
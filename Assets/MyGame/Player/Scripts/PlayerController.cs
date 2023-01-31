using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TK.PlayerController
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Animator))]
	public sealed class PlayerController : MonoBehaviour
	{
		[SerializeField] float m_MovingTurnSpeed = 360;
		[SerializeField] float m_StationaryTurnSpeed = 180;
		[SerializeField] float m_JumpPower = 12f;
		[Range(1f, 4f)] [SerializeField] float m_GravityMultiplier = 2f;
		[SerializeField] float m_RunCycleLegOffset = 0.2f;
		[SerializeField] float m_MoveSpeedMultiplier = 1f;
		[SerializeField] float m_AnimSpeedMultiplier = 1f;
		[SerializeField] float m_GroundCheckDistance = 0.1f;

		#region IK Value
		[SerializeField]
		private bool useIKRot = true;
		//　右足のウエイト
		private float rightFootWeight = 0f;
		//　左足のウエイト
		private float leftFootWeight = 0f;
		//　右足の位置
		private Vector3 rightFootPos;
		//　左足の位置
		private Vector3 leftFootPos;
		//　右足の角度
		private Quaternion rightFootRot;
		//　左足の角度
		private Quaternion leftFootRot;
		//　右足と左足の距離
		private float distance;
		//　足を付く位置のオフセット値
		[SerializeField]
		private float offset = 0.1f;
		//　レイを飛ばす距離
		[SerializeField]
		private float rayRange = 1f;
		[SerializeField]
		private Vector3 rayPositionOffset = Vector3.up * 0.3f;
		#endregion

		Rigidbody m_Rigidbody;
		Animator m_Animator;
		bool m_IsGrounded;
		float m_OrigGroundCheckDistance;
		const float k_Half = 0.5f;
		float m_TurnAmount;
		float m_ForwardAmount;
		Vector3 m_GroundNormal;
		float m_CapsuleHeight;
		Vector3 m_CapsuleCenter;
		CapsuleCollider m_Capsule;
		bool m_Crouching;


		void Start()
		{
			m_Animator = GetComponent<Animator>();
			m_Rigidbody = GetComponent<Rigidbody>();
			m_Capsule = GetComponent<CapsuleCollider>();
			m_CapsuleHeight = m_Capsule.height;
			m_CapsuleCenter = m_Capsule.center;

			m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			m_OrigGroundCheckDistance = m_GroundCheckDistance;
		}


		public void Move(Vector3 move, bool crouch, bool jump)
		{

			if (move.magnitude > 1f) move.Normalize();
			move = transform.InverseTransformDirection(move);
			CheckGroundStatus();
			move = Vector3.ProjectOnPlane(move, m_GroundNormal);
			m_TurnAmount = Mathf.Atan2(move.x, move.z);
			m_ForwardAmount = move.z;

			ApplyExtraTurnRotation();

			if (m_IsGrounded)
			{
				HandleGroundedMovement(crouch, jump);
			}
			else
			{
				HandleAirborneMovement();
			}

			ScaleCapsuleForCrouching(crouch);
			PreventStandingInLowHeadroom();

			UpdateAnimator(move);
		}


		void ScaleCapsuleForCrouching(bool crouch)
		{
			if (m_IsGrounded && crouch)
			{
				if (m_Crouching) return;
				m_Capsule.height = m_Capsule.height / 2f;
				m_Capsule.center = m_Capsule.center / 2f;
				m_Crouching = true;
			}
			else
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
				{
					m_Crouching = true;
					return;
				}
				m_Capsule.height = m_CapsuleHeight;
				m_Capsule.center = m_CapsuleCenter;
				m_Crouching = false;
			}
		}

		void PreventStandingInLowHeadroom()
		{
			if (!m_Crouching)
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
				{
					m_Crouching = true;
				}
			}
		}


		void UpdateAnimator(Vector3 move)
		{
			m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
			m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
			m_Animator.SetBool("Crouch", m_Crouching);
			m_Animator.SetBool("OnGround", m_IsGrounded);
			if (!m_IsGrounded)
			{
				m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
			}

			float runCycle =
				Mathf.Repeat(
					m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
			float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
			if (m_IsGrounded)
			{
				m_Animator.SetFloat("JumpLeg", jumpLeg);
			}

			if (m_IsGrounded && move.magnitude > 0)
			{
				m_Animator.speed = m_AnimSpeedMultiplier;
			}
			else
			{
				m_Animator.speed = 1;
			}
		}


		void HandleAirborneMovement()
		{
			Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
			m_Rigidbody.AddForce(extraGravityForce);

			m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
		}


		void HandleGroundedMovement(bool crouch, bool jump)
		{
			if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
			{
				m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
				m_IsGrounded = false;
				m_Animator.applyRootMotion = false;
				m_GroundCheckDistance = 0.1f;
			}
		}

		void ApplyExtraTurnRotation()
		{
			float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
		}


		public void OnAnimatorMove()
		{
			if (m_IsGrounded && Time.deltaTime > 0)
			{

				Vector3 moveForward = transform.forward * m_Animator.GetFloat("motionZ") * Time.deltaTime;
				Vector3 v = ((m_Animator.deltaPosition + moveForward) * m_MoveSpeedMultiplier) / Time.deltaTime;

				v.y = m_Rigidbody.velocity.y;
				m_Rigidbody.velocity = v;
			}
		}

		private void OnAnimatorIK(int layerIndex)
		{
			//　アニメーションパラメータからIKのウエイトを取得
			rightFootWeight = m_Animator.GetFloat("RightFootWeight");
			leftFootWeight = m_Animator.GetFloat("LeftFootWeight");

			//　右足用のレイの視覚化
			Debug.DrawRay(m_Animator.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);
			//　右足用のレイを飛ばす処理
			var ray = new Ray(m_Animator.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up);

			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, rayRange, LayerMask.GetMask("Ground")))
			{
				rightFootPos = hit.point;

				//　右足IKの設定
				m_Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
				m_Animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPos + new Vector3(0f, offset, 0f));
				if (useIKRot)
				{
					rightFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
					m_Animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
					m_Animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRot);
				}
			}

			//　左足用のレイを飛ばす処理
			ray = new Ray(m_Animator.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up);
			//　左足用のレイの視覚化
			Debug.DrawRay(m_Animator.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);

			if (Physics.Raycast(ray, out hit, rayRange, LayerMask.GetMask("Ground")))
			{
				leftFootPos = hit.point;

				//　左足IKの設定
				m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
				m_Animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPos + new Vector3(0f, offset, 0f));

				if (useIKRot)
				{
					leftFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
					m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
					m_Animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRot);
				}
			}
		}


		void CheckGroundStatus()
		{
			RaycastHit hitInfo;
#if UNITY_EDITOR
			Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
#endif
			if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
			{
				m_GroundNormal = hitInfo.normal;
				m_IsGrounded = true;
				m_Animator.applyRootMotion = true;
			}
			else
			{
				m_IsGrounded = false;
				m_GroundNormal = Vector3.up;
				m_Animator.applyRootMotion = false;
			}
		}
	}

}

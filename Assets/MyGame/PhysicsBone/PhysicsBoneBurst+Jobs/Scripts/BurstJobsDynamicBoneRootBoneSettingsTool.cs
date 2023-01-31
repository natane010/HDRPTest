using UnityEngine;

namespace TK.BurstJob.DynamicBone
{
    public class BurstJobsDynamicBoneRootBoneSettingsTool : MonoBehaviour
    {
        [SerializeField]private int m_BoneGroupNum = 0;

        public int BoneGroupNumber { get { return m_BoneGroupNum; } }
    }
}

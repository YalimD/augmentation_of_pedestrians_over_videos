using System.Collections.Generic;

namespace UnityEngine.AI
{
    [ExecuteInEditMode]
    [AddComponentMenu("Navigation/NavMeshModifier", 32)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshModifier : MonoBehaviour
    {
        [SerializeField]
        bool m_OverrideArea;
        public bool overrideArea { get { return m_OverrideArea; } set { m_OverrideArea = value; } }

        [SerializeField]
        int m_Area;
        public int area { get { return m_Area; } set { m_Area = value; } }

        [SerializeField]
        bool m_IgnoreFromBuild;
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild; } set { m_IgnoreFromBuild = value; } }

        // List of agent types the modifier is applied for.
        // Special values: empty == None, m_AffectedAgents[0] =-1 == All.
        [SerializeField]
        List<int> m_AffectedAgents = new List<int>(new int[] { -1 });    // Default value is All

        public static List<NavMeshModifier> activeModifiers { get; } = new List<NavMeshModifier>();

        void OnEnable()
        {
            if (!activeModifiers.Contains(this))
                activeModifiers.Add(this);
        }

        void OnDisable()
        {
            activeModifiers.Remove(this);
        }

        public bool AffectsAgentType(int agentTypeID)
        {
            if (m_AffectedAgents.Count == 0)
                return false;
            if (m_AffectedAgents[0] == -1)
                return true;
            return m_AffectedAgents.IndexOf(agentTypeID) != -1;
        }
    }
}

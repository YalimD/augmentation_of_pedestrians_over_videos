using System.Collections.Generic;

namespace UnityEngine.AI
{
    [ExecuteInEditMode]
    [AddComponentMenu("Navigation/NavMeshModifierVolume", 31)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshModifierVolume : MonoBehaviour
    {
        [SerializeField]
        Vector3 m_Size = new Vector3(4.0f, 3.0f, 4.0f);
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }

        [SerializeField]
        Vector3 m_Center = new Vector3(0, 1.0f, 0);
        public Vector3 center { get { return m_Center; } set { m_Center = value; } }

        [SerializeField]
        int m_Area;
        public int area { get { return m_Area; } set { m_Area = value; } }

        // List of agent types the modifier is applied for.
        // Special values: empty == None, m_AffectedAgents[0] =-1 == All.
        [SerializeField]
        List<int> m_AffectedAgents = new List<int>(new int[] { -1 });    // Default value is All

        public static List<NavMeshModifierVolume> activeModifiers { get; } = new List<NavMeshModifierVolume>();

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

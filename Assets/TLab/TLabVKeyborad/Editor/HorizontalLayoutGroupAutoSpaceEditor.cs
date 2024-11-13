using UnityEngine;
using UnityEditor;

namespace TLab.VKeyborad.Editor
{
    [CustomEditor(typeof(HorizontalLayoutGroupAutoSpace))]
    public class HorizontalLayoutGroupAutoSpaceEditor : UnityEditor.Editor
    {
        private HorizontalLayoutGroupAutoSpace m_instance;

        private void OnEnable()
        {
            m_instance = target as HorizontalLayoutGroupAutoSpace;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Fit"))
                m_instance.Fit();
        }
    }
}

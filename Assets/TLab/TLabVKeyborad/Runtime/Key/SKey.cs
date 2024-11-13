using UnityEngine;

namespace TLab.VKeyborad
{
    public enum SKeyCode
    {
        BACKSPACE,
        TAB,
        SPACE,
        SHIFT,
        RETURN
    }

    public class SKey : KeyBase
    {
        [SerializeField] private SKeyCode m_sKey;

        public override void OnPress() => keyborad.OnSKeyPress(m_sKey);

        public override void OnShift() => m_lowerDisp.SetActive(true);

#if UNITY_EDITOR
        public override void SetUp()
        {
            base.SetUp();

            string name = gameObject.name;
            switch (name)
            {
                case "BACKSPACE":
                    m_sKey = SKeyCode.BACKSPACE;
                    break;
                case "RETURN":
                    m_sKey = SKeyCode.RETURN;
                    break;
                case "SHIFT":
                    m_sKey = SKeyCode.SHIFT;
                    break;
                case "SPACE":
                    m_sKey = SKeyCode.SPACE;
                    break;
                case "TAB":
                    m_sKey = SKeyCode.TAB;
                    break;
            }

            m_lowerDisp = transform.GetChild(0).gameObject;
            m_upperDisp = transform.GetChild(0).gameObject;
        }
#endif
    }
}

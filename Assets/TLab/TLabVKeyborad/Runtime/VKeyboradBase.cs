using UnityEngine;
using UnityEngine.Events;

namespace TLab.VKeyborad
{
    public class VKeyboradBase : MonoBehaviour
    {
        [Header("Callback")]
        [SerializeField] protected UnityEvent<bool> m_onVisibilityChanged;

        [SerializeField, HideInInspector]
        protected InputFieldBase m_inputFieldBase;

        protected bool m_mobile = false;
        protected bool m_initialized = false;

        public virtual bool mobile
        {
            get
            {
                m_mobile = Platform.mobile;
                return m_mobile;
            }
        }

        public virtual bool initialized => m_initialized;

        public virtual bool isVisible => true;

        public InputFieldBase inputFieldBase => m_inputFieldBase;

        private string THIS_NAME => "[ " + this.GetType() + "] ";

        public virtual void SwitchInputField(InputFieldBase inputFieldBase) => m_inputFieldBase = inputFieldBase;

        public virtual void SetVisibility(bool active)
        {
            if (active == isVisible)
                return;

            m_onVisibilityChanged.Invoke(active);
        }

        public void SwitchVisibility() => SetVisibility(!isVisible);

        public void Hide(bool active) => SetVisibility(!active);

        public void Show(bool active) => SetVisibility(active);

        public virtual void SetUp()
        {
            if (m_initialized)
            {
                Debug.LogError(THIS_NAME + "keyborad has already been initialised");
                return;
            }

            m_mobile = Platform.mobile;

            m_initialized = true;
        }

        protected virtual void Start()
        {
            if (!m_initialized)
                SetUp();
        }
    }
}

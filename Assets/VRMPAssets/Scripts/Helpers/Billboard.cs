using UnityEngine;

namespace XRMultiplayer
{
    public class Billboard : MonoBehaviour
    {
        [SerializeField] bool m_WorldUp;
        [SerializeField] bool m_FlipForward;

        protected Camera m_Camera;

        private void Awake()
        {
            // Initialize m_Camera as null, it will be set later by PlatformManager
            m_Camera = null;
        }

        // Add a custom method to set the camera
        public void SetCamera(Camera camera)
        {
            m_Camera = camera;
        }

        private void Update()
        {
            // Perform the rotation logic
            Quaternion lookRot = Quaternion.LookRotation(m_Camera.transform.position - transform.position);

            if (m_WorldUp)
            {
                Vector3 offset = lookRot.eulerAngles;
                offset.x = 0;
                offset.z = 0;

                if (m_FlipForward)
                    offset.y += 180;

                lookRot = Quaternion.Euler(offset);
            }

            transform.rotation = lookRot;
        }
    }
}

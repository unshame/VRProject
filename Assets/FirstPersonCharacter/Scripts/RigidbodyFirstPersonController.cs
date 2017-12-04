using System;
using UnityEngine;

namespace Characters.FirstPerson
{
    [RequireComponent(typeof (Rigidbody))]
    [RequireComponent(typeof (CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        [Serializable]
        public class MovementSettings
        {
            public float JetForce = 10f;
            public float Acceleration = 2f;
            public float HighSpeed = 50f;
        }


        [Serializable]
        public class AdvancedSettings
        {
            public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            public bool airControl = true; // can the user control the direction that is being moved in the air
            [Tooltip("set it to 0.1 or more if you get stuck in wall")]
            public float shellOffset = 0.1f; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
        }


        public Camera cam;
        public GameObject sphere;
        public AudioClip soundJet, soundSkiSlow, soundSkiFast, soundWind;
        public MovementSettings movementSettings = new MovementSettings();
        public MouseLook mouseLook = new MouseLook();
        public AdvancedSettings advancedSettings = new AdvancedSettings();

        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private Vector3 m_GroundContactNormal;
        private bool m_PreviouslyGrounded, m_IsGrounded;
        private AudioSource m_AudioSkiing;
        private AudioSource m_AudioWind;


        public Vector3 Velocity
        {
            get { return m_RigidBody.velocity; }
        }

        public bool Grounded
        {
            get { return m_IsGrounded; }
        }


        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init (sphere.transform, cam.transform);
            var aSources = GetComponents<AudioSource>();
            m_AudioSkiing = aSources[0];
            m_AudioSkiing.clip = soundJet;
            m_AudioSkiing.Play();
            m_AudioWind = aSources[1];
            m_AudioWind.clip = soundWind;
            m_AudioWind.Play();
        }


        private void Update()
        {
            RotateView();
        }


        private void FixedUpdate()
        {
            GroundCheck();
            Vector2 input = GetInput();
            
            var isJumping = Input.GetButton("Jump");

            if (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon)
            {
                Vector3 desiredMove = cam.transform.forward*input.y + cam.transform.right*input.x;
               // desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

                desiredMove.x = desiredMove.x*movementSettings.Acceleration;

                desiredMove.z = desiredMove.z*movementSettings.Acceleration;

                desiredMove.y = desiredMove.y*movementSettings.Acceleration;

                m_RigidBody.AddForce(desiredMove, ForceMode.Impulse);
            }

            var speed = Vector3.Distance(m_RigidBody.velocity, Vector3.zero);
            m_AudioWind.volume = Math.Max(0.1f, Math.Min(movementSettings.HighSpeed, speed) / movementSettings.HighSpeed);
            m_AudioWind.pitch = Math.Min(movementSettings.HighSpeed, speed) / movementSettings.HighSpeed / 2 + 1;
            if (isJumping) {
                m_AudioSkiing.clip = soundJet;
                m_AudioSkiing.mute = false;
                m_AudioSkiing.volume = 0.9f;
                m_RigidBody.AddForce(new Vector3(0, movementSettings.JetForce * Time.deltaTime, 0), ForceMode.Impulse);
            }
            else if (m_IsGrounded && speed > 1) {
                m_AudioSkiing.clip = speed > movementSettings.HighSpeed ? soundSkiFast : soundSkiSlow;
                m_AudioSkiing.mute = false;
                m_AudioWind.volume = Math.Max(0.6f, Math.Min(movementSettings.HighSpeed, speed) / movementSettings.HighSpeed);
                m_AudioWind.pitch = Math.Min(movementSettings.HighSpeed, speed) / movementSettings.HighSpeed / 2 + 1;
            } else {
                m_AudioSkiing.mute = true;
            }
            if (!m_AudioSkiing.isPlaying) {
                m_AudioSkiing.Play();
            }
            Debug.Log(speed);
        }


        private Vector2 GetInput()
        {
            
            Vector2 input = new Vector2
                {
                    x = Input.GetAxis("Horizontal"),
                    y = Input.GetAxis("Vertical")
                };
            return input;
        }


        private void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation (sphere.transform, cam.transform);

            if (m_IsGrounded || advancedSettings.airControl)
            {
                // Rotate the rigidbody velocity to match the new direction that the character is looking
                Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
                m_RigidBody.velocity = velRotation*m_RigidBody.velocity;
            }
        }

        /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        private void GroundCheck()
        {
            m_PreviouslyGrounded = m_IsGrounded;
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                   ((m_Capsule.height/2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }
        }
    }
}

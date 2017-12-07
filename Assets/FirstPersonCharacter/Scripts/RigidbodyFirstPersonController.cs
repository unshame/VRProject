using System;
using UnityEngine;
using UnityEngine.UI;

namespace Characters.FirstPerson
{
    [RequireComponent(typeof (Rigidbody))]
    [RequireComponent(typeof (CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        [Serializable]
        public class JetpackSettings {
            public float force = 2f;
            public float verticleMultiplier = 1.2f;
            public float highSpeed = 50f;
            public float fuelMax = 200;
            public float fuelUsageRate = 100;
            public float fuelUsageDelay = 0.5f;
            public float fuelUsageThreshold = 30f;
            public float fuelRestoringRate = 150;
            public float fuelRestoringDelay = 0.5f;
            public float maxSpeed = 60f;
            public float belowSpeedLimitDrag = 0f;
            public float aboveSpeedLimitDrag = 1f;
            public float speedDisplayMultiplier = 4f;
        }


        [Serializable]
        public class AdvancedSettings {
            public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            [Tooltip("set it to 0.1 or more if you get stuck in wall")]
            public float shellOffset = 0.1f; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
        }

        public Text jetpackFuelCounter, speedCounter, pickupCounter;

        public Camera cam;

        public AudioClip soundJet, soundSkiSlow, soundSkiFast, soundWind, soundPickup;
        public JetpackSettings jetpackSettings = new JetpackSettings();
        public MouseLook mouseLook = new MouseLook();
        public AdvancedSettings advancedSettings = new AdvancedSettings();

        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private AudioSource audioSkiing, audioWind, audioJet, audioPickup;

        private bool isGrounded;

        private float jetpackFuel;
        private bool isJetting = false;
        private bool isJetRestoring = false;
        private float jetEndedTime;
        private float jetRestoringStartedTime;

        private int pickupsCollected = -1;
        private int pickupsTotal = 0;

        public Vector3 Velocity {
            get { return rigidBody.velocity; }
        }

        public bool Grounded {
            get { return isGrounded; }
        }

        private void Start() {
            rigidBody = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init(rigidBody.transform, cam.transform);
            var aSources = GetComponents<AudioSource>();
            audioSkiing = aSources[0];
            audioSkiing.clip = soundSkiSlow;
            audioSkiing.Play();
            audioWind = aSources[1];
            audioWind.clip = soundWind;
            audioWind.Play();
            audioJet = aSources[2];
            audioJet.clip = soundJet;
            audioJet.Play();
            audioPickup = aSources[3];
            audioPickup.clip = soundPickup;

            SetJetpackFuel(jetpackSettings.fuelMax);

            pickupsTotal = GameObject.FindGameObjectsWithTag("Pickup").Length;
            HandlePickup();
        }


        private void Update() {
            RotateView();
        }


        private void FixedUpdate() {
            GroundCheck();
            HandleInput();

            var speed = GetAdjustedSpeed(Vector3.Distance(rigidBody.velocity, Vector3.zero));
            DisplaySpeed(speed);
            AdjustWindSound(speed);
            AdjustSkiSound(speed);

            // Debug.Log(speed);
        }

        public float GetAdjustedSpeed(float speed) {
            if(speed > jetpackSettings.maxSpeed) {
                rigidBody.drag = jetpackSettings.aboveSpeedLimitDrag;
                return Vector3.Distance(rigidBody.velocity, Vector3.zero);
            }
            else {
                rigidBody.drag = jetpackSettings.belowSpeedLimitDrag;
                return speed;
            }
        }

        private void SetJetpackFuel(float fuel) {
            jetpackFuel = fuel;
            jetpackFuelCounter.text = "Fuel: " + (jetpackSettings.fuelUsageRate == 0 ? "infinity" : Math.Floor(fuel).ToString());
        }

        private void DisplaySpeed(float speed) {
            speedCounter.text = "Speed: " + Math.Floor(speed * 3.6 * jetpackSettings.speedDisplayMultiplier).ToString() + "km/h";
        }

        private void HandleInput() {
            Vector2 input = GetInput();

            float fuelRequired = Time.deltaTime * jetpackSettings.fuelUsageRate;

            if (
                (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && 
                jetpackFuel - fuelRequired > 0 && (!isJetRestoring || jetpackFuel > jetpackSettings.fuelUsageThreshold || Time.time - jetRestoringStartedTime > jetpackSettings.fuelUsageDelay)
            ) {

                Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;

                desiredMove.x = desiredMove.x * jetpackSettings.force;

                desiredMove.z = desiredMove.z * jetpackSettings.force;

                desiredMove.y = desiredMove.y * jetpackSettings.force * jetpackSettings.verticleMultiplier;

                rigidBody.AddForce(desiredMove, ForceMode.Impulse);

                SetJetpackFuel(jetpackFuel - fuelRequired);

                isJetting = true;
                isJetRestoring = false;
                jetEndedTime = Time.time;
            }
            else {
                if (jetpackFuel < jetpackSettings.fuelMax && Time.time - jetEndedTime > jetpackSettings.fuelRestoringDelay) {
                    SetJetpackFuel(Math.Min(jetpackFuel + Time.deltaTime * jetpackSettings.fuelRestoringRate, jetpackSettings.fuelMax));
                }
                isJetting = false;
                if (!isJetRestoring) {
                    isJetRestoring = true;
                    jetRestoringStartedTime = Time.time;
                }
            }

            if(isJetting && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A))) {
                audioJet.mute = false;
                if (!audioJet.isPlaying) {
                    audioJet.Play();
                }
            }
            else {
                audioJet.mute = true;
            }
        }

        private void AdjustWindSound(float speed) {
            audioWind.volume = Math.Max(0.1f, Math.Min(jetpackSettings.highSpeed, speed) / jetpackSettings.highSpeed);
            audioWind.pitch = Math.Min(jetpackSettings.highSpeed, speed) / jetpackSettings.highSpeed / 2 + 1;
        }

        private void AdjustSkiSound(float speed) {
            if (isGrounded && speed > 1) {
                audioSkiing.clip = speed > jetpackSettings.highSpeed ? soundSkiFast : soundSkiSlow;
                audioSkiing.mute = false;
                audioWind.volume = Math.Max(0.6f, Math.Min(jetpackSettings.highSpeed, speed) / jetpackSettings.highSpeed);
                audioWind.pitch = Math.Min(jetpackSettings.highSpeed, speed) / jetpackSettings.highSpeed / 2 + 1;
            }
            else {
                audioSkiing.mute = true;
            }
            if (!audioSkiing.isPlaying) {
                audioSkiing.Play();
            }
        }

        private Vector2 GetInput() {
            
            Vector2 input = new Vector2
                {
                    x = Input.GetAxis("Horizontal"),
                    y = Input.GetAxis("Vertical")
                };
            return input;
        }


        private void RotateView() {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation (rigidBody.transform, cam.transform);
        }

        /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        private void GroundCheck() {
            RaycastHit hitInfo;
            isGrounded = Physics.SphereCast(
                transform.position,
                capsule.radius * (1.0f - advancedSettings.shellOffset),
                Vector3.down,
                out hitInfo,
                ((capsule.height / 2f) - capsule.radius) + advancedSettings.groundCheckDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        void OnTriggerEnter(Collider other) {
            if (other.gameObject.CompareTag("Pickup")) {
                other.gameObject.SetActive(false);
                HandlePickup();
                audioPickup.Play();
            }
        }

        private void HandlePickup() {
            SetJetpackFuel(jetpackSettings.fuelMax);
            pickupsCollected++;
            pickupCounter.text = "Stars: " + pickupsCollected + "/" + pickupsTotal;
            if(pickupsCollected == pickupsTotal) {
                jetpackSettings.fuelUsageRate = 0;
                SetJetpackFuel(jetpackSettings.fuelMax);
            }
        }
    }
}

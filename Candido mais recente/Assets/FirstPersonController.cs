using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField]
        private bool m_IsWalking;
        [SerializeField]
        private float m_WalkSpeed;
        [SerializeField]
        private float m_RunSpeed;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_RunstepLenghten;
        [SerializeField]
        private float m_JumpSpeed;
        [SerializeField]
        private float m_StickToGroundForce;
        [SerializeField]
        private float m_GravityMultiplier;
        [SerializeField]
        private MouseLook m_MouseLook;
        [SerializeField]
        private bool m_UseFovKick;
        [SerializeField]
        private FOVKick m_FovKick = new FOVKick();
        [SerializeField]
        private bool m_UseHeadBob;
        [SerializeField]
        private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField]
        private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField]
        private float m_StepInterval;
        [SerializeField]
        private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField]
        private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField]
        private AudioClip m_LandSound;           // the sound played when character touches back on ground.
        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        private int count;
        public Text scoreText;
        public Text healthText;
        public Text winText;
        //public Text LooseText;
        //cod disparo
        private Transform playerT;
        private Rigidbody playerRB;
        public float maxShootDistance;
        private bool isDead;
        private bool isFiring;
        private bool isKicking;
        LineRenderer laser;
        public Light laserLight;
        float timer;
        float displayTime;
        //player fire rate
        public float timeBetweenBullets;
        public float damage;
        //player health
        public float health;
        public int score;

     
        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
            m_MouseLook.Init(transform, m_Camera.transform);
            count = 0;
            SetCountText();
            winText.text = "";
            // LooseText.text = "";
            //initiate machine state
            playerT = transform;
            isDead = false;
            isFiring = false;
            isKicking = false;
            //get linerenderer
            laser = GetComponent<LineRenderer>();
            laser.enabled = false;
            laserLight.enabled = false;
            //father chronos was here
            timer = 0.0f;
            displayTime = 0.2f * Time.deltaTime;
        }
        // Update is called once per frame
        private void Update()
        {
            RotateView();
            
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }
            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }
            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }
        public void SetCountText()
        {
            healthText.text = "Health : " + health;
            scoreText.text = "Total Eliminations : " + score;
            if (score >= 10)
            {
                winText.text = "You Survived Long Enough!!";
                //op�ao de restart aqui
            }
        }
        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }
        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;
            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;
            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;
                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
            if (Input.GetKey("escape"))
                Application.Quit();
            timer += Time.deltaTime;
            if (!isDead)
            {

                //shoot things
                ShootZombus();
            }
            if (timer >= displayTime * timeBetweenBullets)
            {
                DisableEffects();
            }
            m_MouseLook.UpdateCursorLock();
            
        }
        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }
        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }
            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }
            m_NextStep = m_StepCycle + m_StepInterval;
            PlayFootStepAudio();
        }
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("health"))
            {
                other.gameObject.SetActive(false);
                count = count + 1;
                SetCountText();
            }
        }
        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }
        void ShootZombus()
        {
            if (Input.GetMouseButton(0) && timer >= timeBetweenBullets)//left button pressed
            {
                isFiring = true;
            }
            else
            {
                isFiring = false;
            }
            if (isFiring)
            {
                timer = 0.0f;
                laser.enabled = true; //activate the line renderer
                laser.SetPosition(0, m_Camera.transform.position + m_Camera.transform.forward * 1.0f + m_Camera.transform.up * 0.05f + m_Camera.transform.right * 0.25f);
                //laser.SetPosition(0, playerT.position + playerT.forward * 10);
                laserLight.enabled = true;

               m_Camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
               Vector3 rayPos = playerT.position;
               Vector3 rayDir = m_Camera.transform.forward;
               RaycastHit shootHit;
                Ray shootRay = new Ray(rayPos, rayDir);
                if (Physics.Raycast(shootRay, out shootHit, maxShootDistance, LayerMask.GetMask("shootable")))
                {
                    laser.SetPosition(1, shootHit.point);
                    if (shootHit.collider.CompareTag("enemy"))
                    //if(shootHit.collider.isTrigger == false && shootHit.collider.CompareTag("zombu"))
                    {
                        //nao consegue i
                        enemy zbc = shootHit.collider.GetComponent<enemy>();
                        zbc.TakeDamage(damage);
                    }

                }
                else
                {
                    laser.SetPosition(1, playerT.position + m_Camera.transform.forward * maxShootDistance);
                }

            }
        }
        public void DisableEffects()
        {
            //turn of line renderer and laser light
            laser.enabled = false;
            laserLight.enabled = false;
        }
        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }
        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");
            bool waswalking = m_IsWalking;
#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);
            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }
            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }
        private void RotateView()
        {
            m_MouseLook.LookRotation(transform, m_Camera.transform);
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }
            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
        public void DiePlayerDie()
        {
            isDead = true;
            isFiring = false;
            isKicking = false;
            Destroy(gameObject, 1f);
            
        }

        //take damage
        public void TakeDamage(float dd)
        {
            health -= dd;
            SetCountText();
            if (health <= 0.0f)
            {
                DiePlayerDie();
                //game over
                //op�ao de restart aqui
            }
        }
        public void GainHealth(float hp)
        {
            health += hp;
            if (health > 100) health = 100;
            SetCountText();
        }
        public float FullHealth()
        {
            return health;
        }
        public void ScoringLikeAMan(int scr)
        {
            score += scr;
            SetCountText();
        }
    }
}
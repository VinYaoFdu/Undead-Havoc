using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class playerMoves : MonoBehaviour
{
    public Rigidbody2D rb;
    public Collider2D col;

    //Move speed & Face direction
    [Header("MOVE & FACE DIRECTION")]
    [SerializeField] public bool isFacingPositive = true;
    [SerializeField] float speed;
    private float horizontal;
    private float temp_speed;

    //Ground check
    [Header("GROUND CHECK")]
    public bool hitGround;
    public Transform onGround;
    public Animator animator;
    public LayerMask groundLayer;
    public LayerMask blocksLayer;

    //Player Jump
    [Header("PLAYER JUMP")]
    [SerializeField] bool isjumping;
    [SerializeField] float jumpTime;
    [SerializeField] float jumpForce;
    private bool canJump = true;
    private float temp_jumpForce;
    [SerializeField] float jumpMultiplier;
    [SerializeField] float fallMultiplier;

    Vector2 pGravity;

    //Player Climb
    [Header("PLAYER CLIMB")]
    public bool isTouchingBlocks;
    [SerializeField] private Vector2 offset1;
    [SerializeField] private Vector2 offset2;
    private Vector2 climbBegunPosition;
    private Vector2 climbOverPosition;

    //Player attack
    [Header("PLAYER ATTACK")]
    public bool isCrouching;
    public bool isAiming;
    public bool isFiring;
    public Transform shootPoint_std;
    public Transform shootPoint_crh;
    public GameObject bullet;    

    //Ray
    [Header("PLAYER RAY")]
    [SerializeField] private float raycastLength;
    [SerializeField] Vector2 offset;
    private Vector2 eyePosition;

    //player health
    [Header("PLAYER HEALTH")]
    public int maxHealth = 100;
    public int current_health;
    public int damage = 25;
    public HealthBar health_bar;
//    private bool isDamaged;

    //Collection
    [Header("COLLECTION")]
    public int Ammo = 0;
    public Text storageNum;

    //Sounds effects
    [Header("SOUNDS")]
    [SerializeField] AudioSource fireEffect;
    [SerializeField] AudioSource stepsEffect;


    void Start()
    {
        PlayerEnabled();
        current_health = maxHealth;
        health_bar.SetMaxHealth(maxHealth);

        pGravity = new Vector2(0, -Physics2D.gravity.y);
        temp_speed = speed;
        temp_jumpForce = jumpForce;
        isFiring = false;
        isTouchingBlocks = false;

        

    }


    // Update is called once per frame
    void FixedUpdate()
    {
        rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
        if (!isFacingPositive && horizontal > 0f)
        {
            Flip();
        }
        else if (isFacingPositive && horizontal < 0f)
        {
            Flip();
        }


        if (rb.velocity.y < 0)
        {
            rb.velocity -= pGravity * fallMultiplier * Time.deltaTime;
        }

        physicsCheck();
        jumpAnim();

        if (isAiming == true)
        {
            speed = 0;
            jumpForce = 0;
        }
        else if (isAiming == false)
        {
            speed = temp_speed;
            jumpForce = temp_jumpForce;
        }
    }



    public void TakeDamage(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
//            System.Random random = new System.Random();
//            damage = random.Next(0, 20);
            current_health -= damage;

            health_bar.setHealth(current_health);

            Debug.Log(current_health);

            if (current_health <= 0)
            {
                current_health = 0;
                PlayerDisabled();
                Debug.Log("dead!");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        
    }


    

    //player reload
    public void Reload(InputAction.CallbackContext context)
    {
        if(hitGround && !isCrouching && !isjumping && !isAiming && context.performed)
        {
            animator.SetBool("reloading", true);
            Ammo--;
        }else
            animator.SetBool("reloading", false);

    }

    public void Fire(InputAction.CallbackContext context)
    {
        
        if (!isCrouching && isAiming && !isFiring && !isTouchingBlocks && context.performed)
        {
            fireEffect.Play();
            animator.SetBool("firing", true);
            if(isFacingPositive)
                Instantiate(bullet, shootPoint_std.position, transform.rotation);
            if (!isFacingPositive)
            {
                GameObject bulletInstance = Instantiate(bullet, shootPoint_std.position, transform.rotation);
                bulletInstance.transform.localScale = new Vector3(-0.007190318f, 0.008811175f, 1);
            }
            isFiring = true;
        }

        if(isCrouching && isAiming && !isFiring && !isTouchingBlocks && context.performed)
        {
            fireEffect.Play();
            animator.SetBool("crouch&firing", true);

            if (isFacingPositive)
                Instantiate(bullet, shootPoint_crh.position, transform.rotation);
            if (!isFacingPositive)
            {
                GameObject bulletInstance = Instantiate(bullet, shootPoint_crh.position, transform.rotation);
                bulletInstance.transform.localScale = new Vector3(-0.007190318f, 0.008811175f, 1);
            }
            isFiring = true;
        }

        if (context.canceled)
        {
            isFiring = false;
            animator.SetBool("firing", false);
            animator.SetBool("crouch&firing", false);
        }
    }

    //player aims
    public void Aim(InputAction.CallbackContext context)
    {
        if (context.performed && hitGround)
        {
            animator.SetBool("aiming", true);
            isAiming = true;
        }
        if(context.canceled)
        {
            animator.SetBool("aiming", false);
            isAiming = false;
        }

        if((isCrouching && context.performed) || (context.performed && isCrouching))
        {
            animator.SetBool("crouching&aiming", true);
            isAiming = true;
        }
        if (!isCrouching && context.performed)
        {
            animator.SetBool("aiming", true);
            isAiming = true;
        }
        if (!isAiming)
        {
            animator.SetBool("aiming", false);
            animator.SetBool("crouching&aiming", false);
            isAiming = false;
        }
    }

    //player crouches
    public void Crouch(InputAction.CallbackContext context)
    {
        if (context.performed && hitGround)
        {
            stepsEffect.Stop();
            isCrouching = true;
            animator.SetBool("crouching", true);
            speed *= 0.1f;

        }
        if (context.canceled)
        {
            isCrouching = false;
            animator.SetBool("crouching", false);
            speed = temp_speed;

        }
    }

    //player jumps
    public void Jump(InputAction.CallbackContext context)
    {
        if (isTouchingBlocks)
        {
            canJump = false;
            speed = 0;
        }
        else
        {
            canJump = true;
            speed = temp_speed;
        }           


        if (context.performed && hitGround && canJump)
        {
            animator.SetBool("readyToJump", true);
//            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
//            animator.SetBool("jumping", true);
        }

        if (context.performed && isTouchingBlocks)
        {
            animator.SetBool("climbing", true);
        }else
            animator.SetBool("climbing", false);

        // Ignore player input during a jump
  

    }

    private void jumping()
    {
        animator.SetBool("readyToJump", false);
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        animator.SetBool("jumping", true);
    }
    
    private void climbOver()
    {
        transform.position = climbOverPosition;
    }

    //player moves
    public void Move(InputAction.CallbackContext context)
    {
        stepsEffect.Play();
        horizontal = context.ReadValue<Vector2>().x;
        animator.SetFloat("running", Mathf.Abs(horizontal));

        if (context.canceled)
        {
            stepsEffect.Stop();
        }


    }

    public void physicsCheck()
    {
        //Ray
        Vector2 raycastDirection = isFacingPositive ? transform.right : -transform.right;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, raycastDirection, raycastLength, LayerMask.GetMask("Ground"));
        Color rayColor = Color.red;
        if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            rayColor = Color.green;
            animator.SetBool("touchBlocks", true);
            isTouchingBlocks = true;
        }
        else
        {
            animator.SetBool("touchBlocks", false);
            isTouchingBlocks = false;
        }
        Debug.DrawRay(transform.position, raycastDirection * raycastLength, rayColor);        

        //=================
        Vector2 raycastDirection1 = isFacingPositive ? transform.right : -transform.right;
        RaycastHit2D hit1 = Physics2D.Raycast(eyePosition, raycastDirection1, raycastLength, LayerMask.GetMask("Ground"));
        Color rayColor1 = Color.red;
        if (hit.collider != null && hit1.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            rayColor1 = Color.green;
            animator.SetBool("touchBlocks", true);
            isTouchingBlocks = true;
        }
        else
        {
            animator.SetBool("touchBlocks", false);
            isTouchingBlocks = false;
        }
        Debug.DrawRay(eyePosition, raycastDirection1 * raycastLength, rayColor1);
        Vector2 playerPosition = transform.position;
        eyePosition = playerPosition + offset;
        //=================

        if (isTouchingBlocks)
        {
            transform.position = climbBegunPosition;
        }


        //Ground check
        if (col.IsTouchingLayers(groundLayer))
        {
            hitGround = true;
            isjumping = false;
        }
        else
        {
            hitGround = false;
            isjumping = true;
        }
    }

    //change animation stages
    public void jumpAnim()
    {
        animator.SetBool("idle", false);
        if (animator.GetBool("jumping"))
        {
            if(rb.velocity.y < 0)
            {
                animator.SetBool("jumping", false);
                animator.SetBool("falling", true);
            }
        }
        
        else if (hitGround)
        {
            animator.SetBool("falling", false);
            animator.SetBool("idle", true);
        }
    }

    // Player direction switch
    private void Flip()
    {
        if (!isjumping) //lock player's direction when jumping
        {
            isFacingPositive = !isFacingPositive;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1.0f;
            transform.localScale = localScale;
        }        
    }

    //Scene switch
    public void PlayerDisabled()
    {
        animator.enabled = false;
        rb.bodyType = RigidbodyType2D.Static;
    }

    public void PlayerEnabled()
    {
        animator.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    //object collection
    public void OnTriggerEnter2D(Collider2D collision)
    {
        //collect Ammor
        if (collision.tag == "Ammo")
        {
            Destroy(collision.gameObject);
            Ammo += 1;
            storageNum.text = Ammo.ToString();
        }

        //collect medicine
        if (collision.tag == "Medical")
        {
            Destroy(collision.gameObject);
            current_health += 30;
            health_bar.setHealth(current_health);

            if (current_health >= maxHealth)
            {
                current_health = maxHealth;
                health_bar.setHealth(current_health);
            }
            Debug.Log(current_health);
        }

        
    }

    //taking damages
    private void OnCollisionEnter2D(Collision2D enemy_col)
    {       
        if (enemy_col.gameObject.CompareTag("Enemy"))
        {
            current_health -= damage;
            health_bar.setHealth(current_health);
            Debug.Log(current_health);
            if (current_health <= 0)
            {
                current_health = 0;
                PlayerDisabled();
                Debug.Log("dead!");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }


    }
    
}
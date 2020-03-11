using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{

    public float forwardSpeed = 1.5f;
    public float sidewaysSpeed = 0.8f;
    public float jumpForce = 1.2f;

    private CharacterController controller;
    public bool isGrounded = false;
    public float gravity;
    public LayerMask hitLayer;
    public float rotationSpeed = 1f;

    public Transform head;

    public Animator animator;
    private float LiveRunMultiplier = 1;
    // Start is called before the first frame update
    void Start()
    {

        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        // hitLayer = LayerMask.NameToLayer("world");
    }


    public bool isRunning = false;
    private Vector3 moveDirection = Vector3.zero;

    private bool wasFirst = false;
    IEnumerator ResetTimer()
    {
        yield return new WaitForSeconds(0.5f);
        wasFirst = false;
    }

    // Update is called once per frame
    void Update()
    {

        float vAxis = Input.GetAxis("Vertical");
        float hAxis = Input.GetAxis("Horizontal");

        if (isGrounded)
        {
            moveDirection.y = 0;
            if (Input.GetKey(KeyCode.Space))
            {
                moveDirection.y = jumpForce;
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                if (wasFirst)
                {
                    LiveRunMultiplier = 1.5f;
                    isRunning = true;
                    animator.SetBool("isRunning", true);
                }
                else
                {
                    wasFirst = true;
                    StartCoroutine(ResetTimer());
                }
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                LiveRunMultiplier = 1;
                isRunning = false;
                animator.SetBool("isRunning", false);

            }
        }

        moveDirection = transform.TransformDirection(new Vector3(hAxis * sidewaysSpeed, moveDirection.y, vAxis * forwardSpeed * LiveRunMultiplier));
        moveDirection.y -= gravity * Time.deltaTime;

        controller.Move(moveDirection * Time.deltaTime);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, currentRotation.y + mouseX * rotationSpeed, 0);

            Vector3 currentHeadRotation = head.localRotation.eulerAngles;
            head.localRotation = Quaternion.Euler(currentHeadRotation.x - mouseY * rotationSpeed, 0, 0);
        }

        Ray ray = new Ray(transform.position, Vector3.down);
        Debug.DrawRay(transform.position, Vector3.down * 1f, Color.red);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1f, hitLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }


    }
}

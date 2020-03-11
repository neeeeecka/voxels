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

    public float acceleration = 0;
    // Start is called before the first frame update
    void Start()
    {

        controller = GetComponent<CharacterController>();
        // hitLayer = LayerMask.NameToLayer("world");
    }
    private Vector3 moveDirection = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        float vAxis = Input.GetAxis("Vertical");
        float hAxis = Input.GetAxis("Horizontal");

        moveDirection = transform.TransformDirection(new Vector3(hAxis * sidewaysSpeed, moveDirection.y, vAxis * forwardSpeed));
        moveDirection.y -= gravity * Time.deltaTime;

        if (isGrounded)
        {
            moveDirection.y = 0;
            acceleration = 0;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                moveDirection.y = jumpForce;
            }
        }

        controller.Move(moveDirection * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 currentRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, currentRotation.y + mouseX * rotationSpeed, 0);

        Vector3 currentHeadRotation = head.localRotation.eulerAngles;
        head.localRotation = Quaternion.Euler(currentHeadRotation.x - mouseY * rotationSpeed, 0, 0);

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

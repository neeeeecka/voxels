using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddForce : MonoBehaviour
{
    public bool force = false;
    private bool lastForce = false;
    public float speed = 0;
    public float time = 0;

    private Rigidbody rigidbody;
    private bool launch = false;
    // Start is called before the first frame update
    // Update is called once per frame
    IEnumerator Timer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (lastForce != force)
            {
                lastForce = force;
                speed = 0;
                time = 0;
            }
            if (force)
            {
                launch = true;
                time += 1f;

                if (time >= 3)
                {
                    rigidbody.velocity = Vector3.zero;
                    force = false;
                    launch = false;
                }

            }
        }
    }
    void Start()
    {
        rigidbody = this.GetComponent<Rigidbody>();
        StartCoroutine(Timer());
    }
    void Update()
    {

    }
    void FixedUpdate()
    {
        if (launch)
        {
            rigidbody.AddForce(transform.forward * Time.fixedDeltaTime, ForceMode.VelocityChange);
            speed = rigidbody.velocity.magnitude;
        }
    }
}

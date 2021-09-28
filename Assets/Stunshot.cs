using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stunshot : MonoBehaviour
{
	public float lifetime=1.5f;
	public float muzzleVelocity = 5f;
	public float safeDistance = 0.5f;
	private bool handlingCollision = false;
	protected Raceur whoFired; //avoid self-zapping
	
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lifetime-=Time.deltaTime;
        if(lifetime < 0) {
			Destroy(gameObject);
			return;
		}
		if(!GetComponent<Collider>().enabled) {
			if((transform.position - whoFired.transform.position).magnitude > safeDistance) {
				GetComponent<Collider>().enabled = true;
			}
		}
    }
    
    void OnCollisionEnter(Collision collision) {
		if(handlingCollision) {
			return;
		}
		handlingCollision = true;
		
		Raceur whoHit = collision.gameObject.GetComponent<Raceur>();
		
		if(whoHit == null) {
			Destroy(gameObject);
			return;
		}
		if(whoHit == whoFired) {
			handlingCollision = false;
			return;
		}
		whoHit.ImHit(5f);
		Destroy(gameObject);
	}
	
	public void Launch(Raceur sender) {
		whoFired = sender;
		Vector3 vel = sender.GetVelocity();
		vel+= muzzleVelocity*whoFired.transform.forward;
		GetComponent<Rigidbody>().velocity = vel;
	}
	
	
}

using UnityEngine;
using System.Collections;

public class StaticFractureOnImpact : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnCollisionEnter(Collision col){
		if(col.gameObject.tag=="Projectile"){
			Debug.Log("Entered collision with Projectile");
		}

	}
}

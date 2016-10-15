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
			//remove this game object's mesh renderer,box collider,Mesh filter and rigid body
			this.RemoveAllComponents();
			//create two game objects of half the the width as this cube
			//add them as child to this cube

		}

	}

	private void RemoveAllComponents(){
		BoxCollider boxCollider=this.gameObject.GetComponent(typeof(BoxCollider)) as BoxCollider;
		Destroy(boxCollider);

		MeshRenderer meshRenderer=this.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		Destroy(meshRenderer);

		MeshFilter mesh=this.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		Destroy(mesh);

		Rigidbody rigidBody=this.gameObject.GetComponent(typeof(Rigidbody)) as Rigidbody;
		Destroy(rigidBody);
	}
}

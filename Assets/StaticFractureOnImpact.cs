using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FortuneAlgorithm;

public class StaticFractureOnImpact : MonoBehaviour {

	public float width;
	public float height;
	public float thickness;

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
			Vector3 originalPosition=transform.position;
			Material material=this.RemoveAllComponents();
			//create two game objects of half the the width as this cube
			//add them as child to this cube
//			GameObject first = Instantiate(Resources.Load("SplitHalf 1", typeof(GameObject))) as GameObject;
//			GameObject second = Instantiate(Resources.Load("SplitHalf 2", typeof(GameObject))) as GameObject;
			DoublyConnectedEdgeList dcel= new VoronoiFracture().GetVoronoiDiagram();
			CreateBodiesForEachFace(dcel,material,originalPosition);
			DoublyConnectedEdgeList d=null;
//			d.ToString();//intentional program crash for pausing
		}

	}

	private Material RemoveAllComponents(){
		BoxCollider boxCollider=this.gameObject.GetComponent(typeof(BoxCollider)) as BoxCollider;
		Destroy(boxCollider);

		MeshRenderer meshRenderer=this.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		Material material=meshRenderer.material;
		Destroy(meshRenderer);

		MeshFilter mesh=this.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		Destroy(mesh);

		Rigidbody rigidBody=this.gameObject.GetComponent(typeof(Rigidbody)) as Rigidbody;
		Destroy(rigidBody);
		return material;
	}

	private void CreateBodiesForEachFace(DoublyConnectedEdgeList dcel,Material material,Vector3 originalPosition){

		float vWidth=dcel.ux-dcel.lx;
		float vHeight=dcel.uy-dcel.ly;
		float ox=originalPosition.x-width/2;
		float oy=originalPosition.z-height/2;
		//TODO hardcoded for now to figure out what the problem is
		ox=-9.45f;
		oy=-7.92f;
		foreach(Face face in dcel.faceList){

			float x=face.siteEvent.x * width/(vWidth*4);
			float y=face.siteEvent.y * height/(vHeight*3);

			GameObject fragment=new GameObject();
			fragment.AddComponent<Rigidbody>();
			MeshFilter meshFilter=fragment.AddComponent<MeshFilter>() as MeshFilter;
			MeshRenderer meshRenderer=fragment.AddComponent<MeshRenderer>() as MeshRenderer;
			meshRenderer.material=material;

			meshFilter.mesh=GetMeshFor(face,dcel);

			MeshCollider meshCollider=fragment.AddComponent<MeshCollider>() as MeshCollider;
			meshCollider.convex=true;
			meshCollider.sharedMesh=meshFilter.mesh;

			fragment.transform.position=new Vector3(ox+x,originalPosition.y,oy+y);
		}
	}

	private float Reduction(float possiblyNegative,float alwaysPositive){
		if(possiblyNegative<0){
			return possiblyNegative+alwaysPositive;
		}else{
			return possiblyNegative-alwaysPositive;
		}
	}

	private Mesh GetMeshFor(Face face,DoublyConnectedEdgeList dcel){
		float vWidth=dcel.ux-dcel.lx;
		float vHeight=dcel.uy-dcel.ly;

		List<Vector3> vertexList=new List<Vector3>();
		List<Vector2> uvList=new List<Vector2>();
		Edge t=face.GetStartingEdge();
		do{
			float x=t.origin.x * width/vWidth;
			float y=t.origin.y * height/vHeight;
			Vector3 vertex=new Vector3(x,thickness,y);
			vertexList.Add(vertex);
			uvList.Add(new Vector2(t.origin.x/vWidth,t.origin.y/vHeight));
			t=t.next;
		}while(t!=face.GetStartingEdge());

		//repeat for lower side
		t=face.GetStartingEdge();
		do{
			float x=t.origin.x * width/vWidth;
			float y=t.origin.y * height/vHeight;
			Vector3 vertex=new Vector3(x,-thickness,y);
			vertexList.Add(vertex);
			uvList.Add(new Vector2(t.origin.x/vWidth,t.origin.y/vHeight));
			t=t.next;
		}while(t!=face.GetStartingEdge());

		Vector3 []vertices=vertexList.ToArray();

		//there are n-2 triangles on this convex polygon
		List<int> triangles=new List<int>();

		//polyogn on upper side
		for(int i=1;i<vertices.Length/2-1;i++){
			//always add group of 3
			triangles.Add(0);
			triangles.Add(i+1);
			triangles.Add(i);
		}
			
		//polyogn on lower side
		int resetIndex=vertices.Length/2;
		for(int i=1;i<vertices.Length/2-1;i++){
			//always add group of 3
			triangles.Add(resetIndex+0);
			triangles.Add(resetIndex+i);
			triangles.Add(resetIndex+i+1);
		}
			
		//triangles for thickness sides
		for(int i=0;i<vertices.Length/2-1;i++){
			// add group of 3 for one triangle of rectangle
			triangles.Add(i);
			triangles.Add(i+1);
			triangles.Add(resetIndex+i+1);

			triangles.Add(i);
			triangles.Add(resetIndex+i+1);
			triangles.Add(resetIndex+i);
		}

		//handle the last case seperately because its wrapping around
		// add group of 3 for one triangle of rectangle (using exisitng i)
		int j=vertices.Length/2-1;
		triangles.Add(j);
		triangles.Add(0);
		triangles.Add(resetIndex+0);

		triangles.Add(j);
		triangles.Add(resetIndex+0);
		triangles.Add(resetIndex+j);

		int [] trianglesArray=triangles.ToArray();
		Mesh mesh= new Mesh();
		mesh.vertices=vertices;
		mesh.triangles=trianglesArray;
		mesh.uv=uvList.ToArray();
		return mesh;
	}
}

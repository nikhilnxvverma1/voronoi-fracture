using UnityEngine;
using System.Collections;

public class SplitCubeIntoFractures {

	private GameObject gameObject;
	private VoronoiCell[] voronoiCells;
	
	public SplitCubeIntoFractures(GameObject cubeGameObject){
		this.gameObject=cubeGameObject;
	}

	public void RemoveAllComponentsFromMainCube(){
		BoxCollider boxCollider=this.gameObject.GetComponent(typeof(BoxCollider)) as BoxCollider;
		MonoBehaviour.Destroy(boxCollider);

		MeshRenderer meshRenderer=this.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		MonoBehaviour.Destroy(meshRenderer);

		MeshFilter mesh=this.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		MonoBehaviour.Destroy(mesh);

		Rigidbody rigidBody=this.gameObject.GetComponent(typeof(Rigidbody)) as Rigidbody;
		MonoBehaviour.Destroy(rigidBody);
	}

	public void ComputeVoronoiDiagram(Collision col){
		this.voronoiCells=new VoronoiCell[8];
		this.voronoiCells[0]=new VoronoiCell(new Point(-0.5,-0.5));		
		this.voronoiCells[1]=new VoronoiCell(new Point(-0.5,0.5));
		this.voronoiCells[2]=new VoronoiCell(new Point(-0.2,0.2));
		this.voronoiCells[3]=new VoronoiCell(new Point(-0.1,0.8));
		this.voronoiCells[4]=new VoronoiCell(new Point(0.3,-0.7));
		this.voronoiCells[5]=new VoronoiCell(new Point(0.2,-0.1));
		this.voronoiCells[6]=new VoronoiCell(new Point(0.6,0.1));
		this.voronoiCells[7]=new VoronoiCell(new Point(0.7,0.5));

		this.voronoiCells[0].vertices=new Point[5];
		this.voronoiCells[0].vertices[0]=new Point(-1,0.1);
		this.voronoiCells[0].vertices[1]=new Point(-0.5,0.1);
		this.voronoiCells[0].vertices[2]=new Point(-0.2,0);//TODO
		this.voronoiCells[0].vertices[3]=new Point(-0.05,0);
		this.voronoiCells[0].vertices[4]=new Point(-0.2,0);
	}

	public void CreateGameObjectForEachCell(){

	}
}

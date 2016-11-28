using UnityEngine;
using System.Collections;
using FortuneAlgorithm;

public class VoronoiFracture{

	public DoublyConnectedEdgeList GetVoronoiDiagram(){
		//input
		float [,] input=new float[,]{{20,80},{40,60},{20,30},{70,70},{60,50}};

		DoublyConnectedEdgeList dcel=new DoublyConnectedEdgeList(0,0,100,100);


		SiteEvent siteEvent;
		Face face;

		//20,30
		siteEvent=new SiteEvent(20,20);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(50,0)));
		face.AddEdge(new Edge(new Vertex(48,15)));
		face.AddEdge(new Edge(new Vertex(15,45)));
		face.AddEdge(new Edge(new Vertex(0,45)));
		face.AddEdge(new Edge(new Vertex(0,0)));

		//70,30
		siteEvent=new SiteEvent(70,30);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(50,0)));
		face.AddEdge(new Edge(new Vertex(100,0)));
		face.AddEdge(new Edge(new Vertex(100,58)));
		face.AddEdge(new Edge(new Vertex(55,35)));
		face.AddEdge(new Edge(new Vertex(48,15)));

		//40,60
		siteEvent=new SiteEvent(40,40);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(48,15)));
		face.AddEdge(new Edge(new Vertex(55,35)));
		face.AddEdge(new Edge(new Vertex(43,65)));
		face.AddEdge(new Edge(new Vertex(15,45)));

		//60,50
		siteEvent=new SiteEvent(60,50);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(55,35)));
		face.AddEdge(new Edge(new Vertex(100,58)));
		face.AddEdge(new Edge(new Vertex(100,100)));
		face.AddEdge(new Edge(new Vertex(50,100)));
		face.AddEdge(new Edge(new Vertex(43,65)));

		//20,70
		siteEvent=new SiteEvent(20,70);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(0,45)));
		face.AddEdge(new Edge(new Vertex(15,45)));
		face.AddEdge(new Edge(new Vertex(43,65)));
		face.AddEdge(new Edge(new Vertex(50,100)));
		face.AddEdge(new Edge(new Vertex(0,100)));

		return dcel;
	}
}
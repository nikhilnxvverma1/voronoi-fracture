using UnityEngine;
using System.Collections;
using FortuneAlgorithm;

public class VoronoiFracture{

	public DoublyConnectedEdgeList GetVoronoiDiagram(){
		//input
		float [,] input=new float[,]{{20,80},{40,60},{20,30},{70,70},{60,50}};

		DoublyConnectedEdgeList dcel=new DoublyConnectedEdgeList(0,0,100,100);

		//20,20
		SiteEvent siteEvent=new SiteEvent(20,20);
		Face face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(40,30)));
		face.AddEdge(new Edge(new Vertex(0,85)));
		face.AddEdge(new Edge(new Vertex(0,0)));
		face.AddEdge(new Edge(new Vertex(50,0)));

		//70,30
		siteEvent=new SiteEvent(70,30);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(40,30)));
		face.AddEdge(new Edge(new Vertex(50,0)));
		face.AddEdge(new Edge(new Vertex(100,0)));
		face.AddEdge(new Edge(new Vertex(100,50)));


		//60,50
		siteEvent=new SiteEvent(60,50);
		face=new Face(siteEvent);
		siteEvent.face=face;
		dcel.faceList.Add(face);

		face.AddEdge(new Edge(new Vertex(40,30)));
		face.AddEdge(new Edge(new Vertex(100,50)));
		face.AddEdge(new Edge(new Vertex(100,100)));
		face.AddEdge(new Edge(new Vertex(0,100)));
		face.AddEdge(new Edge(new Vertex(0,85)));

		return dcel;
	}
}
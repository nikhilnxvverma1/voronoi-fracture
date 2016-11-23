using System;
using System.Collections;
using System.Collections.Generic;

namespace FortuneAlgorithm{
	
	class MainClass{
		
		public static void Main (string[] args){

			//input
			float [,] input=new float[,]{{23,43},{54,14},{34,12},{12,53},{12,56},{88,54}};

			//make the event queue
			PriorityQueue queue=new PriorityQueue();
			BeachLine beachline=new BeachLine();
			DoublyConnectedEdgeList dcel=new DoublyConnectedEdgeList();
			for(int i=0;i<input.Length/2;i++){
				queue.Push(new SiteEvent(input[i,0],input[i,1]));
			}

			InternalNode node=new InternalNode(new SiteEvent(4.83f,2.558f),new SiteEvent(3.16f,2.543f),null);
			InternalNode.BreakPoint b=node.ComputeBreakpointAt(1f);
			Console.WriteLine("breakpoint is "+b);
			while(!queue.IsEmpty()){
				Event thisEvent=queue.Pop();
				if (thisEvent!=null) {
					thisEvent.Handle (queue,beachline,dcel);
				}else{
					break;
				}
			}
		}
	}

	abstract class Event{
		public float x;
		public float y;

		public Event(float x,float y){
			this.x=x;
			this.y=y;
		}

		abstract public void Handle(PriorityQueue queue,BeachLine beachline,DoublyConnectedEdgeList dcel);

		public override string ToString (){
			return "("+x+","+y+")";
		}
	}

	class SiteEvent :Event{

		public SiteEvent(float x,float y):base(x,y){
			
		}

		override public void Handle(PriorityQueue queue,BeachLine beachline,DoublyConnectedEdgeList dcel){
			Console.WriteLine(this);

			Parabola above=beachline.GetParabolaFor(x,y);
			if(above==null){//this means that the beach line tree is empty
				beachline.InsertRootParabola(this);	
			}else{
				beachline.InsertAndSplit(this,above);
			}
		}
	}

	class CircleEvent : Event{
		Parabola parabola;	

		public CircleEvent(float x,float y):base(x,y){
			
		}
		override public void Handle(PriorityQueue queue,BeachLine beachline,DoublyConnectedEdgeList dcel){
			Console.WriteLine(this);
		}
	}

	class PriorityQueue{

		ArrayList heap=new ArrayList();

		public Event Top(){
			if(heap.Count>0){
				return  ((Event)heap[0]);
			}else{
				return null;
			}

		}

		public Event Pop(){
			if(heap.Count==0){
				return null;
			}

			Event lastTop=(Event)heap[0];

			if(heap.Count>1){
				object lastEvent=heap[heap.Count-1];
				heap.RemoveAt(heap.Count-1);
				heap[0]=lastEvent;
				BubbleDown(0);
			}else{
				heap.RemoveAt(heap.Count-1);
			}
			return lastTop;
		}

		public void Push(Event eventNode){
			heap.Add(eventNode);
			BubbleUp(heap.Count-1);
			Console.WriteLine("Inserted event "+eventNode.x+" "+eventNode.y);
		}

		public bool IsEmpty(){
			return false;
		}

		private int Parent(int index){
			return (index-1)/2;
		}

		private int Left(int index){
			return index*2+1;
		}

		private int Right(int index){
			return index*2+2;
		}

		private bool Compare(object o1,object o2){
			return ((Event)o1).y>((Event)o2).y;
		}

		private void BubbleUp(int index){
			while(index>0 && Compare(heap[index],heap[Parent(index)])){
				int parentIndex=Parent(index);

				//swap this position with its parent
				object t=heap[index];
				heap[index]=heap[parentIndex];
				heap[parentIndex]=t;
				index=parentIndex;
			}
		}

		private void BubbleDown(int index){
			int swapIndex=SmallestBetweenRootAndChildren(index);
			while(swapIndex!=index){

				object t=heap[index];
				heap[index]=heap[swapIndex];
				heap[swapIndex]=t;

				index=swapIndex;
				swapIndex=SmallestBetweenRootAndChildren(index);
			}
		}

		private int SmallestBetweenRootAndChildren(int index){

			int left=Left(index);
			int right=Right(index);

			//no child therefore parent is the smallest
			if(left>=heap.Count){
				return index;
			}

			//considering only parent and left
			if(right>=heap.Count){
				if(Compare(heap[index],heap[left])){
					return index;
				}else{
					return left;
				}
			}

			//considering all 3
			if(Compare(heap[left],heap[right])){
				if(Compare(heap[index],heap[left])){
					return index;
				}else{
					return left;
				}
			}else{
				if(Compare(heap[index],heap[right])){
					return index;
				}else{
					return right;
				}
			}
		}
	}

	abstract class Node{
		Node parent;

		public Node(Node p){
			parent=p;
		}

		abstract public bool IsLeaf();
		/**
		 * Traverses to the leaf node for the given x
		 */
		abstract public Node Traverse(float x,float y);
	}

	class Parabola:Node{
		SiteEvent siteEvent;
		CircleEvent circleEvent;

		public Parabola(SiteEvent siteEvent,Node parent):base(parent){
			this.siteEvent=siteEvent;
		}

		public override bool IsLeaf (){
			return true;
		}

		public override Node Traverse (float x, float y){
			return this;
		}
	}

	class InternalNode:Node{
		public SiteEvent site1;
		public SiteEvent site2;
		Edge edge;
		Node left;
		Node right;

		public InternalNode(SiteEvent site1,SiteEvent site2,Node parent):base(parent){
			this.site1=site1;
			this.site2=site2;
		}

		public override bool IsLeaf (){			
			return false;
		}

		public override Node Traverse (float x, float y){
			BreakPoint p=ComputeBreakpointAt(y);
			if(x>p.x){
				return right;
			}else{
				return left;
			}
		}

		public BreakPoint ComputeBreakpointAt(float y){

			//breakpoint is retrived from the center of the circle touching the two sites and being tangent to the sweep line

			//by substituting site1 and site 2 in the equation of the circle and substituting the y value of the sweepline 
			//we can get the x value of the point at which the circle touches the sweep line or in otherwords the x of the center
			float x=((site2.x*site2.x)+(site2.y*site2.y)-(site1.x*site1.x)-(site1.y*site1.y)+2*(site1.y)*y-2*(site2.y)*y)/(2*(site2.x-site1.x));

			//now we use the x value in the equation of the perpendicular bisector between the two sites to get the y of the center
			SiteEvent site=site1;
			if(site1.x==x){
				//to prevent divide by zero error while calculating slope
				site=site2;//assumingly the perpendicular bisector will never be a vertical line with infinite slope
			}
			float mx=(site.x+x)/2;
			float my=(site.y+y)/2;
			float slope=(site.y-y)/(site.x-x); 
			float inverseSlope=-1/slope;
			float c=my-inverseSlope*mx;

			//perpendicular bisector of a chord will always pass through the center of a circle
			float centerY=inverseSlope*x+c;
			return new BreakPoint(x,centerY);
		}

		public class BreakPoint{
			public float x;
			public float y;

			public BreakPoint(float x,float y){
				this.x=x;
				this.y=y;
			}

			public override string ToString (){
				return "("+x+","+y+")";
			}
		}
	}

	class BeachLine{

		Node root;

		public Parabola GetParabolaFor(float x,float y){
			if(root==null){
				return null;
			}
			Node node=root;
			do{
				node=node.Traverse(x,y);
			}while(!node.IsLeaf());

			return null;
		}

		public void InsertRootParabola(SiteEvent siteEvent){
			root=new Parabola(siteEvent,null);
		}

		public void InsertAndSplit(SiteEvent siteEvent,Parabola above){
			
		}
	}

	class Vertex{
		public float x;
		public float y;

	}

	class Face{
		SiteEvent siteEvent;
		Edge startingEdge;
	}

	class Edge{
		Vertex origin;
		Edge twin;
		Edge next;
		Edge previous;
		Face face;
	}

	class DoublyConnectedEdgeList{
		public List<Vertex> vertexList=new List<Vertex>();
		public List<Face> faceList=new List<Face>();
		public List<Edge> edgeList=new List<Edge>();
	}
}

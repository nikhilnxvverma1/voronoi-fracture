using System;
using System.Collections;
using System.Collections.Generic;

namespace FortuneAlgorithm{
	
	class MainClass{
		
		public static void Main (string[] args){

			//input
			float [,] input=new float[,]{{20,80},{40,60},{20,30},{70,70},{60,50}};

			//make the event queue
			PriorityQueue queue=new PriorityQueue();
			BeachLine beachline=new BeachLine();
			DoublyConnectedEdgeList dcel=new DoublyConnectedEdgeList();
			for(int i=0;i<input.Length/2;i++){
				queue.Push(new SiteEvent(input[i,0],input[i,1]));
			}

//			InternalNode node=new InternalNode(new SiteEvent(40f,60f),new SiteEvent(70f,70f),null);
//			InternalNode.BreakPoint b=node.ComputeBreakpointAt(50f);
//			Console.WriteLine("breakpoint is "+b);
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
		public int index;

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
			Console.WriteLine("Handling Site Event: "+this);

			Parabola above=beachline.GetParabolaFor(x,y);
			if(above==null){//this means that the beach line tree is empty
				beachline.InsertRootParabola(this);	
			}else{
				if(above.circleEvent!=null){
					queue.Delete(above.circleEvent);
				}
				Parabola newParabola=beachline.InsertAndSplit(this,above);
				Triplet leftSide=beachline.FindTripletFromLeftSide(newParabola);
				if(leftSide!=null){
					CircleEvent circleEvent=leftSide.ComputeCircleEvent();
					queue.Push(circleEvent);
				}
				Triplet rightSide=beachline.FindTripletFromRightSide(newParabola);
				if(rightSide!=null){
					CircleEvent circleEvent=rightSide.ComputeCircleEvent();
					queue.Push(circleEvent);
				}
			}
		}
	}

	class CircleEvent : Event{
		public Triplet triplet;	

		public CircleEvent(float x,float y,Triplet triplet):base(x,y){
			this.triplet=triplet;
		}
		override public void Handle(PriorityQueue queue,BeachLine beachline,DoublyConnectedEdgeList dcel){
			Console.WriteLine("Handling Circle Event: "+this);
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
				heap.RemoveAt(heap.Count-1);//we are removing the last element so is an O(1) operation since it doesn't shift remaining things up on place
				heap[0]=lastEvent;
				BubbleDown(0);
			}else{
				heap.RemoveAt(heap.Count-1);
			}
			return lastTop;
		}

		public void Push(Event eventNode){
			eventNode.index=heap.Count;
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

				//swap this positions and indices with its parent
				SwapIndices(heap[index],heap[parentIndex]);
				object t=heap[index];
				heap[index]=heap[parentIndex];
				heap[parentIndex]=t;
				index=parentIndex;
			}
		}

		private void SwapIndices(object e1,object e2){
			int t=((Event)e1).index;
			((Event)e1).index=((Event)e2).index;
			((Event)e2).index=t;
		}

		private void BubbleDown(int index){
			int swapIndex=SmallestBetweenRootAndChildren(index);
			while(swapIndex!=index){

				//swap this positions and indices with the smallest node
				SwapIndices(heap[index],heap[swapIndex]);
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

		/**
		 * Deletes an event using its index inforamtion. This method returns false if the event object is not in the queue
		 **/
		public bool Delete(Event eventObj){
			Event eventInQ=(Event)heap[eventObj.index];
			if(eventInQ!=eventObj){
				return false;
			}

			if(heap.Count>1){
				object lastEvent=heap[heap.Count-1];
				heap.RemoveAt(heap.Count-1);//we are removing the last element so is an O(1) operation since it doesn't shift remaining things up on place
				heap[eventObj.index]=lastEvent;
				BubbleDown(eventObj.index);
			}else{
				heap.RemoveAt(heap.Count-1);
			}
			return true;
		}
	}

	abstract class Node{
		public InternalNode parent;

		public Node(InternalNode p){
			parent=p;
		}

		abstract public bool IsLeaf();
		/**
		 * Traverses to the leaf node for the given x,y 
		 */
		abstract public Node Traverse(float x,float y);
	}

	class Parabola:Node{
		public SiteEvent siteEvent;
		public CircleEvent circleEvent;

		public Parabola(SiteEvent siteEvent,InternalNode parent):base(parent){
			this.siteEvent=siteEvent;
		}

		public override bool IsLeaf (){
			return true;
		}

		public override Node Traverse (float x, float y){
			return this;
		}

		public override string ToString (){
			return "P"+this.siteEvent;
		}
	}

	class Triplet{
		public Parabola left;
		public Parabola middle;
		public Parabola right;

		public Triplet(Parabola left,Parabola middle,Parabola right){
			this.left=left;
			this.middle=middle;
			this.right=right;
		}

		public CircleEvent ComputeCircleEvent(){
			
			//get the equations of the two perpendicular bisectors
			PerpendicularBisector p1=new PerpendicularBisector(left.siteEvent.x,left.siteEvent.y,middle.siteEvent.x,middle.siteEvent.y);
			PerpendicularBisector p2=new PerpendicularBisector(right.siteEvent.x,right.siteEvent.y,middle.siteEvent.x,middle.siteEvent.y);

			//find out the intersectino point of the perpendicular bisectors to get the center of the circle
			Point center=p1.GetIntersectionPoint(p2);

			//get the radius by finding the difference between any point and the center
			float radius=(float)Math.Sqrt((center.x-left.siteEvent.x)*(center.x-left.siteEvent.x)+(center.y-left.siteEvent.y)*(center.y-left.siteEvent.y));
			return new CircleEvent(center.x,center.y-radius,this);
		}

		class PerpendicularBisector{
			float m;
			float c;
			bool horizontalLine=false;
			bool verticalLine=false;
			float level;

			public PerpendicularBisector(float x1,float y1,float x2,float y2){
				if(x1-x2==0){
					horizontalLine=true;
					level=(y1+y2)/2;
				}else if(y1-y2==0){
					verticalLine=true;
					level=(x1+x2)/2;
				}else{
					//midpoint
					float mx=(x1+x2)/2;
					float my=(y1+y2)/2;

					//inverse slope
					float normalSlope=(y1-y2)/(x1-x2);
					m=-(1/normalSlope);

					//substitute midpoint to get c
					c=my-m*mx;
				}


			}

			public Point GetIntersectionPoint(PerpendicularBisector lineEquation){
				float x,y;
				if(horizontalLine){
					if(lineEquation.verticalLine){
						x=lineEquation.level;
						y=level;
					}else if(lineEquation.horizontalLine){
						return null;//parallel lines
					}else{
						y=level;
						x=(y-lineEquation.c)/lineEquation.m;
					}
				}else if(verticalLine){
					if(lineEquation.verticalLine){
						return null;//parallel lines
					}else if(lineEquation.horizontalLine){
						x=level;
						y=lineEquation.level;
					}else{
						y=level;
						x=(y-lineEquation.c)/lineEquation.m;
					}
				}else{
					if(lineEquation.verticalLine){
						x=lineEquation.level;
						y=m*x+c;
					}else if(lineEquation.horizontalLine){						
						y=lineEquation.level;
						x=(y-c)/m;
					}else{
						x=(c-lineEquation.c)/(lineEquation.m-m);
						y=m*x+c;
					}
				}
					
				return new Point(x,y);
			}
		}

		class Point{
			public float x;
			public float y;
			public Point(float x,float y){
				this.x=x;
				this.y=y;
			}

			public override string ToString (){
				return "("+x+","+y+")";
			}
		}
			
	}

	class InternalNode:Node{
		public SiteEvent site1;
		public SiteEvent site2;
		public Edge edge;
		public Node left;
		public Node right;

		public InternalNode(SiteEvent site1,SiteEvent site2,InternalNode parent):base(parent){
			this.site1=site1;
			this.site2=site2;
		}

		public override bool IsLeaf (){			
			return false;
		}

		public override string ToString (){
			return "S"+site1+" "+site2;
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

			return (Parabola)node;
		}

		public void InsertRootParabola(SiteEvent siteEvent){
			root=new Parabola(siteEvent,null);
		}

		public Parabola InsertAndSplit(SiteEvent siteEvent,Parabola above){
			
			//we try to maintain the left site in site1 and right in site2 but it doesn't really make any difference
			SiteEvent site1,site2;
			if(siteEvent.x>above.siteEvent.x){
				site1=above.siteEvent;
				site2=siteEvent;
			}else{
				site1=siteEvent;
				site2=above.siteEvent;
			}

			//divide this parabola node into 3 parabola nodes by using two internal nodes 
			InternalNode replacer=new InternalNode(site1,site2,above.parent);
			replacer.left=new Parabola(above.siteEvent,replacer);

			InternalNode subNode=new InternalNode(site1,site2,replacer);
			replacer.right=subNode;

			Parabola newParabola=new Parabola(siteEvent,subNode);
			subNode.left=newParabola;
			subNode.right=new Parabola(above.siteEvent,subNode);

			//replace the above with replacer
			if(above.parent!=null){
				if(above==above.parent.left){
					above.parent.left=replacer;
				}else{
					above.parent.right=replacer;
				}
			}else{
				root=replacer;
			}

			return newParabola;
		}

		public Triplet FindTripletFromLeftSide(Parabola parabola){			
			//find the right most leaf node in the left subtree
			Parabola left1=FindLeftSibling(parabola);
			if(left1!=null){
				Parabola left2=FindLeftSibling(left1);
				if(left2!=null){
					return new Triplet(left2,left1,parabola);				
				}else{
					return null;
				}
			}else{
				return null;
			}
		}

		public Triplet FindTripletFromRightSide(Parabola parabola){			
			//find the left most leaf nod in the right subtree
			Parabola right1=FindRightSibling(parabola);
			if(right1!=null){
				Parabola right2=FindRightSibling(right1);
				if(right2!=null){
					return new Triplet(parabola,right1,right2);	
				}else{
					return null;
				}
			}else{
				return null;
			}
		}

		private Parabola FindLeftSibling(Parabola parabola){
			
			Node node=parabola;

			//look for the parent that has a left child
			Node comingFrom=null;
			while(node.parent!=null&& node.parent.left==node){
				comingFrom=node;
				node=node.parent;
			}

			//it reached the root coming from the left side
			if(node.parent==null && !node.IsLeaf()&& ((InternalNode)node).left==comingFrom){
				return null;
			}

			//get the right most leaf child node of this parent's left subtree
			if(node.parent.left.IsLeaf()){
				return (Parabola)node.parent.left;
			}else{
				Node generalNode=node.parent.left;
				while(!generalNode.IsLeaf()){
					generalNode=((InternalNode)generalNode).right;
				}
				return (Parabola)generalNode;
			}
		}

		private Parabola FindRightSibling(Parabola parabola){

			Node node=parabola;

			//look for the parent that has a left child
			Node comingFrom=null;
			while(node.parent!=null&& node.parent.right==node){
				comingFrom=node;
				node=node.parent;
			}

			//it reached the root coming from the right side
			if(node.parent==null && !node.IsLeaf() && ((InternalNode)node).right==comingFrom){
				return null;
			}

			//get the left most leaf child node of this parent's right subtree
			if(node.parent.right.IsLeaf()){
				return (Parabola)node.parent.right;
			}else{
				Node generalNode=node.parent.right;
				while(!generalNode.IsLeaf()){
					generalNode=((InternalNode)generalNode).left;
				}
				return (Parabola)generalNode;
			}

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

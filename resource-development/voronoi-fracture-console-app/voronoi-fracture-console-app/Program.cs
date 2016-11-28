using System;
using System.Collections;
using System.Collections.Generic;

namespace FortuneAlgorithm{


	class MainClass{
		
		public static void Main (string[] args){

			//input
			float [,] input=new float[,]{{20,80},{40,60},{20,30},{70,70},{60,50}};

			//{"sites":[200,800,400,600,200,300,700,700,600,500],"queries":[]}

			//make the event queue
			PriorityQueue queue=new PriorityQueue();
			BeachLine beachline=new BeachLine();
			DoublyConnectedEdgeList dcel=new DoublyConnectedEdgeList();
			for(int i=0;i<input.Length/2;i++){
				queue.Push(new SiteEvent(input[i,0],input[i,1]));
			}

//			InternalNode node=new InternalNode(new SiteEvent(40f,60f),new SiteEvent(70f,70f),null);
//			InternalNode.BreakPoint b=node.ComputeBreakpointAt(50f);
//			MainClass.Log("breakpoint is "+b);

			while(!queue.IsEmpty()){
				Event thisEvent=queue.Pop();
				if (thisEvent!=null) {
					thisEvent.Handle (queue,beachline,dcel);
				}else{
					break;
				}
			}

			MainClass.Log("Finished Computing Voronoi Diagram");
		}


		/**
		 * Simple log method, that is used throughout this namespace. 
		 * This is done so as to make this algorithm 'environment' independent by making simple change to this method.
		 * Example: In Unity this method will use 'Debug.Log' etc.
		 */
		public static void Log(string message){
			Console.WriteLine(message);
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
			MainClass.Log("Site Event: "+this);

			Parabola above=beachline.GetParabolaFor(x,y);
			if(above==null){//this means that the beach line tree is empty
				beachline.InsertRootParabola(this);	
			}else{
				if(above.circleEvent!=null){					
					//remove the circle event from priority queue and remove references
					above.circleEvent.Delete(queue);
				}

				//insert the new site's arc under the arc above
				Parabola newParabola=beachline.InsertAndSplit(this,above);

				//find consecutive triplets and if exists, add circle events in the queue
				Triplet leftSide=beachline.FindTripletOnLeftSide(newParabola);
				if(leftSide!=null){
					CircleEvent circleEvent=leftSide.ComputeCircleEvent();
					if(circleEvent.y<=y){
						circleEvent.Insert(queue);					
					}
				}
				Triplet rightSide=beachline.FindTripletOnRightSide(newParabola);
				if(rightSide!=null){
					CircleEvent circleEvent=rightSide.ComputeCircleEvent();
					if(circleEvent.y<=y){
						circleEvent.Insert(queue);
					}
				}
			}
		}
	}

	class CircleEvent : Event{
		float radius;
		public Triplet triplet;	

		public CircleEvent(float x,float y,float radius,Triplet triplet):base(x,y-radius){
			this.radius=radius;
			this.triplet=triplet;
		}
		override public void Handle(PriorityQueue queue,BeachLine beachline,DoublyConnectedEdgeList dcel){
			MainClass.Log("Circle Event: "+this);

			float cx=x;
			float cy=y+radius;

			// add a vertex in the DCEL and connect it to the dangling edge
			Vertex newVertex=new Vertex(cx,cy);
			dcel.vertexList.Add(newVertex);

			//assertion: middle arc will definetely have a grand parent 
			InternalNode grandParent=triplet.middle.parent.parent;

			//delete the middle arc and mind its parent's children
			Node sibling=triplet.middle.parent.OtherChild(triplet.middle);
			grandParent.Replace(triplet.middle.parent,sibling);//replace child

			//manage grandparent's site
			SiteEvent otherSiteEvent=triplet.middle.parent.OtherSiteEvent(triplet.middle.siteEvent);
			if(!grandParent.Contains(otherSiteEvent)){
				grandParent.Replace(triplet.middle.siteEvent,otherSiteEvent);//replace site event (overloaded method)
			}

			//nullify this circle event from the triplet arc
			triplet.middle.circleEvent=null;

			//find consecutive triplets and if exists, add circle events in the queue
			Triplet leftSide=beachline.FindTripletOnLeftSide(triplet.left);
			if(leftSide!=null){
				CircleEvent circleEvent=leftSide.ComputeCircleEvent();
				if(circleEvent.y<y){//this time we want them to be strictly below th beachline, because we don't want to repeat this current event again
					circleEvent.Insert(queue);
				}
			}
			Triplet rightSide=beachline.FindTripletOnRightSide(triplet.right);
			if(rightSide!=null){
				CircleEvent circleEvent=rightSide.ComputeCircleEvent();
				if(circleEvent.y<y){//this time we want them to be strictly below th beachline, because we don't want to repeat this current event again
					circleEvent.Insert(queue);
				}
			}
		}		

		/**
		 * Removes the circle event from priority queue and removes its references from the arc(only if they were set).
		 * Returns true if deleted from queue, false otherwise (in case it wasn't already in queue)
		 */ 
		public bool Delete(PriorityQueue queue){

			//nullify the references to this circle event in the triplet arcs(only if it is currently set to this)

			if(triplet.middle.circleEvent==this){
				triplet.middle.circleEvent=null;
			}

			return queue.Delete(this);
		}

		/**
		 * Inserts this circle event in the queue and sets this event as a references in the triplet arcs.
		 * Also checks are removes the circle event if existed in any of the triplet arc
		 * Return false if no existing event was found, true otherwise
		 */
		public bool Insert(PriorityQueue queue){
			queue.Push(this);

			//remove existing event
			bool existingCircleEventPresent=false;

			if(triplet.middle.circleEvent!=this && triplet.middle.circleEvent!=null){
				triplet.middle.circleEvent.Delete(queue);
				existingCircleEventPresent=true;
			}

			//set the references
			triplet.middle.circleEvent=this;

			return existingCircleEventPresent;
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
				SwapIndices(lastTop,lastEvent);
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
			MainClass.Log("Inserted event "+eventNode.x+" "+eventNode.y);
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

			MainClass.Log("Deleting "+eventObj);

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
			return new CircleEvent(center.x,center.y,radius,this);
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

		public override string ToString (){
			return left+" "+middle+" "+right;
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
			this.edge=new Edge();
		}

		public override bool IsLeaf (){			
			return false;
		}

		public string SiteToString(){
			return "S"+site1+" "+site2;
		}

		public override string ToString (){

			string leftSide=left==null?"Left:null":left.IsLeaf()?"Left:"+left:"Left:"+((InternalNode)left).SiteToString();
			string rightSide=right==null?"Right:null":right.IsLeaf()?"Right:"+right:"Right:"+((InternalNode)right).SiteToString();

			return SiteToString()+" "+leftSide+" "+rightSide;
		}

		public override Node Traverse (float x, float y){
			float breakPointX=ComputeBreakpointAt(y);
			if(x>breakPointX){
				return right;
			}else{
				return left;
			}
		}

		public bool Contains(SiteEvent siteEvent){
			return site1==siteEvent||site2==siteEvent;
		}

		public Node OtherChild(Node child){
			if(left==child){
				return right;
			}else if(right==child){
				return left;
			}else{
				return null;
			}
		}

		public SiteEvent OtherSiteEvent(SiteEvent siteEvent){
			if(siteEvent==site1){
				return site2;
			}else if(siteEvent==site2){
				return site1;
			}else{
				return null;
			}
		}

		public bool Replace(SiteEvent siteEvent,SiteEvent with){
			if(site1==siteEvent){
				site1=with;
				return true;
			}else if(site2==siteEvent){
				site2=with;
				return true;
			}else{
				return false;
			}
		}

		public bool Replace(Node node,Node with){
			if(left==node){
				left=with;
				with.parent=this;
				return true;
			}else if(right==node){
				right=with;
				with.parent=this;
				return true;
			}else{
				return false;
			}
		}

		/**
		 * Uses the circle technique to compute the breakpoint.(Deprecated because it only gives one breakpoint)
		 * Breakpoint is retrived from the center of the circle touching the two sites and being tangent to the sweep line.
		 */
		public BreakPoint ComputeBreakpointUsingCircleTechnique(float y){

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

		/**
		 * Uses the equation of parabola to compute the x value of the breakpoint.
		 */
		public float ComputeBreakpointAt(float y){

			//we use the equation of the parabola to get the intersection of the two arcs
			float d = 2f*(site1.y-y);
			float a1 = 1f/d;
			float b1 = -2f*site1.x/d;
			float c1 = y+d/4f+site1.x*site1.x/d;

			d = 2f*(site2.y-y);
			float a2 = 1f/d;
			float b2 = -2f*site2.x/d;
			float c2 = y+d/4+site2.x*site2.x/d;//minor adjustment

			float a = a1 - a2;
			float b = b1 - b2;
			float c = c1 - c2;

			//since this is a quadratic equation, so it will have 2 solutions
			float discremenant = b*b - 4 * a * c;
			float x1 = (-b+(float)Math.Sqrt(discremenant))/(2*a);
			float x2 = (-b-(float)Math.Sqrt(discremenant))/(2*a);

			//the two solutions are basically the left and the right breakpoint values (just x)
			return site1.x<=site2.x?Math.Min(x1,x2):Math.Max(x1,x2);
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

			//divide this parabola node into 3 parabola nodes by using two internal nodes 
			InternalNode replacer=new InternalNode(above.siteEvent,siteEvent,above.parent);
			replacer.left=new Parabola(above.siteEvent,replacer);

			InternalNode subNode=new InternalNode(siteEvent,above.siteEvent,replacer);
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

		public Triplet FindTripletOnLeftSide(Parabola parabola){			
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

		public Triplet FindTripletOnRightSide(Parabola parabola){			
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

		public override string ToString (){
			return "Root: "+this.root;
		}
	}

	class Vertex{
		public float x;
		public float y;
		public Vertex(float x,float y){
			this.x=x;
			this.y=y;
		}

		public override string ToString (){
			return "("+x+","+y+")";
		}
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

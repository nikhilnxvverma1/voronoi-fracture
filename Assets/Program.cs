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
			DoublyConnectedEdgeList dcel=new DoublyConnectedEdgeList(0,0,100,100);
			for(int i=0;i<input.Length/2;i++){

				//create a new site for this point
				SiteEvent siteEvent=new SiteEvent(input[i,0],input[i,1]);
				queue.Push(siteEvent);

				//instantiate and add the face object in the dcel here
				siteEvent.face=new Face(siteEvent);
				dcel.faceList.Add(siteEvent.face);
			}

			while(!queue.IsEmpty()){
				Event thisEvent=queue.Pop();
				if (thisEvent!=null) {
					thisEvent.Handle (queue,beachline,dcel);
				}else{
					break;
				}
			}

			//complete incomplete faces that may have dangling edges that need to get clipped by the bounds
			foreach(Face face in dcel.faceList){
				face.CompleteFaceIfIncomplete(dcel);	
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

	public abstract class Event{
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

	public class SiteEvent :Event{

		public Face face;

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
				AddEdgeForTheNewlyCreatedInternalNode(newParabola,dcel);

				//find consecutive triplets and if exists, add circle events in the queue
				Triplet leftSide=beachline.FindTripletOnLeftSide(newParabola);
				if(leftSide!=null){
					CircleEvent circleEvent=leftSide.ComputeCircleEvent();
					if(circleEvent!=null && circleEvent.y<=y){
						circleEvent.Insert(queue);					
					}
				}
				Triplet rightSide=beachline.FindTripletOnRightSide(newParabola);
				if(rightSide!=null){
					CircleEvent circleEvent=rightSide.ComputeCircleEvent();
					if(circleEvent!=null && circleEvent.y<=y){
						circleEvent.Insert(queue);
					}
				}
			}
		}

		/**
		 * Adding a new parabola by inserting and splitting always leads to 2 new internal nodes being created.
		 * Each internal node represents a breakpoint but only the top one will trace out an edge with a twin. 
		 * This method adds them to the DCEL
		 */ 
		private void AddEdgeForTheNewlyCreatedInternalNode(Parabola newArc,DoublyConnectedEdgeList dcel){			

			Edge edgeForSite=new Edge();
			InternalNode grandParent=newArc.parent.parent;
			grandParent.edge=edgeForSite;
			dcel.edgeList.Add(edgeForSite);
			dcel.edgeList.Add(edgeForSite.twin);
		}
	}

	public class CircleEvent : Event{
		float radius;
		public Triplet triplet;	

		public CircleEvent(float x,float y,float radius,Triplet triplet):base(x,y-radius){
			this.radius=radius;
			this.triplet=triplet;
		}

		override public void Handle(PriorityQueue queue,BeachLine beachline,DoublyConnectedEdgeList dcel){
			MainClass.Log("Circle Event: "+this);

			//assertion: middle arc will definetely have a grand parent 
			InternalNode grandParent=triplet.middle.parent.parent;

			//sibling of the middle arc
			Node sibling=triplet.middle.parent.OtherChild(triplet.middle);

			//One internal node always gets deleted(middles' parent), and another internal node gets modified because of convergence
			InternalNode converger=null;
			bool convergerOnRight;
			if(sibling.IsLeaf()){
				converger=grandParent;
				//check if middle's parent is left child of grandparent
				convergerOnRight=grandParent.left==triplet.middle.parent;
			}else{
				converger=(InternalNode)sibling;
				//check if sibling is right node of parent
				convergerOnRight=triplet.middle.parent.right==sibling;
			}				

			//delete the middle arc and mind its parent's children
			grandParent.Replace(triplet.middle.parent,sibling);//replace child

			//manage grandparent's site
			SiteEvent otherSiteEvent=triplet.middle.parent.OtherSiteEvent(triplet.middle.siteEvent);
			if(!grandParent.Contains(otherSiteEvent)){
				grandParent.Replace(triplet.middle.siteEvent,otherSiteEvent);//replace site event (overloaded method)
			}

			//using the same but possibly modified converger, connect the dangling edges and start a new edge
			//from the breakpoint that has been converged at that point
			ConnectDanglingEdges(converger,convergerOnRight,dcel);

			//nullify this circle event from the triplet arc
			triplet.middle.circleEvent=null;

			//find consecutive triplets and if exists, add circle events in the queue
			Triplet leftSide=beachline.FindTripletOnLeftSide(triplet.right);
			if(leftSide!=null){
				CircleEvent circleEvent=leftSide.ComputeCircleEvent();
				if(circleEvent!=null && circleEvent.y<y){//this time we want them to be strictly below th beachline, because we don't want to repeat this current event again
					circleEvent.Insert(queue);
				}
			}
			Triplet rightSide=beachline.FindTripletOnRightSide(triplet.left);
			if(rightSide!=null){
				CircleEvent circleEvent=rightSide.ComputeCircleEvent();
				if(circleEvent!=null && circleEvent.y<y){//this time we want them to be strictly below th beachline, because we don't want to repeat this current event again
					circleEvent.Insert(queue);
				}
			}
		}		

		private Vertex ConnectDanglingEdges(InternalNode converger,bool convergerOnRight,DoublyConnectedEdgeList dcel){

			// add a vertex in the DCEL and connect it to the dangling edge
			Vertex convergingPoint=new Vertex(x,y+radius);

			if(convergingPoint.y>dcel.uy){//above upper bound
				if(convergingPoint.x<dcel.lx){//before left bound
					ClipWithTopLeftCorner(dcel);
				}else if(convergingPoint.x>dcel.ux){//after right bound
					ClipWithTopRightCorner(dcel);
				}else{//between left and right bounds
					ClipWithTopBounds(convergingPoint,converger,convergerOnRight,dcel);
				}

			}else if(convergingPoint.y<dcel.ly){//below lower bound
				if(convergingPoint.x<dcel.lx){//before left bound
					ClipWithBottomLeftCorner(dcel);
				}else if(convergingPoint.x>dcel.ux){//after right bound
					ClipWithBottomRightCorner(dcel);
				}else{//between left and right bounds
					ClipWithBottomBounds(convergingPoint,converger,convergerOnRight,dcel);
				}
			}else{
				if(convergingPoint.x<dcel.lx){//before left bound
					ClipWithLeftBounds(convergingPoint,converger,convergerOnRight,dcel);
				}else if(convergingPoint.x>dcel.ux){//after right bound
					ClipWithRightBounds(convergingPoint,converger,convergerOnRight,dcel);
				}else{//between left and right bounds
					JoinVertexWithinBounds(convergingPoint,converger,convergerOnRight,dcel);
				}
			}

			return convergingPoint;
		}

		private void JoinVertexWithinBounds(Vertex vertex,InternalNode converger,bool convergerOnRight,DoublyConnectedEdgeList dcel){
			dcel.vertexList.Add(vertex);

			//every vertex in voronoi diagram is either the topmost or bottommost vertex of some face
			if(vertex.y>triplet.middle.siteEvent.y){ //top vertex of middle face

				//to find the terminated edge(edge that ends on this vertex)
				InternalNode terminatedEdgeNode=NodeForBreakpointBetween(triplet.left.siteEvent,triplet.right.siteEvent,triplet.middle.parent);					

				//old existing edge of converger's parent if present should be connected
				Edge terminatedEdge=terminatedEdgeNode.edge;
				if(terminatedEdge!=null){

					//if the origin of the left face's edge is not set, it means its getting clipped by the upper bounds
					if(terminatedEdge.origin==null){

						//get the clipped point
						float y=dcel.uy;//upper bounds of the bounding box(in our case sweepline is going from top to bottom)
						float x=dcel.GetXOfParabolaIntersectionGivenY(triplet.left.siteEvent,triplet.right.siteEvent,y);

						if(x<dcel.lx){//check if x is before the left bound							
							x=dcel.lx;//left bounds of the  bounding box(in our case sweepline is going from top to bottom)
							y=dcel.GetYOfParabolaIntersectionGivenX(triplet.left.siteEvent,triplet.right.siteEvent,x);

						}else if(x>dcel.ux){//check if x is after the right bound
							x=dcel.ux;//right bounds of the  bounding box(in our case sweepline is going from top to bottom)
							y=dcel.GetYOfParabolaIntersectionGivenX(triplet.left.siteEvent,triplet.right.siteEvent,x);
						}
						Vertex clippedPoint=new Vertex(x,y,true);

						//as a double edge, the orignal will be used on the left side 
						terminatedEdge.origin=clippedPoint;

						//Since we set origin on the edge of left face, we will append to forward list
						triplet.left.siteEvent.face.AppendToForwardList(terminatedEdge);

					}

					//for the right side, we will simple prepend the twin now by supplying the clipped point as the 
					//point for existing back terminal, and the converging point as the origin of twin
					terminatedEdge.twin.origin=vertex;
					triplet.right.siteEvent.face.PrependToBackwardList(terminatedEdge.twin,terminatedEdge.origin);

				}

				//get edge of grand parent 
				Edge existingEdge=converger.edge;

				//a new divergent edge(dangling) will be added to the parent of the convergent
				Edge divergent=new Edge();
				dcel.edgeList.Add(divergent);
				dcel.edgeList.Add(divergent.twin);
				converger.parent.edge=divergent;

				if(convergerOnRight){//existing edge right of divergent edge's twin
					triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(vertex,divergent.twin,existingEdge);

					//add the original divergent to the forward list of the left site's face
					divergent.origin=vertex;
					triplet.left.siteEvent.face.AppendToForwardList(divergent);

					//prepend the twin of the existing edge(which is another divergent) to the right face's backward list
					triplet.right.siteEvent.face.PrependToBackwardList(existingEdge.twin,vertex);
				}else{//existing edge left of divergent edge					

					triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(vertex,existingEdge.twin,divergent);

					//add the original divergent to the forward list of the left site's face
					existingEdge.origin=vertex;
					triplet.left.siteEvent.face.AppendToForwardList(existingEdge);

					//prepend the twin of the divergent to the right face's backward list
					triplet.right.siteEvent.face.PrependToBackwardList(divergent.twin,vertex);
				}

			}else{ //bottom vertex of middle face

				//if forward list hasn't started, instantiate and clip with upper bounds 
				if(triplet.middle.siteEvent.face.ForwardListNotStarted()){
					//this happens when the middle face is situated near the top bounds
					ClipClosingEdgeCrossingBoundsBetween(triplet.middle,triplet.right,dcel);
				}

				//if backward list hasn't started, instantiate and  clip with upper bounds 
				if(triplet.middle.siteEvent.face.BackwardListNotStarted()){
					//this happens when the middle face is situated near the top bounds
					ClipClosingEdgeCrossingBoundsBetween(triplet.left,triplet.middle,dcel);
				}

				triplet.middle.siteEvent.face.ConnectBackwardAndForwardListsAt(vertex);

				//a new divergent edge(dangling) will be added to the convergent
				Edge convergent=new Edge();
				convergent.origin=vertex;
				dcel.edgeList.Add(convergent);
				dcel.edgeList.Add(convergent.twin);

				converger.edge=convergent;

				//as a double edge, the original gets used for the left side and its twin for the right
				triplet.left.siteEvent.face.AppendToForwardList(convergent);
				triplet.right.siteEvent.face.PrependToBackwardList(convergent.twin,vertex);
			}
		}

		/** Geometrically only a top vertex event can happen here */
		private void ClipWithTopBounds(Vertex outside,InternalNode converger,bool convergerOnRight,DoublyConnectedEdgeList dcel){
			float y=dcel.uy;
			float x=dcel.GetXOfParabolaIntersectionGivenY(triplet.left.siteEvent,triplet.middle.siteEvent,y);
			Vertex leftPoint=new Vertex(x,y,true);
			dcel.vertexList.Add(leftPoint);

			y=dcel.uy;
			x=dcel.GetXOfParabolaIntersectionGivenY(triplet.middle.siteEvent,triplet.right.siteEvent,y);
			Vertex rightPoint=new Vertex(x,y,true);
			dcel.vertexList.Add(rightPoint);

			Edge pseudoEdge=new Edge(leftPoint,true);
			dcel.edgeList.Add(pseudoEdge);

			//get edge of grand parent 
			Edge existingEdge=converger.edge;

			//a new divergent edge(dangling) will be added to the parent of the convergent
			Edge divergent=new Edge();
			dcel.edgeList.Add(divergent);
			dcel.edgeList.Add(divergent.twin);
			converger.parent.edge=divergent;

			if(convergerOnRight){//existing edge right of divergent edge's twin
				triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(leftPoint,divergent.twin,existingEdge);

				//add the original divergent to the forward list of the left site's face
				divergent.origin=leftPoint;
				triplet.left.siteEvent.face.AppendToForwardList(divergent);

				//prepend the twin of the existing edge(which is another divergent) to the right face's backward list
				triplet.right.siteEvent.face.PrependToBackwardList(existingEdge.twin,leftPoint);
			}else{//existing edge left of divergent edge					

				triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(leftPoint,existingEdge.twin,divergent);

				//add the original divergent to the forward list of the left site's face
				existingEdge.origin=rightPoint;
				triplet.left.siteEvent.face.AppendToForwardList(existingEdge);

				//prepend the twin of the divergent to the right face's backward list
				triplet.right.siteEvent.face.PrependToBackwardList(divergent.twin,rightPoint);
			}				
		}

		/** Geometrically only a bottom vertex event can happen here */
		private void ClipWithBottomBounds(Vertex outside,InternalNode converger,bool convergerOnRight,DoublyConnectedEdgeList dcel){

			//if forward list hasn't started, instantiate and clip with upper bounds 
			if(triplet.middle.siteEvent.face.ForwardListNotStarted()){
				//this happens when the middle face is situated near the top bounds
				ClipClosingEdgeCrossingBoundsBetween(triplet.middle,triplet.right,dcel);
			}

			//if backward list hasn't started, instantiate and  clip with upper bounds 
			if(triplet.middle.siteEvent.face.BackwardListNotStarted()){
				//this happens when the middle face is situated near the top bounds
				ClipClosingEdgeCrossingBoundsBetween(triplet.left,triplet.middle,dcel);
			}

			float y=dcel.uy;
			float x=dcel.GetXOfParabolaIntersectionGivenY(triplet.left.siteEvent,triplet.middle.siteEvent,y);
			Vertex leftPoint=new Vertex(x,y,true);
			dcel.vertexList.Add(leftPoint);

			y=dcel.uy;
			x=dcel.GetXOfParabolaIntersectionGivenY(triplet.middle.siteEvent,triplet.right.siteEvent,y);
			Vertex rightPoint=new Vertex(x,y,true);
			dcel.vertexList.Add(rightPoint);

			Edge pseudoEdge=new Edge(rightPoint,true);
			dcel.edgeList.Add(pseudoEdge);

			//append the clipped section and close at the left point
			triplet.middle.siteEvent.face.AppendToForwardList(pseudoEdge);
			triplet.middle.siteEvent.face.ConnectBackwardAndForwardListsAt(leftPoint);
		}

		private void ClipWithLeftBounds(Vertex outside,InternalNode converger,bool convergerOnRight,DoublyConnectedEdgeList dcel){
			if(outside.y>triplet.middle.siteEvent.y){//top vertex of middle face

				//find the clipping with left bounds between left and middle
				float x=dcel.lx;
				float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.left.siteEvent,triplet.middle.siteEvent,x);
				Vertex leftClipBwLeftMiddle=new Vertex(x,y,true);
				dcel.vertexList.Add(leftClipBwLeftMiddle);

				//find the clipping with left bounds between middle and right
				x=dcel.lx;
				y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.right.siteEvent,x);
				Vertex leftClipBwMiddleRight=new Vertex(x,y,true);
				dcel.vertexList.Add(leftClipBwMiddleRight);

				//create  a pseudo edge for the clipped section
				Edge pseudoEdge=new Edge(leftClipBwMiddleRight,true);//middle-right will always be the upper on the left bound
				dcel.edgeList.Add(pseudoEdge);

				//get edge of grand parent 
				Edge existingEdge=converger.edge;

				//create the lists for middle face and set the existing edge's twin on the right face after setting its clipping point
				triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(leftClipBwMiddleRight,pseudoEdge,existingEdge);
				triplet.right.siteEvent.face.PrependToBackwardList(existingEdge.twin,leftClipBwMiddleRight);

				//a new divergent edge(dangling) will be added to the parent of the convergent
				Edge divergent=new Edge();
				dcel.edgeList.Add(divergent);
				dcel.edgeList.Add(divergent.twin);
				converger.parent.edge=divergent;

				//this divergent will also be between left and middle faces
				triplet.middle.siteEvent.face.PrependToBackwardList(divergent.twin,leftClipBwLeftMiddle);
				divergent.origin=leftClipBwLeftMiddle;
				triplet.left.siteEvent.face.AppendToForwardList(divergent);

			}else{//bottom vertex of middle face

				//if middle is above both left and right, it needs to be handled differently
				if(triplet.middle.siteEvent.y>triplet.left.siteEvent.y && triplet.middle.siteEvent.y>triplet.right.siteEvent.y){
					//find the clipping with left bounds
					float x=dcel.lx;
					float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.right.siteEvent,x);
					Vertex leftClip=new Vertex(x,y,true);
					dcel.vertexList.Add(leftClip);

					//append a pseudo edge
					Edge psuedoEdge=new Edge(leftClip,true);
					dcel.edgeList.Add(psuedoEdge);
					triplet.middle.siteEvent.face.AppendToForwardList(psuedoEdge);

					//find the lower clipped point
					x=dcel.lx;
					y=dcel.GetYOfParabolaIntersectionGivenX(triplet.right.siteEvent,triplet.left.siteEvent,x);
					Vertex lowerClip=new Vertex(x,y,true);
					dcel.vertexList.Add(lowerClip);

					//convergent will emerge on the other side and will get clipped when its other endpoint is encountered
					Edge convergent=new Edge();
					convergent.origin=lowerClip;
					dcel.edgeList.Add(convergent);
					dcel.edgeList.Add(convergent.twin);
					converger.edge=convergent;

					//we will add this convergent to the left face 
					triplet.left.siteEvent.face.AppendToForwardList(convergent);

					//for the right face, create another pseudo edge that will close it for the right face
					Edge closeRightFace=new Edge(lowerClip,true);
					dcel.edgeList.Add(closeRightFace);
					triplet.right.siteEvent.face.PrependToBackwardList(closeRightFace,leftClip);
					triplet.right.siteEvent.face.PrependToBackwardList(convergent.twin,lowerClip);	


				}else{
					//find the clipping with left bounds between left and middle
					float x=dcel.lx;
					float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.left.siteEvent,triplet.middle.siteEvent,x);
					Vertex leftClipBwLeftMiddle=new Vertex(x,y,true);
					dcel.vertexList.Add(leftClipBwLeftMiddle);

					//find the clipping with left bounds between middle and right
					x=dcel.lx;
					y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.right.siteEvent,x);
					Vertex leftClipBwMiddleRight=new Vertex(x,y,true);
					dcel.vertexList.Add(leftClipBwMiddleRight);

					//if forward list hasn't started, instantiate and clip with upper bounds 
					if(triplet.middle.siteEvent.face.ForwardListNotStarted()){
						//this happens when the middle face is situated near the top bounds
						ClipClosingEdgeCrossingBoundsBetween(triplet.middle,triplet.right,dcel);
					}

					//if backward list hasn't started, instantiate and  clip with upper bounds 
					if(triplet.middle.siteEvent.face.BackwardListNotStarted()){
						//this happens when the middle face is situated near the top bounds
						ClipClosingEdgeCrossingBoundsBetween(triplet.left,triplet.middle,dcel);
					}

					//create  a pseudo edge for the clipped section
					Edge pseudoEdge=new Edge(true);
					dcel.edgeList.Add(pseudoEdge);

					//prepend the pseddo edge and connect using middle-right(which is always be the lower on the left bound)
					triplet.middle.siteEvent.face.PrependToBackwardList(pseudoEdge,leftClipBwLeftMiddle);
					triplet.middle.siteEvent.face.ConnectBackwardAndForwardListsAt(leftClipBwMiddleRight);
				}
			}
		}

		private void ClipWithRightBounds(Vertex outside,InternalNode converger,bool convergerOnRight,DoublyConnectedEdgeList dcel){
			if(outside.y>triplet.middle.siteEvent.y){//top vertex of middle face

				//find the clipping with left bounds between left and middle
				float x=dcel.ux;
				float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.left.siteEvent,triplet.middle.siteEvent,x);
				Vertex rightClipBwLeftMiddle=new Vertex(x,y,true);
				dcel.vertexList.Add(rightClipBwLeftMiddle);

				//find the clipping with left bounds between middle and right
				x=dcel.ux;
				y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.right.siteEvent,x);
				Vertex rightClipBwMiddleRight=new Vertex(x,y,true);
				dcel.vertexList.Add(rightClipBwMiddleRight);

				//create  a pseudo edge for the clipped section
				Edge pseudoEdge=new Edge(rightClipBwLeftMiddle,true);//left-middle will always be the upper on the left bound
				dcel.edgeList.Add(pseudoEdge);

				//get edge of grand parent 
				Edge existingEdge=converger.edge;

				//create the lists for middle face and set the existing edge's twin on the left face after setting its clipping point
				triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(rightClipBwLeftMiddle,existingEdge.twin,pseudoEdge);
				existingEdge.origin=rightClipBwLeftMiddle;
				triplet.left.siteEvent.face.AppendToForwardList(existingEdge);

				//a new divergent edge(dangling) will be added to the parent of the convergent
				Edge divergent=new Edge();
				dcel.edgeList.Add(divergent);
				dcel.edgeList.Add(divergent.twin);
				converger.parent.edge=divergent;

				//this divergent will also be between right and middle faces
				triplet.right.siteEvent.face.PrependToBackwardList(divergent.twin,rightClipBwMiddleRight);
				divergent.origin=rightClipBwMiddleRight;
				triplet.middle.siteEvent.face.AppendToForwardList(divergent);

			}else{//bottom vertex of middle face
				//if middle is above both left and right, it needs to be handled differently
				if(triplet.middle.siteEvent.y>triplet.left.siteEvent.y && triplet.middle.siteEvent.y>triplet.right.siteEvent.y){
					//find the clipping with left bounds
					float x=dcel.ux;
					float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.right.siteEvent,x);
					Vertex rightClip=new Vertex(x,y,true);
					dcel.vertexList.Add(rightClip);

					//append a pseudo edge
					Edge psuedoEdge=new Edge(rightClip,true);
					dcel.edgeList.Add(psuedoEdge);
					triplet.middle.siteEvent.face.PrependToBackwardList(psuedoEdge,rightClip);

					//find the lower clipped point
					x=dcel.lx;
					y=dcel.GetYOfParabolaIntersectionGivenX(triplet.right.siteEvent,triplet.left.siteEvent,x);
					Vertex lowerClip=new Vertex(x,y,true);
					dcel.vertexList.Add(lowerClip);

					//convergent will emerge on the other side and will get clipped when its other endpoint is encountered
					Edge convergent=new Edge();
					convergent.origin=lowerClip;
					dcel.edgeList.Add(convergent);
					dcel.edgeList.Add(convergent.twin);
					converger.edge=convergent;

					//we will add this convergent to the right face 
					triplet.right.siteEvent.face.PrependToBackwardList(convergent.twin,lowerClip);

					//for the left face, create another pseudo edge that will close it for the left face
					Edge closeLeftFace=new Edge(rightClip,true);
					dcel.edgeList.Add(closeLeftFace);
					triplet.left.siteEvent.face.AppendToForwardList(closeLeftFace);
					triplet.left.siteEvent.face.AppendToForwardList(convergent);

				}else{
					//find the clipping with left bounds between left and middle
					float x=dcel.ux;
					float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.left.siteEvent,triplet.middle.siteEvent,x);
					Vertex rightClipBwLeftMiddle=new Vertex(x,y,true);
					dcel.vertexList.Add(rightClipBwLeftMiddle);

					//find the clipping with left bounds between middle and right
					x=dcel.ux;
					y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.right.siteEvent,x);
					Vertex rightClipBwMiddleRight=new Vertex(x,y,true);
					dcel.vertexList.Add(rightClipBwMiddleRight);

					//if forward list hasn't started, instantiate and clip with upper bounds 
					if(triplet.middle.siteEvent.face.ForwardListNotStarted()){
						//this happens when the middle face is situated near the top bounds
						ClipClosingEdgeCrossingBoundsBetween(triplet.middle,triplet.right,dcel);
					}

					//if backward list hasn't started, instantiate and  clip with upper bounds 
					if(triplet.middle.siteEvent.face.BackwardListNotStarted()){
						//this happens when the middle face is situated near the top bounds
						ClipClosingEdgeCrossingBoundsBetween(triplet.left,triplet.middle,dcel);
					}

					//create  a pseudo edge for the clipped section
					Edge pseudoEdge=new Edge(rightClipBwMiddleRight,true);//middle-right will be the upper one
					dcel.edgeList.Add(pseudoEdge);

					//prepend the pseddo edge and connect using middle-right(which is always be the lower on the left bound)
					triplet.middle.siteEvent.face.AppendToForwardList(pseudoEdge);
					triplet.middle.siteEvent.face.ConnectBackwardAndForwardListsAt(rightClipBwLeftMiddle);
				}
			}
		}

		/** Geometrically its only possible to get top vertex case here. */
		private void ClipWithTopLeftCorner(DoublyConnectedEdgeList dcel){

			//create another top left vertex
			Vertex topLeftCorner=new Vertex(dcel.lx,dcel.uy,true);
			dcel.vertexList.Add(topLeftCorner);

			//create two pseudo edges for the clips for the middle face
			Edge cornerTop=new Edge(topLeftCorner,true);
			Edge cornerLeft=new Edge(true);
			dcel.edgeList.Add(cornerTop);
			dcel.edgeList.Add(cornerLeft);

			triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(topLeftCorner,cornerLeft,cornerTop);
		}

		/** Geometrically its only possible to get top vertex case here. */
		private void ClipWithTopRightCorner(DoublyConnectedEdgeList dcel){			

			//create another top left vertex
			Vertex topRightCorner=new Vertex(dcel.ux,dcel.uy,true);
			dcel.vertexList.Add(topRightCorner);

			//create two pseudo edges for the clips for the middle face
			Edge cornerTop=new Edge(true);
			Edge cornerRight=new Edge(topRightCorner,true);
			dcel.edgeList.Add(cornerTop);
			dcel.edgeList.Add(cornerRight);

			triplet.middle.siteEvent.face.CreateForwardAndBackwardListsAt(topRightCorner,cornerTop,cornerRight);
		}

		/** Geometrically its only possible to get bottom vertex case here. */
		private void ClipWithBottomRightCorner(DoublyConnectedEdgeList dcel){			

			//find clipping points
			float x=dcel.ux;
			float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.middle.siteEvent,x);
			Vertex rightClip=new Vertex(x,y);
			dcel.vertexList.Add(rightClip);

			y=dcel.ly;
			x=dcel.GetXOfParabolaIntersectionGivenY(triplet.middle.siteEvent,triplet.left.siteEvent,y);
			Vertex bottomClip=new Vertex(x,y);
			dcel.vertexList.Add(bottomClip);

			//create two pseudo edges for the clips for the middle face
			Edge cornerRight=new Edge(rightClip,true);
			Edge cornerBottom=new Edge(true);
			dcel.edgeList.Add(cornerRight);
			dcel.edgeList.Add(cornerBottom);

			triplet.middle.siteEvent.face.AppendToForwardList(cornerRight);
			triplet.middle.siteEvent.face.PrependToBackwardList(cornerBottom,bottomClip);

			//create another bottom right vertex
			Vertex bottomRightCorner=new Vertex(dcel.ux,dcel.ly,true);
			dcel.vertexList.Add(bottomRightCorner);
			triplet.middle.siteEvent.face.ConnectBackwardAndForwardListsAt(bottomRightCorner);
		}

		/** Geometrically its only possible to get bottom vertex case here. */
		private void ClipWithBottomLeftCorner(DoublyConnectedEdgeList dcel){			

			//find clipping points
			float x=dcel.lx;
			float y=dcel.GetYOfParabolaIntersectionGivenX(triplet.middle.siteEvent,triplet.middle.siteEvent,x);
			Vertex leftClip=new Vertex(x,y);
			dcel.vertexList.Add(leftClip);

			y=dcel.ly;
			x=dcel.GetXOfParabolaIntersectionGivenY(triplet.middle.siteEvent,triplet.left.siteEvent,y);
			Vertex bottomClip=new Vertex(x,y);
			dcel.vertexList.Add(bottomClip);

			//create two pseudo edges for the clips for the middle face
			Edge cornerLeft=new Edge(true);
			Edge cornerBottom=new Edge(bottomClip,true);
			dcel.edgeList.Add(cornerLeft);
			dcel.edgeList.Add(cornerBottom);

			triplet.middle.siteEvent.face.AppendToForwardList(cornerBottom);
			triplet.middle.siteEvent.face.PrependToBackwardList(cornerLeft,leftClip);

			//create another bottom left vertex
			Vertex bottomLeftCorner=new Vertex(dcel.lx,dcel.ly,true);
			dcel.vertexList.Add(bottomLeftCorner);
			triplet.middle.siteEvent.face.ConnectBackwardAndForwardListsAt(bottomLeftCorner);
		}

		private Edge ClipClosingEdgeCrossingBoundsBetween(Parabola leftSide,Parabola rightSide,DoublyConnectedEdgeList dcel){
			//find the clipped point with the upper bounds
			float y = dcel.uy;
			float x = dcel.GetXOfParabolaIntersectionGivenY(leftSide.siteEvent,rightSide.siteEvent,y);
			Vertex clippedPoint;
			if(x<dcel.lx){
				x=dcel.lx;
				y=dcel.GetYOfParabolaIntersectionGivenX(leftSide.siteEvent,rightSide.siteEvent,x);
			}else if(x>dcel.ux){
				x=dcel.ux;
				y=dcel.GetYOfParabolaIntersectionGivenX(leftSide.siteEvent,rightSide.siteEvent,x);
			}
			clippedPoint=new Vertex(x,y,true);	
			dcel.vertexList.Add(clippedPoint);

			//get the edge whose endpoint this breakpoint is(create if needed)
			InternalNode breakpoint=NodeForBreakpointBetween(leftSide.siteEvent,rightSide.siteEvent,triplet.middle.parent);
			Edge terminatedEdge=null;
			if(breakpoint.edge==null){
				terminatedEdge=new Edge();
				dcel.edgeList.Add(terminatedEdge);
				dcel.edgeList.Add(terminatedEdge.twin);
				breakpoint.edge=terminatedEdge;
			}else{
				terminatedEdge=breakpoint.edge;
			}

			//set the clipped point as the origin of the left face(middle in this case) 
			terminatedEdge.origin=clippedPoint;

			//add to forward and backward lists of left and right sides respectively
			leftSide.siteEvent.face.AppendToForwardList(terminatedEdge);
			rightSide.siteEvent.face.PrependToBackwardList(terminatedEdge.twin,terminatedEdge.origin);

			return terminatedEdge;
		}

		private InternalNode NodeForBreakpointBetween(SiteEvent site1,SiteEvent site2,InternalNode fromParent){

			//keep going up from the given parent until you reach a (left,right) breakpoint
			InternalNode breakpointNode=fromParent;			
			while(breakpointNode!=null && breakpointNode.IsBreakpointBetween(triplet.left.siteEvent,triplet.right.siteEvent)){
				breakpointNode=breakpointNode.parent;
			}
			return breakpointNode;
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

	public class PriorityQueue{

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

	public abstract class Node{
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

	public class Parabola:Node{
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

	public class Triplet{
		public Parabola left;
		public Parabola middle;
		public Parabola right;

		public Triplet(Parabola left,Parabola middle,Parabola right){
			this.left=left;
			this.middle=middle;
			this.right=right;
		}

		public CircleEvent ComputeCircleEvent(){

			//make sure the three arcs have distinct site events
			if(left.siteEvent==right.siteEvent || left.siteEvent==middle.siteEvent || middle.siteEvent==right.siteEvent){
				return null;
			}

			//its possible that the three parabola nodes considered are not consecutive because one outlier's(left or right) site event 
			//is situated over on the other side, in which case, the the middle arc is not formed "at" the beachline but within it.
			//For this purpose we make a simple check. if left-> middle-> right is counter clockwise, then we return null.
			//			if ((lSite.y-cSite.y)*(rSite.x-cSite.x)<=(lSite.x-cSite.x)*(rSite.y-cSite.y)) {return;}
			if((left.siteEvent.y-middle.siteEvent.y)*(right.siteEvent.x-middle.siteEvent.x)>=(left.siteEvent.x-middle.siteEvent.x)*(right.siteEvent.y-middle.siteEvent.y)){
				return null;
			}

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

	public class InternalNode:Node{
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

		public bool IsBreakpointBetween(SiteEvent site1,SiteEvent site2){
			return this.site1==site1 && this.site2==site2;
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

	public class BeachLine{

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

	public class Vertex{
		public float x;
		public float y;
		public bool isLyingOnBounds=false;

		public Vertex(float x,float y,bool  isLyingOnBounds){
			this.x=x;
			this.y=y;
			this.isLyingOnBounds=isLyingOnBounds;
		}

		public Vertex(float x,float y):this(x,y,false){			
		}

		public override string ToString (){
			return "("+x+","+y+")";
		}
	}

	public class Face{
		public SiteEvent siteEvent;
		private Edge start;
		private Edge last;
		private Edge forwardList;
		private Edge backwardList;
		private Edge startTerminal;
		private Edge endTerminal;

		public Face(SiteEvent siteEvent){
			this.siteEvent=siteEvent;
		}

		public override string ToString (){
			return "F"+siteEvent;
		}

		public Edge GetStartingEdge(){
			return start;
		}

		public Edge GetForwardTerminal(){
			return startTerminal;
		}

		public Edge GetBackwardTerminal(){
			return endTerminal;
		}

		public void AppendToForwardList(Edge edge){
			edge.face=this;
			edge.next=null;
			edge.previous=null;
			if(forwardList==null){
				forwardList=edge;
			}else{
				startTerminal.next=edge;
				edge.previous=startTerminal;
			}
			startTerminal=edge;
		}

		public void PrependToBackwardList(Edge edge,Vertex originOfDanglingLast){
			edge.face=this;
			edge.next=null;
			edge.previous=null;
			if(backwardList==null){
				backwardList=edge;
			}else{
				endTerminal.origin=originOfDanglingLast;
				endTerminal.previous=edge;
				edge.next=endTerminal;
			}
			endTerminal=edge;
		}

		public void CreateForwardAndBackwardListsAt(Vertex origin,Edge endingEdge,Edge startingEdge){

			startingEdge.origin=origin;

			this.start=startingEdge;
			this.forwardList=startingEdge;
			this.startTerminal=startingEdge;

			this.last=endingEdge;
			this.backwardList=endingEdge;
			this.endTerminal=endingEdge;

			endingEdge.next=startingEdge;
			startingEdge.previous=endingEdge;
		}

		public void ConnectBackwardAndForwardListsAt(Vertex originOfDanglingLast){
			endTerminal.origin=originOfDanglingLast;
			startTerminal.next=endTerminal;
		}

		public bool ForwardListNotStarted(){
			return forwardList==null;
		}

		public bool BackwardListNotStarted(){
			return backwardList==null;
		}

		/**
		 * Deprecated: only use for appending a cyclic list of edges
		 */
		public void AddEdge(Edge edge){
			if(start==null){
				start=edge;
				last=edge;
			}else{
				last.next=edge;
				edge.previous=last;
				last=edge;
			}
			last.next=start;
		}
			
		public bool CompleteFaceIfIncomplete(DoublyConnectedEdgeList dcel){			
			return false;
		}
	}

	public class Edge{
		public Vertex origin;
		public Edge twin;
		public Edge next;
		public Edge previous;
		public Face face;

		/**
		 * Creates an edge with a twin. Intentionally private so that its only used internally 
		 * and doesn't become recursive
		 */
		private Edge(Edge twin){
			this.twin=twin;
		}

		public Edge(){			
			this.twin=new Edge(this);
		}			

		public Edge(Vertex origin){
			this.origin=origin;
			this.twin=new Edge(this);
		}

		public Edge(Vertex origin,bool dontCreateTwin){
			this.origin=origin;
			if(!dontCreateTwin){
				this.twin=new Edge(this);
			}
		}

		public Edge(bool dontCreateTwin){

		}

		public Edge ForFace(Face face){
			if(this.face==face){
				return this;
			}else if(this.twin.face==face){
				return twin;
			}else{
				return null;
			}
		}

		public override string ToString (){
			return "E"+this.face+" "+this.origin;
		}
	}

	public class DoublyConnectedEdgeList{
		//bounding box lower left and upper right coordinates
		public float lx;
		public float ly;
		public float ux;
		public float uy;
		public List<Vertex> vertexList=new List<Vertex>();
		public List<Face> faceList=new List<Face>();
		public List<Edge> edgeList=new List<Edge>();

		public DoublyConnectedEdgeList(float lx,float ly,float ux,float uy){
			this.lx=lx;
			this.ly=ly;
			this.ux=ux;
			this.uy=uy;
		}

		public override string ToString (){
			return "V: "+vertexList.Count+" E: "+edgeList.Count+" F: "+faceList.Count;
		}

		public float GetXOfParabolaIntersectionGivenY(SiteEvent site1,SiteEvent site2, float y){
			//using the equation of the parabola we find the x by substituting out the directrix
			return (site2.x*site2.x - site1.x*site1.x + site2.y*site2.y - site1.y*site1.y - 2*site2.y*y + 2*site1.y*y)/(2*(site2.x - site1.x));
		}

		public float GetYOfParabolaIntersectionGivenX(SiteEvent site1,SiteEvent site2, float x){
			//using the equation of the parabola we find the y by substituting out the directrix
			return (site1.x*site1.x - site2.x*site2.x + site1.y*site1.y - site2.y*site2.y + 2*site2.x*x - 2*site1.x*x)/(2*(site1.y - site2.y));
		}
	}
}

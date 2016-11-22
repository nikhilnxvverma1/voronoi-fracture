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
			for(int i=0;i<input.Length/2;i++){
				queue.Push(new SiteEvent(input[i,0],input[i,1]));
			}
			while(!queue.IsEmpty()){
				Event thisEvent=queue.Pop();
				if (thisEvent!=null) {
					thisEvent.Handle ();
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

		abstract public void Handle();

		public override string ToString (){
			return "("+x+","+y+")";
		}
	}

	class SiteEvent :Event{

		public SiteEvent(float x,float y):base(x,y){
			
		}

		override public void Handle(){
			Console.WriteLine(this);
		}
	}

	class CircleEvent : Event{
		Parabola parabola;	

		public CircleEvent(float x,float y):base(x,y){
			
		}
		override public void Handle(){
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
		abstract public bool IsLeaf();
	}

	class Parabola:Node{
		SiteEvent siteEvent;
		CircleEvent circleEvent;
		public override bool IsLeaf (){
			return true;
		}
	}

	class Internal:Node{
		SiteEvent site1;
		SiteEvent site2;
		Edge edge;
		Node left;
		Node right;

		public override bool IsLeaf (){
			return false;
		}
	}

	class BeachLine{
		Parabola GetParabolaByX(float x){
			return null;
		}
	}

	class Vertex{
		float x;
		float y;
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


}

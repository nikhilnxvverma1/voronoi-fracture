using System;
using System.Collections;

namespace VoronoiFracture{
	
	class MainClass{
		
		public static void Main (string[] args){

			//input
			float [,] input=new float[,]{{23,43},{54,14},{34,12},{12,53},{12,56},{88,54}};

			//make the event queue
			EventQueue queue=new EventQueue();
			for(int i=0;i<input.Length/2;i++){
				queue.Push(new Event(input[i,0],input[i,1]));
			}

			while(!queue.IsEmpty()){
				Event thisEvemt=queue.Pop();
				if (thisEvemt!=null) {
					thisEvemt.Handle ();
				}
			}
		}
	}

	class Event{
		public float x;
		public float y;

		public Event(float x,float y){
			this.x=x;
			this.y=y;
		}

		virtual public void Handle(){
				
		}
	}

	class CircleEvent : Event{
		Parabola parabola;	

		public CircleEvent(float x,float y):base(x,y){
			
		}
		override public void Handle(){
			
		}
	}

	class EventQueue{

		public Event Top(){
			return  null;
		}

		public Event Pop(){
			return null;
		}

		public void Push(Event eventNode){
			Console.WriteLine("Inserting event "+eventNode.x+" "+eventNode.y);
		}

		public bool IsEmpty(){
			return false;
		}
	}

	abstract class Node{
		Node parent;
		abstract public bool IsLeaf();
	}

	class Parabola:Node{
		CircleEvent circleEvent;
		public override bool IsLeaf (){
			return true;
		}
	}

	class Internal:Node{
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

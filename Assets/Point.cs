using UnityEngine;
using System.Collections;

public class Point {

	double x;
	double y;
	double z;

	public Point(double x,double y){
		this.x=x;
		this.y=y;
		this.z=0;
	}

	public Point(double x,double y,double z){
		this.x=x;
		this.y=y;
		this.z=z;
	}
}

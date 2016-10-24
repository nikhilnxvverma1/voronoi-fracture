/**
 * Created by NikhilVerma on 16/10/16.
 */
public class Main {

    Point [] input={
            new Point(-0.5,-0.5),
            new Point(-0.5,0.5),
            new Point(-0.2,0.2),
            new Point(-0.1,0.8),
            new Point(0.3,-0.7),
            new Point(0.2,-0.1),
            new Point(0.6,0.1),
            new Point(0.7,0.5)
    };

    public static void main(String[] args) {

    }

}

class Point{
    double x;
    double y;

    public Point(double x, double y) {
        this.x = x;
        this.y = y;
    }
}

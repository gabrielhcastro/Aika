namespace GameServer.Model.World;
public class Position {
    public float X { get; set; }
    public float Y { get; set; }

    public Position(float x, float y) {
        X = x;
        Y = y;
    }
}
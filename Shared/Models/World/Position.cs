namespace Shared.Models.World;

public class Position {
    public Single X { get; set; }
    public Single Y { get; set; }

    public Position(Single x, Single y) {
        X = x; 
        Y = y;
    }
}
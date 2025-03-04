using GameServer.Model.World;

namespace GameServer.Model.Character;

public class Neighbors {
    bool Occuped;
    Position Position;

    public Neighbors(Single positionX, Single positionY) {
        Position = new(positionX, positionY);
        Occuped = false;
    }
}
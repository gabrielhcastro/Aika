using GameServer.Model.World;

namespace GameServer.Model.Character;

public class Neighbors(Single positionX, Single positionY) {
    readonly bool Occuped = false;
    Position Position = new(positionX, positionY);
}
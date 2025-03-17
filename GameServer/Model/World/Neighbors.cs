namespace GameServer.Model.World; 
public struct Neighbors(Position position) {
    public Position Position { get; } = position;
    public bool Occupied { get; private set; } = false;

    public void SetOccupied(bool status) => Occupied = status;
}
namespace GameServer.Model.World;
public struct Position(float x, float y) {
    public float X { get; set; } = x;
    public float Y { get; set; } = y;

    public readonly bool IsValid() {
        return !(
            float.IsInfinity(X) ||
            float.IsInfinity(Y) ||
            float.IsNaN(X) ||
            float.IsNaN(Y)
            );
    }

    public readonly float Distance(Position pos) {
        if(!IsValid() || !pos.IsValid()) return float.MaxValue;

        float dx = X - pos.X;
        float dy = Y - pos.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public readonly bool InRange(Position pos, float range) {
        return Distance(pos) <= range;
    }

    public readonly void ForEach(byte range, Action<Position> action) {
        int startX = Math.Max((int)Math.Floor(X) - range, 0);
        int startY = Math.Max((int)Math.Floor(Y) - range, 0);
        int endX = Math.Min((int)Math.Floor(X) + range, 4096);
        int endY = Math.Min((int)Math.Floor(Y) + range, 4096);

        for(int x = startX; x <= endX; x++) {
            for(int y = startY; y <= endY; y++) {
                action(new Position(x, y));
            }
        }
    }

    public static Position Lerp(Position start, Position dest, float time) {
        return start + (dest - start) * time;
    }

    public static Position Qerp(Position start, Position dest, float time, bool inverse = false) {
        float quad = inverse ? 2 - time : time;
        return start + (dest - start) * time * quad;
    }

    public static bool operator ==(Position pos1, Position pos2) {
        return pos1.X == pos2.X && pos1.Y == pos2.Y;
    }

    public static bool operator !=(Position pos1, Position pos2) {
        return !(pos1 == pos2);
    }

    public static Position operator +(Position pos1, Position pos2) {
        return new Position(pos1.X + pos2.X, pos1.Y + pos2.Y);
    }

    public static Position operator -(Position pos1, Position pos2) {
        return new Position(pos1.X - pos2.X, pos1.Y - pos2.Y);
    }

    public static Position operator *(Position pos, float value) {
        return new Position(pos.X * value, pos.Y * value);
    }

    public static Position MidAdvanceValue(Position currentPosition, float range) {
        return currentPosition + new Position(range / 2, range / 2);
    }

    public static Position MidBackValue(Position oldestPosition, float range) {
        return oldestPosition - new Position(range / 2, range / 2);
    }

    public readonly Position Floor() {
        return new Position(MathF.Floor(X), MathF.Floor(Y));
    }

    public override readonly int GetHashCode() {
        return HashCode.Combine(X, Y);
    }

    public override readonly bool Equals(object obj) {
        return obj is Position pos && this == pos;
    }
}
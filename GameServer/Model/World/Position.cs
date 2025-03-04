namespace GameServer.Model.World; 
public struct Position(float x, float y) {
    public float X { get; set; } = x;
    public float Y { get; set; } = y;

    public readonly bool IsValid() {
        return !float.IsInfinity(X) && !float.IsInfinity(Y) &&
               !float.IsNaN(X) && !float.IsNaN(Y);
    }

    public readonly float Distance(Position pos) {
        if(!IsValid() || !pos.IsValid()) return 65354;

        try {
            float dx = X - pos.X;
            float dy = Y - pos.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            return Math.Clamp(distance, 0, 65354);
        }
        catch(Exception ex) {
            Console.WriteLine($"Erro de cálculo em Distance: {ex.Message}");
            return 65354;
        }
    }

    public readonly bool InRange(Position pos, float range) {
        return Distance(pos) <= range;
    }

    public readonly void ForEach(byte range, Action<Position> action) {
        for(int x = (int)Math.Round(X) - range; x <= (int)Math.Round(X) + range; x++) {
            for(int y = (int)Math.Round(Y) - range; y <= (int)Math.Round(Y) + range; y++) {
                if(x > 4096 || x == 0 || y > 4096 || y == 0)
                    continue;

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
        float newRange = range / 2;
        return new Position(currentPosition.X + newRange, currentPosition.Y + newRange);
    }

    public static Position MidBackValue(Position oldestPosition, float range) {
        float newRange = range / 2;
        return new Position(oldestPosition.X - newRange, oldestPosition.Y - newRange);
    }

    public override readonly bool Equals(object obj) {
        if(obj is Position pos) return this == pos;
        return false;
    }

    public override readonly int GetHashCode() {
        return HashCode.Combine(X, Y);
    }
}
using GameServer.Model.Character;
using GameServer.Model.World;
using Shared.Core;

namespace GameServer.Core.Handlers.Game;

// Quadtree implementation for character view handling
public class ViewHandler : Singleton<ViewHandler> {

    public ViewHandler() { }

    public ViewHandler(float width, float height) : base() {
        _root = new Node(0, 0, width, height);
    }

    private class Node(float x, float y, float width, float height) {
        public float X = x, Y = y, Width = width, Height = height;
        public List<CharacterEntitie> Characters = new List<CharacterEntitie>();
        public Node[] Children = new Node[4];

        public bool Contains(Position position) =>
            position.X >= X && position.X < X + Width &&
            position.Y >= Y && position.Y < Y + Height;
    }

    private readonly Node _root;

    public void Insert(CharacterEntitie character) => Insert(_root, character);

    private static void Insert(Node node, CharacterEntitie character) {
        if(!node.Contains(character.Position)) return;

        if(node.Characters.Count < 4 && node.Children[0] == null) {
            node.Characters.Add(character);
            return;
        }

        if(node.Children[0] == null) Split(node);

        foreach(var child in node.Children) {
            if(child.Contains(character.Position)) {
                Insert(child, character);
                return;
            }
        }
    }

    private static void Split(Node node) {
        float hw = node.Width / 2, hh = node.Height / 2;
        node.Children[0] = new Node(node.X, node.Y, hw, hh);
        node.Children[1] = new Node(node.X + hw, node.Y, hw, hh);
        node.Children[2] = new Node(node.X, node.Y + hh, hw, hh);
        node.Children[3] = new Node(node.X + hw, node.Y + hh, hw, hh);
    }

    public List<CharacterEntitie> QueryRange(Position position, float range) {
        var results = new List<CharacterEntitie>();
        QueryRange(_root, position, range, results);
        return results;
    }

    private static void QueryRange(Node node, Position position, float range, List<CharacterEntitie> results) {
        if(node == null) return;

        if(!NodeIntersectsRange(node, position, range)) return;

        foreach(var character in node.Characters) {
            if(character.Position.InRange(position, range))
                results.Add(character);
        }

        foreach(var child in node.Children) {
            QueryRange(child, position, range, results);
        }
    }

    private static bool NodeIntersectsRange(Node node, Position position, float range) {
        return position.X + range >= node.X && position.X - range <= node.X + node.Width &&
               position.Y + range >= node.Y && position.Y - range <= node.Y + node.Height;
    }
}

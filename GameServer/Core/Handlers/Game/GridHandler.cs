using GameServer.Model.Character;
using GameServer.Model.World;
using Shared.Core;

namespace GameServer.Core.Handlers.Game;
internal class GridHandler : Singleton<GridHandler> {
    private readonly int _cellSize;
    private readonly Dictionary<(int, int), List<CharacterEntitie>> _grid = [];

    public GridHandler() {
        _cellSize = 100;
    }

    public void AddCharacter(CharacterEntitie character) {
        var cell = GetCell(character.Position);
        if(!_grid.TryGetValue(cell, out List<CharacterEntitie> value)) {
            value = ([]);
            _grid[cell] = value;
        }

        value.Add(character);

        Console.WriteLine($"Personagem {character.Name} adicionado à célula ({cell.Item1}, {cell.Item2}).");
    }

    public void RemoveCharacter(CharacterEntitie character) {
        var cell = GetCell(character.Position);
        if(_grid.TryGetValue(cell, out List<CharacterEntitie> value)) {
            value.Remove(character);
            Console.WriteLine($"Personagem {character.Name} removido da célula ({cell.Item1}, {cell.Item2}).");
        }
    }

    public List<CharacterEntitie> GetNearbyCharacters(Position position, float range) {
        var nearbyCharacters = new List<CharacterEntitie>();
        var centerCell = GetCell(position);

        Console.WriteLine($"Buscando personagens próximos na célula central ({centerCell.Item1}, {centerCell.Item2}).");

        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {
                var cell = (centerCell.Item1 + x, centerCell.Item2 + y);
                if(_grid.TryGetValue(cell, out List<CharacterEntitie> value)) {
                    nearbyCharacters.AddRange(value);

                    Console.WriteLine($"Célula ({cell.Item1}, {cell.Item2}) contém {value.Count} personagens.");
                }
            }
        }

        var filteredCharacters = nearbyCharacters.Where(c => c.Position.Distance(position) <= range).ToList();

        Console.WriteLine($"Total de personagens encontrados dentro do alcance: {filteredCharacters.Count}");

        return filteredCharacters;
    }

    private (int, int) GetCell(Position position) {
        var cell = ((int)(position.X / _cellSize), (int)(position.Y / _cellSize));
        Console.WriteLine($"Posição ({position.X}, {position.Y}) mapeada para a célula ({cell.Item1}, {cell.Item2}).");
        return cell;
    }
}

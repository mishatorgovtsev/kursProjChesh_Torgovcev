using ChessDotNet;

namespace kursProjChesh_Torgovcev.services;

public class ChessService
{
    private ChessGame _game;
    
    public ChessService()
    {
        _game = new ChessGame();
    }
    
    // Загрузить позицию из FEN (для восстановления состояния игры)
    public void LoadPosition(string fen)
    {
        _game = new ChessGame(fen);
    }
    
    public (bool success, string newFen, string pgnMove) MakeMove(string from, string to, string promotion = "q")
    {
        char promotionChar = promotion?.ToLower() switch
        {
            "r" => 'R',
            "b" => 'B',
            "n" => 'N',
            _   => 'Q'  // по умолчанию ферзь
        };

        // Пробуем сначала с promotion (для пешки), потом без
        var moveWithPromo = new Move(from, to, _game.WhoseTurn, promotionChar);
    
        if (_game.IsValidMove(moveWithPromo))
        {
            _game.MakeMove(moveWithPromo, true);
            return (true, _game.GetFen(), moveWithPromo.ToString());
        }

        // Обычный ход без превращения
        var move = new Move(from, to, _game.WhoseTurn);
        if (_game.IsValidMove(move))
        {
            _game.MakeMove(move, true);
            return (true, _game.GetFen(), move.ToString());
        }
    
        return (false, _game.GetFen(), string.Empty);
    }
    
    public bool IsGameOver() 
    { 
        return _game.IsCheckmated(_game.WhoseTurn) || _game.IsStalemated(_game.WhoseTurn); 
    }
    
    public string GetCurrentFen() => _game.GetFen();
    
    public Player WhoseTurn => _game.WhoseTurn;
}
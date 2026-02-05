using ChessDotNet;

namespace kursProjChesh_Torgovcev.services;

public class ChessService
{
    private readonly ChessGame _game;
    
    public ChessService()
    {
        _game = new ChessGame();
    }
    
    public (bool success, string newFen, string pgnMove) MakeMove(string from, string to)
    {
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
namespace PlaylandBoxer;

public class Game
{
    public void Run()
    {
        Console.WriteLine("Starting Playland Boxer...");
        Console.WriteLine("Place your graphics and video assets in the Assets folder and update the game logic to load them.");

        var assets = new GameAssets();
        if (!assets.Load())
        {
            Console.WriteLine("No assets loaded yet.");
        }
        else
        {
            Console.WriteLine("Assets loaded successfully.");
        }

        Console.WriteLine("Ready for next development step.");
    }
}

using System.Collections.Generic;
using UnityEngine;

public static class ShellPoolManager
{
    private static readonly Dictionary<string, ShellPool> pools;

    private static readonly Dictionary<Game, List<string>> preloadShells = new Dictionary<Game, List<string>>
    {
        { Game.MetalForce, new List<string> { "Shell_MachineGun", "Shell_1", "Shell_2", "Shell_3" } },
        { Game.BattleOfWarplanes, new List<string> { "Shell_1", "Shell_SACLOSMissile" } },
        { Game.WingsOfWar, new List<string> { "Shell_1", "Shell_SACLOSMissile" } },
        { Game.Armada, new List<string> { "Shell_1" } }
    };

    static ShellPoolManager()
    {
        pools = new Dictionary<string, ShellPool>(10);
    }

    public static Shell GetShell(string shellName, Vector3 position, Quaternion rotation)
    {
        ShellPool shellPool;

        if (!pools.TryGetValue(shellName, out shellPool))
        {
            shellPool = new ShellPool(shellName);
            pools.Add(shellName, shellPool);
        }

        Shell shell = shellPool.FromPool();

        shell.transform.position = position;
        shell.transform.rotation = rotation;

        return shell;
    }

    public static Shell GetShell(string shellName)
    {
        ShellPool shellPool;

        if (!pools.TryGetValue(shellName, out shellPool))
        {
            shellPool = new ShellPool(shellName);
            pools.Add(shellName, shellPool);
        }

        Shell shell = shellPool.FromPool();

        return shell;
    }

    public static void ReturnToPool(Shell shell)
    {
        ShellPool shellPool;

        if (!pools.TryGetValue(shell.shellName, out shellPool))
        {
            DT.LogError("Pool ({0}) doesn't exist", shell.shellName);
            return;
        }

        shellPool.Return(shell);
    }

    public static void ClearAllPools()
    {
        pools.Clear();
    }

    public static void ReloadAllPools()
    {
        pools.Clear();
        PreloadShells();
    }

    public static void PreloadShells()
    {
        foreach (var preloadShellPair in preloadShells)
        {
            if (preloadShellPair.Key == GameData.CurrentGame)
            {
                foreach (string preloadShellName in preloadShellPair.Value)
                {
                    ShellPool shellPool;

                    if (!pools.TryGetValue(preloadShellName, out shellPool))
                    {
                        shellPool = new ShellPool(preloadShellName);
                        pools.Add(preloadShellName, shellPool);
                    }
                }
            }
        }
    }
}
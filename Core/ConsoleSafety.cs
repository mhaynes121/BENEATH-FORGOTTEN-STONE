namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Guards Console APIs that throw when stdout isn't backed by a real
/// console handle (e.g. certain debugger hosts) or when the requested
/// size exceeds what the host terminal allows. Shared by every screen
/// (title, menus, in-game HUD) rather than duplicated per screen.
/// </summary>
public static class ConsoleSafety
{
    public static void TrySetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// Guards the single most common crash this game has actually hit in the wild: Console.Clear()
    /// throws IOException("The handle is invalid.") whenever the console handle it reads to
    /// measure the buffer isn't valid -- observed for real during interactive play, not just a
    /// headless/redirected-output edge case. Every screen's own "wipe the console before drawing"
    /// call should go through this instead of raw Console.Clear() (see the many call sites this
    /// replaced) so one bad frame never crashes the whole game.
    /// </summary>
    public static void TryClear()
    {
        try
        {
            Console.Clear();
            return;
        }
        catch (IOException)
        {
        }

        // The real-world trigger for this (a window resize/move) leaves .NET holding a stale
        // reference to the OS console handle -- reassigning Console.Out to a fresh stream over
        // that same OS handle forces it to reacquire, which is what actually fixes Clear()/
        // SetCursorPosition for the REST of the session, not just this one call. Tried before the
        // retry below so the retry has a real chance instead of just hoping the identical stale
        // reference recovers on its own.
        TryReacquireOutputHandle();

        try
        {
            Console.Clear();
            return;
        }
        catch (IOException)
        {
        }

        // Console.Clear() keeps failing -- the handle isn't coming back on its own this session
        // (this is what used to leave a screen's stale content -- e.g. InventoryScreen's wider
        // two-column layout -- permanently stuck on top of whatever draws next, only ever fixed
        // by a full process restart via save/reload). Console.Clear()/SetCursorPosition are the
        // only two APIs observed to throw this; plain Console.WriteLine keeps working (the game
        // keeps rendering just fine, just with residue), so fall back to manually overwriting
        // every row with blank lines. Advancing via '\n' rather than SetCursorPosition means this
        // never depends on the exact APIs that are already failing.
        try
        {
            int width = SafeConsoleWidth();
            int height = SafeConsoleHeight();
            string blankLine = new string(' ', width);
            for (int row = 0; row < height; row++)
            {
                Console.WriteLine(blankLine);
            }
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Console.WindowWidth/BufferWidth can throw the same "handle invalid" IOException as Clear/SetCursorPosition -- falls back to a size comfortably larger than anything this game ever draws (DungeonGenerator's levels cap at 60x22; InventoryScreen's widest content is still well under this) rather than letting TryClear's manual-blank fallback fail before it even starts.</summary>
    private static int SafeConsoleWidth()
    {
        try
        {
            return Math.Max(Console.WindowWidth, Console.BufferWidth);
        }
        catch (IOException)
        {
            return 120;
        }
    }

    private static int SafeConsoleHeight()
    {
        try
        {
            return Math.Max(Console.WindowHeight, Console.BufferHeight);
        }
        catch (IOException)
        {
            return 50;
        }
    }

    /// <summary>Forces .NET to reacquire the OS console output handle instead of holding onto whatever stale reference triggered the IOException -- a plain StreamWriter over a fresh Console.OpenStandardOutput() handle, same as Console.Out already wraps internally, so this changes nothing about normal output once it succeeds.</summary>
    private static void TryReacquireOutputHandle()
    {
        try
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// Same guard as TryClear, for Console.SetCursorPosition -- the exact call that followed
    /// Console.Clear() in the actual crash report this was added for (both throw the identical
    /// IOException from the same momentarily-invalid handle). Also guards ArgumentOutOfRangeException,
    /// matching Renderer's own private cursor-position helper, in case a caller's row/column ever
    /// falls outside the console's current buffer.
    /// </summary>
    public static void TrySetCursorPosition(int left, int top)
    {
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch (IOException)
        {
            // Same stale-handle recovery as TryClear above -- reacquire first, then retry, rather
            // than just hoping the identical stale reference works this time.
            TryReacquireOutputHandle();
            try
            {
                Console.SetCursorPosition(left, top);
            }
            catch (IOException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    /// <summary>
    /// Non-blocking key peek for the Sleep command -- Console.KeyAvailable/ReadKey both throw
    /// InvalidOperationException when stdin has been redirected (e.g. a headless/piped run, same
    /// class of host-environment issue TryClear/TrySetCursorPosition guard for their own APIs),
    /// on top of the usual IOException. Returns null on either failure or when nothing is waiting,
    /// so a caller can treat "no key" and "can't check" identically -- sleep just keeps going.
    /// </summary>
    public static ConsoleKeyInfo? TryReadKeyIfAvailable()
    {
        try
        {
            return Console.KeyAvailable ? Console.ReadKey(intercept: true) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static void TryFitConsole(int width, int height)
    {
        try
        {
            int desiredWidth = Math.Min(width, Console.LargestWindowWidth);
            int desiredHeight = Math.Min(height, Console.LargestWindowHeight);

            if (Console.BufferWidth < desiredWidth || Console.BufferHeight < desiredHeight)
            {
                Console.SetBufferSize(
                    Math.Max(Console.BufferWidth, desiredWidth),
                    Math.Max(Console.BufferHeight, desiredHeight));
            }

            if (Console.WindowWidth < desiredWidth || Console.WindowHeight < desiredHeight)
            {
                Console.SetWindowSize(
                    Math.Max(Console.WindowWidth, desiredWidth),
                    Math.Max(Console.WindowHeight, desiredHeight));
            }
        }
        catch (IOException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
}
